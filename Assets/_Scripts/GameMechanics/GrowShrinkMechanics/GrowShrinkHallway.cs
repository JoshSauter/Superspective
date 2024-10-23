using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using Saving;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace GrowShrink {
    [RequireComponent(typeof(UniqueId))]
    public class GrowShrinkHallway : SaveableObject<GrowShrinkHallway, GrowShrinkHallway.GrowShrinkHallwayNewSave> {
        public float scaleFactor = 4;

        private GrowShrinkTransitionTrigger effectiveTriggerZone => shrunkTriggerZone ? shrunkTriggerZone : originalTriggerZone;
        public GrowShrinkTransitionTrigger originalTriggerZone;
        [ReadOnly]
        public ProBuilderMesh[] pbMeshes;
        [ReadOnly]
        public ProBuilderMesh targetPbMesh;
        [ReadOnly]
        public GrowShrinkTransitionTrigger shrunkTriggerZone;

        private bool PbMeshesNeedToBeSet => true || pbMeshes == null || pbMeshes.Length == 0 || pbMeshes.Any(m => m == null);

        // Objects that are or were in the tunnel at some point
        private Dictionary<string, SerializableReference<GrowShrinkObject>> growShrinkObjects = new Dictionary<string, SerializableReference<GrowShrinkObject>>();

        string GetId(Collider c) => GrowShrinkTransitionTrigger.GetId(c);

        // Note to self: If the results of this look fucked up, check that the pivot points for combined mesh & trigger zone hitbox are the same
        [Button("Compile", EButtonEnableMode.Editor)]
        void Compile() {
            Decompile();

            Vector3 originalScale = transform.localScale;
            transform.localScale = Vector3.one;
#region Vertex Transformation Methods
            Vector3 pivotAxis;
            Vector3 smallSidePointWorld, largeSidePointWorld;

            void SetupTriggerZoneBoundaryFrom(MeshCollider meshCollider) {
                effectiveTriggerZone.SetupBoundaries(meshCollider);
                smallSidePointWorld = effectiveTriggerZone.smallSidePointWorld;
                largeSidePointWorld = effectiveTriggerZone.largeSidePointWorld;
            }
            
            // Takes in a world position and returns the world position closest to it along the pivot line
            Vector3 PivotPoint(Vector3 sample) {
                Vector3 sampleToOrigin = sample - largeSidePointWorld;

                float dot = Vector3.Dot(sampleToOrigin, pivotAxis);
                return largeSidePointWorld + pivotAxis * dot;
            }

            Vector3 TransformVertexShrink(Vector3 worldPos) {
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

            Vector3 TransformVertexIn(Vector3 worldPos) {
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
            if (shrunkTriggerZone != null) {
                DestroyImmediate(this.shrunkTriggerZone.gameObject);
            }
            if (targetPbMesh != null) {
                DestroyImmediate(this.targetPbMesh.gameObject);
            }
            
            // Set up CombineMesh gameObject
            targetPbMesh = ProBuilderMesh.Create();
            targetPbMesh.transform.position = transform.position;
            targetPbMesh.transform.rotation = transform.rotation;
            targetPbMesh.transform.SetParent(transform);
            targetPbMesh.transform.SetSiblingIndex(0);
            targetPbMesh.gameObject.name = $"{gameObject.name}_CombinedMesh";
            
            if (PbMeshesNeedToBeSet) {
                // Get all mesh renderers from children recursively, skipping this gameObject and the triggerZone gameObject
                pbMeshes = gameObject.GetComponentsInChildrenRecursively<ProBuilderMesh>()
                    .Where(mf => mf.gameObject.activeInHierarchy
                                 && mf.gameObject != this.gameObject
                                 && mf.gameObject != originalTriggerZone.gameObject
                                 && mf.gameObject != targetPbMesh.gameObject)
                    .ToArray();
            }

            var combineMeshes = pbMeshes.Append(targetPbMesh).ToArray();

            var resultMeshes = CombineMeshes.Combine(combineMeshes, targetPbMesh);
            if (resultMeshes.Count > 1) {
                Debug.LogError("Multiple result meshes not yet handled");
                return;
            }
            
            for (int i = 0; i < pbMeshes.Length; i++) {
                // Turn off children meshes
                pbMeshes[i].gameObject.SetActive(false);
            }
            
            Mesh targetMesh = targetPbMesh.gameObject.GetComponent<MeshFilter>().sharedMesh;
            MeshCollider targetMeshCollider = targetPbMesh.gameObject.GetOrAddComponent<MeshCollider>();
            targetMeshCollider.sharedMesh = targetMesh;
            
            SetupTriggerZoneBoundaryFrom(targetMeshCollider);
            pivotAxis = (originalTriggerZone.largeSidePointWorld - originalTriggerZone.smallSidePointWorld).normalized;

            void ShrinkMeshCollider(MeshCollider meshCollider) {
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
                    
                    SetupTriggerZoneBoundaryFrom(meshCollider);
                }
                
                Transform(TransformVertexIn);
                Transform(TransformVertexShrink);
            }

            ShrinkMeshCollider(targetMeshCollider);

            // Set up CombineMesh gameObject for the shrunk trigger zone
            GameObject shrunkTriggerZoneGO = Instantiate(originalTriggerZone.gameObject);
            shrunkTriggerZoneGO.name = $"{originalTriggerZone.gameObject.name}_Shrunk";
            shrunkTriggerZoneGO.transform.SetParent(transform);
            shrunkTriggerZoneGO.transform.SetSiblingIndex(1);
            shrunkTriggerZoneGO.transform.position = originalTriggerZone.transform.position;
            shrunkTriggerZoneGO.transform.rotation = originalTriggerZone.transform.rotation;
            // Remove the PB mesh filter because it doesn't seem to allow setting the mesh value
            DestroyImmediate(shrunkTriggerZoneGO.GetComponent<ProBuilderMesh>());
            // Add a normal Unity MeshFilter instead
            MeshFilter shrunkTriggerMeshFilter = shrunkTriggerZoneGO.GetOrAddComponent<MeshFilter>();
            MeshCollider shrunkTriggerCollider = shrunkTriggerZoneGO.GetOrAddComponent<MeshCollider>();
            shrunkTriggerZone = shrunkTriggerZoneGO.GetOrAddComponent<GrowShrinkTransitionTrigger>();

            Mesh shrunkTriggerMesh = MeshCopyFromPBMesh(originalTriggerZone.GetComponent<ProBuilderMesh>());
            shrunkTriggerMesh.name = $"{shrunkTriggerMesh.name}_Shrunk";
            shrunkTriggerCollider.sharedMesh = shrunkTriggerMesh;
            shrunkTriggerMeshFilter.sharedMesh = shrunkTriggerMesh;
            SetupTriggerZoneBoundaryFrom(shrunkTriggerCollider);
            
            ShrinkMeshCollider(shrunkTriggerCollider);
            // Force the MeshCollider to refresh the bounds
            shrunkTriggerCollider.ForceRefresh();
            
            originalTriggerZone.gameObject.SetActive(false);
            
            transform.localScale = originalScale;
            
            targetPbMesh.Refresh();
        }

        [Button("Decompile", EButtonEnableMode.Editor)]
        public void Decompile() {
            if (targetPbMesh != null) {
                DestroyImmediate(this.targetPbMesh.gameObject);
                targetPbMesh = null;
            }

            if (shrunkTriggerZone != null) {
                DestroyImmediate(this.shrunkTriggerZone.gameObject);
                shrunkTriggerZone = null;
            }
            
            if (PbMeshesNeedToBeSet) {
                // Get all meshes from children recursively, skipping this gameObject and the triggerZone gameObject
                pbMeshes = gameObject.GetComponentsInChildrenRecursively<ProBuilderMesh>()
                    .Where(mf => mf.gameObject != this.gameObject && mf.gameObject != originalTriggerZone.gameObject)
                    .ToArray();
            }

            if (pbMeshes != null) {
                foreach (var mesh in pbMeshes) {
                    if (mesh == null) continue;
                    mesh.gameObject.SetActive(true);
                }
            }
            originalTriggerZone.gameObject.SetActive(true);
        }

        protected override void Start() {
            base.Start();

            effectiveTriggerZone.OnTransitionTrigger += SetLerpValue;
            effectiveTriggerZone.OnHallwayEnter += ObjectEnter;
            effectiveTriggerZone.OnHallwayExit += ObjectExit;
        }

        private void ObjectEnter(Collider c, bool enteredSmallSide) {
            string id = GetId(c);
            if (!growShrinkObjects.ContainsKey(id)) {
                GrowShrinkObject growShrinkObj = c.GetComponent<GrowShrinkObject>();
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

        [Serializable]
        public class GrowShrinkHallwayNewSave : SerializableSaveObject<GrowShrinkHallway> {
            private float scaleFactor;
            private SerializableDictionary<string, SerializableReference<GrowShrinkObject>> growShrinkObjects;

            public GrowShrinkHallwayNewSave(GrowShrinkHallway script) : base(script) {
                scaleFactor = script.scaleFactor;
                growShrinkObjects = script.growShrinkObjects;
            }

            public override void LoadSave(GrowShrinkHallway script) {
                script.growShrinkObjects = this.growShrinkObjects;
                script.scaleFactor = this.scaleFactor;
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