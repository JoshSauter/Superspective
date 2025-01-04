using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Saving;
using SerializableClasses;
using Sirenix.OdinInspector;
using SuperspectiveUtils;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace GrowShrink {
    [RequireComponent(typeof(UniqueId))]
    public class GrowShrinkHallway : SuperspectiveObject<GrowShrinkHallway, GrowShrinkHallway.GrowShrinkHallwaySave> {
        public float scaleFactor = 4;

        private GrowShrinkTransitionTrigger EffectiveTriggerZone => shrunkTriggerZone ? shrunkTriggerZone : originalTriggerZone;
        public GrowShrinkTransitionTrigger originalTriggerZone;
        [ReadOnly]
        public ProBuilderMesh[] pbMeshes;
        [ReadOnly]
        public ProBuilderMesh[] targetPbMeshes; // One per material
        [ReadOnly]
        public GrowShrinkTransitionTrigger shrunkTriggerZone;

        private bool PbMeshesNeedToBeSet => true || pbMeshes == null || pbMeshes.Length == 0 || pbMeshes.Any(m => m == null);

        // Objects that are or were in the tunnel at some point
        private Dictionary<string, SuperspectiveReference<GrowShrinkObject>> growShrinkObjects = new Dictionary<string, SuperspectiveReference<GrowShrinkObject>>();

        string GetId(Collider c) => GrowShrinkTransitionTrigger.GetId(c);

#if UNITY_EDITOR
        // Note to self: If the results of this look fucked up, check that the pivot points for combined mesh & trigger zone hitbox are the same
        [Button("Compile")]
        void Compile() {
            Undo.SetCurrentGroupName("Compile GrowShrinkHallway");
            int compileGroup = Undo.GetCurrentGroup();
            
            Decompile(true);

#region Vertex Transformation Methods
            Vector3 pivotAxis = (originalTriggerZone.largeSidePointWorld - originalTriggerZone.smallSidePointWorld).normalized;
            
            Undo.RecordObject(this, "GrowShrinkHallway: Set Trigger Zone Boundaries");
            Undo.RecordObject(EffectiveTriggerZone, "GrowShrinkHallway: Set Trigger Zone Boundaries");
            EffectiveTriggerZone.SetupBoundaries(originalTriggerZone.MeshCollider);
            Vector3 smallSidePointWorld = EffectiveTriggerZone.smallSidePointWorld;
            Vector3 largeSidePointWorld = EffectiveTriggerZone.largeSidePointWorld;
            float logScaleFactor = Mathf.Log(scaleFactor);
            
            float originalLength = (largeSidePointWorld - smallSidePointWorld).magnitude;
            float resultingLengthOfHallway = (largeSidePointWorld - smallSidePointWorld).magnitude * (logScaleFactor / (scaleFactor - 1));
            float lengthRatio = resultingLengthOfHallway / originalLength;
            
            // Takes in a world position and returns the world position closest to it along the pivot line
            Vector3 PivotPoint(Vector3 sample) {
                Vector3 sampleToOrigin = sample - largeSidePointWorld;

                float dot = Vector3.Dot(sampleToOrigin, pivotAxis);
                return largeSidePointWorld + pivotAxis * dot;
            }

            // Transforms a vertex, bringing it in along the pivot axis, then scaling down the cross-section to the new size
            Vector3 TransformVertex(Vector3 worldPos) {
                // Compute the pivot point for this vertex (a reference point along the hallway axis)
                Vector3 pivotPoint = PivotPoint(worldPos);
                
                // Compute normalized position along the hallway (0 = start, 1 = end but can go beyond for further shrinking on e.g. the doorframe at the small side)
                float t = Mathf.Clamp(Utils.Vector3InverseLerp(largeSidePointWorld, smallSidePointWorld, pivotPoint), 0, float.MaxValue);

                // Compute scale factor at this point
                float vertexScaleFactor = 1f / (1 + t * (scaleFactor - 1));
                
                // Yay, integrals:
                float scaledLengthAtT = Mathf.Log(1 + t * (scaleFactor - 1)) / logScaleFactor;
                float distanceToBringIn = (largeSidePointWorld-smallSidePointWorld).magnitude * (t - scaledLengthAtT * lengthRatio);
                
                // Bring the vertex closer or farther along the axis by scaling the distance
                Vector3 modifiedPivotPoint = pivotPoint + pivotAxis * distanceToBringIn;
                
                // Shrink or grow the cross-section in the plane perpendicular to the pivot axis
                Vector3 sampleToPivot = worldPos - pivotPoint;
                float unscaledDistance = sampleToPivot.magnitude;
                float scaledDistance = unscaledDistance * vertexScaleFactor;
                Vector3 targetPoint = modifiedPivotPoint + sampleToPivot.normalized * scaledDistance;

                if (DEBUG) {
                    Debug.DrawLine(worldPos, targetPoint, Color.cyan, 5f);

                    float distanceBroughtIn = Vector3.Distance(pivotPoint, modifiedPivotPoint);
                    debug.Log($"{worldPos:F2} -> {targetPoint:F2}, brought in {distanceBroughtIn:F3} with scalar {vertexScaleFactor:F3}");
                }
                
                return targetPoint;
            }

            Vector3 TransformVertexShrinkOld(Vector3 worldPos) {
                Vector3 pivot = PivotPoint(worldPos);
                float unscaledDistance = Vector3.Distance(worldPos, pivot);
                float scaledDistance = unscaledDistance / scaleFactor;
                Vector3 sampleToPivot = (worldPos - pivot).normalized;
                Vector3 targetPoint = pivot + sampleToPivot * scaledDistance;

                float t = Mathf.Clamp01(Utils.Vector3InverseLerp(largeSidePointWorld, smallSidePointWorld, pivot));
                
                Vector3 vertexWorldPos = Vector3.Lerp(worldPos, targetPoint, t);
                Vector3 result = transform.InverseTransformPoint(vertexWorldPos);

                // debug.Log($"{vertex:F2} -> {result:F2}");
                if (DEBUG) {
                    Debug.DrawLine(worldPos, vertexWorldPos, Color.cyan, 5f);
                }
                
                return vertexWorldPos;
            }

            Vector3 TransformVertexInOld(Vector3 worldPos) {
                Vector3 pivot = PivotPoint(worldPos);
                float t = Mathf.Clamp(Utils.Vector3InverseLerp(largeSidePointWorld, smallSidePointWorld, pivot), 0, float.MaxValue);
                // debug.Log($"InverseLerp({largeSidePointWorld.x}, {smallSidePointWorld.x}, {pivot.x:F2}) = {t}");

                // Integral of some math I did who knows if it's actually right
                float scalar = ((t * t) * (t - scaleFactor * (t - 3)) / (6.0f * scaleFactor));
                float distanceToBringIn = (largeSidePointWorld-smallSidePointWorld).magnitude * scalar;

                Vector3 vertexWorldPos = worldPos + distanceToBringIn * pivotAxis;

                Vector3 result = transform.InverseTransformPoint(vertexWorldPos);

                debug.Log($"{worldPos:F2} -> {vertexWorldPos:F2}, brought in {distanceToBringIn:F3} with scalar {scalar:F3}");
                return vertexWorldPos;
            }
#endregion
            // Destroy the old combined mesh
            if (shrunkTriggerZone != null) {
                Undo.DestroyObjectImmediate(this.shrunkTriggerZone.gameObject);
                shrunkTriggerZone = null;
            }
            if (targetPbMeshes != null) {
                foreach (ProBuilderMesh pbMesh in targetPbMeshes) {
                    Undo.DestroyObjectImmediate(pbMesh.gameObject);
                }
                targetPbMeshes = null;
            }
            
            // Create a new root for the geometry
            Transform geometryRoot = new GameObject("Geometry Root").transform;
            Undo.RegisterCreatedObjectUndo(geometryRoot.gameObject, "GrowShrinkHallway: Create Geometry Root");
            geometryRoot.SetParent(transform);
            geometryRoot.SetSiblingIndex(0);
            geometryRoot.localScale = Vector3.one;
            
            // Set up CombineMesh gameObjects
            ProBuilderMesh CreateEmptyTargetPbMesh(Material forMaterial) {
                ProBuilderMesh targetPbMesh = ProBuilderMesh.Create();
                Undo.RegisterCreatedObjectUndo(targetPbMesh.gameObject, "GrowShrinkHallway: Create Combined Mesh");
                Undo.RecordObject(targetPbMesh.transform, "GrowShrinkHallway: Set Combined Mesh Transform");
                targetPbMesh.transform.position = transform.position;
                targetPbMesh.transform.rotation = transform.rotation;
                targetPbMesh.transform.SetParent(geometryRoot);
                targetPbMesh.transform.SetSiblingIndex(0);
                targetPbMesh.gameObject.name = $"{gameObject.name}_CombinedMesh_{forMaterial?.name ?? "Shape"}";
                
                // This line is needed to prevent a null Material from the new ProBuilderMesh from ending up in the resulting MeshRender materials
                if (forMaterial != null) {
                    targetPbMesh.SetMaterial(new List<Face>(), forMaterial);
                }

                return targetPbMesh;
            }

            Dictionary<Material, List<ProBuilderMesh>> meshesByMaterial = new Dictionary<Material, List<ProBuilderMesh>>();
            Dictionary<Material, ProBuilderMesh> targetMeshByMaterial = new Dictionary<Material, ProBuilderMesh>();
            if (PbMeshesNeedToBeSet) {
                Undo.RecordObject(this, "GrowShrinkHallway: Set PB Meshes");
                // Get all mesh renderers from children recursively, skipping this gameObject and the triggerZone gameObject
                pbMeshes = gameObject.GetComponentsInChildrenRecursively<ProBuilderMesh>()
                    .Where(mf => mf.gameObject.activeInHierarchy
                                 && mf.gameObject != this.gameObject
                                 && mf.gameObject != originalTriggerZone.gameObject)
                    .ToArray();
            }
            
            // Set up the target meshes for each material
            foreach (ProBuilderMesh pbMesh in pbMeshes) {
                Material[] materials = pbMesh.GetComponent<MeshRenderer>().sharedMaterials;
                if (materials.Length != 1) {
                    Debug.LogError("ProBuilder mesh has multiple materials. Split the mesh into multiple meshes with one material each.\nProblematic mesh: " + pbMesh.gameObject.name);
                    Undo.RevertAllInCurrentGroup();
                    return;
                }
                
                Material material = materials[0];
                // New material found, create a new target mesh for it
                if (!meshesByMaterial.ContainsKey(material)) {
                    meshesByMaterial[material] = new List<ProBuilderMesh>();
                    targetMeshByMaterial[material] = CreateEmptyTargetPbMesh(material);
                }
                meshesByMaterial[material].Add(pbMesh);
            }

            MeshCollider CombineMeshesForMaterial(Material material) {
                ProBuilderMesh targetPbMesh = targetMeshByMaterial[material];
                var pbMeshesForMaterial = meshesByMaterial[material];
                var combineMeshes = pbMeshesForMaterial.Append(targetPbMesh).ToArray();                
                var resultMeshes = CombineMeshes.Combine(combineMeshes, targetPbMesh);
                if (resultMeshes.Count > 1) {
                    Undo.RevertAllInCurrentGroup();
                    throw new Exception("Multiple result meshes not yet handled");
                }

                Mesh targetMesh = targetPbMesh.GetComponent<MeshFilter>().sharedMesh;
                MeshCollider targetMeshCollider = targetPbMesh.gameObject.GetOrAddComponent<MeshCollider>();
                Undo.RecordObject(targetMeshCollider, "GrowShrinkHallway: Set Combined Mesh Collider");
                targetMeshCollider.sharedMesh = targetMesh;
                
                targetPbMesh.Refresh();

                return targetMeshCollider;
            }

            void ShrinkMeshCollider(MeshCollider meshCollider) {
                Undo.RecordObject(meshCollider, "GrowShrinkHallway: Shrink Combined Mesh Collider");
                Undo.RecordObject(meshCollider.sharedMesh, "GrowShrinkHallway: Shrink Combined Mesh Collider");
                // Adjust mesh vertices
                Mesh mesh = meshCollider.sharedMesh;
                void Transform(Func<Vector3, Vector3> transform) {
                    mesh.vertices = mesh.vertices
                        .Select(meshCollider.transform.TransformPoint)
                        .Select(transform)
                        .Select(meshCollider.transform.InverseTransformPoint)
                        .ToArray();
                    // Force the MeshCollider to refresh the bounds
                    meshCollider.sharedMesh = null;
                    meshCollider.sharedMesh = mesh;
                }
                
                Transform(TransformVertex);
            }
            
            Undo.RecordObjects(pbMeshes.Select(x => x.gameObject).ToArray(), "GrowShrinkHallway");
            foreach (ProBuilderMesh pbMesh in pbMeshes) {
                // Turn off children meshes
                pbMesh.gameObject.SetActive(false);
            }

            // Set up CombineMesh gameObject for the shrunk trigger zone
            GameObject shrunkTriggerZoneGO = Instantiate(originalTriggerZone.gameObject, geometryRoot, false);
            Undo.RegisterCreatedObjectUndo(shrunkTriggerZoneGO, "GrowShrinkHallway: Create Shrunk Trigger Zone");
            shrunkTriggerZoneGO.name = $"{originalTriggerZone.gameObject.name}_Shrunk";
            Undo.RecordObject(shrunkTriggerZoneGO.transform, "GrowShrinkHallway: Set Shrunk Trigger Zone Transform");
            shrunkTriggerZoneGO.transform.SetSiblingIndex(1);
            shrunkTriggerZoneGO.transform.position = originalTriggerZone.transform.position;
            shrunkTriggerZoneGO.transform.rotation = originalTriggerZone.transform.rotation;
            
            // Remove the PB mesh filter because it doesn't seem to allow setting the mesh value
            Undo.DestroyObjectImmediate(shrunkTriggerZoneGO.GetComponent<ProBuilderMesh>());
            
            // Add a normal Unity MeshFilter instead
            MeshFilter shrunkTriggerMeshFilter = shrunkTriggerZoneGO.GetOrAddComponent<MeshFilter>();
            Undo.RegisterCreatedObjectUndo(shrunkTriggerMeshFilter, "GrowShrinkHallway: Create Shrunk Trigger Zone Mesh Filter");
            MeshCollider shrunkTriggerCollider = shrunkTriggerZoneGO.GetOrAddComponent<MeshCollider>();
            Undo.RegisterCreatedObjectUndo(shrunkTriggerCollider, "GrowShrinkHallway: Create Shrunk Trigger Zone Mesh Collider");
            shrunkTriggerZone = shrunkTriggerZoneGO.GetOrAddComponent<GrowShrinkTransitionTrigger>();
            Undo.RegisterCreatedObjectUndo(shrunkTriggerZone, "GrowShrinkHallway: Create Shrunk Trigger Zone Transition Trigger");

            Mesh shrunkTriggerMesh = MeshCopyFromPBMesh(originalTriggerZone.GetComponent<ProBuilderMesh>());
            Undo.RegisterCreatedObjectUndo(shrunkTriggerMesh, "GrowShrinkHallway: Create Shrunk Trigger Zone Mesh");
            shrunkTriggerMesh.name = $"{shrunkTriggerMesh.name}_Shrunk";
            Undo.RecordObject(shrunkTriggerCollider, "GrowShrinkHallway: Set Shrunk Trigger Zone Mesh Collider");
            shrunkTriggerCollider.sharedMesh = shrunkTriggerMesh;
            Undo.RecordObject(shrunkTriggerMeshFilter, "GrowShrinkHallway: Set Shrunk Trigger Zone Mesh Filter");
            shrunkTriggerMeshFilter.sharedMesh = shrunkTriggerMesh;
            
            EffectiveTriggerZone.SetupBoundaries(shrunkTriggerCollider);
            
            ShrinkMeshCollider(shrunkTriggerCollider);
            // Force the MeshCollider to refresh the bounds
            Undo.RecordObject(shrunkTriggerCollider, "GrowShrinkHallway: Refresh Shrunk Trigger Zone Collider");
            shrunkTriggerCollider.ForceRefresh();
            
            Undo.RecordObject(originalTriggerZone.gameObject, "GrowShrinkHallway: Set Original Trigger Zone Inactive");
            originalTriggerZone.gameObject.SetActive(false);
            
            foreach (Material material in meshesByMaterial.Keys) {
                ShrinkMeshCollider(CombineMeshesForMaterial(material));
            }
            targetPbMeshes = targetMeshByMaterial.Values.ToArray();
            
            EffectiveTriggerZone.SetupBoundaries(shrunkTriggerCollider);
            
            Undo.CollapseUndoOperations(compileGroup);
        }

        [Button("Decompile")]
        public void Decompile(bool isPartOfParentOperation = false) {
            if (!isPartOfParentOperation) {
                Undo.SetCurrentGroupName("Decompile GrowShrinkHallway");
            }
            int decompileGroup = Undo.GetCurrentGroup();
            
            if (targetPbMeshes != null) {
                foreach (ProBuilderMesh pbMesh in targetPbMeshes) {
                    Undo.DestroyObjectImmediate(pbMesh.gameObject);
                }
                Undo.RecordObject(this, "GrowShrinkHallway: Remove Combined Mesh");
                targetPbMeshes = null;
            }
            
            if (transform.Find("Geometry Root") is Transform geometryRoot) {
                Undo.DestroyObjectImmediate(geometryRoot.gameObject);
            }

            if (shrunkTriggerZone != null) {
                Undo.DestroyObjectImmediate(this.shrunkTriggerZone.gameObject);
                Undo.RecordObject(this, "GrowShrinkHallway: Remove Shrunk Trigger Zone");
                shrunkTriggerZone = null;
            }
            
            if (PbMeshesNeedToBeSet) {
                Undo.RecordObject(this, "GrowShrinkHallway: Set PB Meshes");
                // Get all meshes from children recursively, skipping this gameObject and the triggerZone gameObject
                pbMeshes = gameObject.GetComponentsInChildrenRecursively<ProBuilderMesh>()
                    .Where(mf => mf.gameObject != this.gameObject && mf.gameObject != originalTriggerZone.gameObject)
                    .ToArray();
            }

            if (pbMeshes != null) {
                Undo.RecordObjects(pbMeshes.Select(x => x.gameObject).ToArray(), "GrowShrinkHallway");
                foreach (var mesh in pbMeshes) {
                    if (mesh == null) continue;
                    mesh.gameObject.SetActive(true);
                }
            }
            Undo.RecordObject(originalTriggerZone.gameObject, "GrowShrinkHallway");
            originalTriggerZone.gameObject.SetActive(true);
            
            if (!isPartOfParentOperation) {
                Undo.CollapseUndoOperations(decompileGroup);
            }
        }
#endif

        protected override void Start() {
            base.Start();

            EffectiveTriggerZone.OnTransitionTrigger += SetLerpValue;
            EffectiveTriggerZone.OnHallwayEnter += ObjectEnter;
            EffectiveTriggerZone.OnHallwayExit += ObjectExit;
        }

        private void ObjectEnter(Collider c, bool enteredSmallSide) {
            string id = GetId(c);
            if (!growShrinkObjects.ContainsKey(id)) {
                GrowShrinkObject growShrinkObj = GrowShrinkObject.collidersAffectedByGrowShrinkObjects.GetOrNull(c);
                if (growShrinkObj == null) return;
                growShrinkObjects[id] = growShrinkObj;
            }

            growShrinkObjects[id].GetOrNull()?.EnteredHallway(this, enteredSmallSide);
        }

        private void ObjectExit(Collider c, bool exitededSmallSide) {
            string id = GetId(c);
            if (!growShrinkObjects.ContainsKey(id)) return;

            growShrinkObjects[id].GetOrNull()?.ExitedHallway(this, exitededSmallSide);
        }

        private void SetLerpValue(Collider c, float t) {
            string id = GetId(c);
            if (!growShrinkObjects.ContainsKey(id)) {
                GrowShrinkObject growShrinkObj = c.GetComponent<GrowShrinkObject>();
                if (growShrinkObj == null) return;
                growShrinkObjects[id] = growShrinkObj;
            }

            debug.LogWarning($"t-value for {id}: {t:F2}");
            growShrinkObjects[id].GetOrNull()?.SetScaleFromHallway(this, t);
        }

#region Saving

        public override void LoadSave(GrowShrinkHallwaySave save) {
            scaleFactor = save.scaleFactor;
            growShrinkObjects = save.growShrinkObjects;
        }

        [Serializable]
        public class GrowShrinkHallwaySave : SaveObject<GrowShrinkHallway> {
            public float scaleFactor;
            public SerializableDictionary<string, SuperspectiveReference<GrowShrinkObject>> growShrinkObjects;

            public GrowShrinkHallwaySave(GrowShrinkHallway script) : base(script) {
                scaleFactor = script.scaleFactor;
                growShrinkObjects = script.growShrinkObjects;
            }
        }

#endregion

        static Mesh MeshCopyFromPBMesh(ProBuilderMesh pbMesh) {
            Mesh mesh = new Mesh();
            mesh.SetVertices(pbMesh.GetVertices().Select(o => new Vector3() {
                x = o.position.x,
                y = o.position.y,
                z = o.position.z,
            }).ToList());
            var indices = new List<int>();
            pbMesh.faces
                .ToList()
                .ForEach(o => indices.AddRange(o.indexes.ToList()));
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}