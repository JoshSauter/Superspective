using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperspectiveUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using PBMesh = UnityEngine.ProBuilder.ProBuilderMesh;

/// <summary>
/// Merges all ProBuilder meshes into a single separate GameObject, one per unique Material.
/// Inactive GameObjects under the selection will be ignored in this process but moved to the "Unmerged" parent.
/// Objects with > 1 Materials will also be ignored and logged so the user can separate them out by Material first.
/// Input Structure:
/// ─ Selected Root GameObject(s)
///   ┕ Mesh1 (Unlit)
///   ┕ Mesh2 (Unlit)
///   ┕ Mesh3 (Glass)
///   ┕ Mesh3Bad (Glass but inactive)
///   ┕ Mesh4 (UnlitBlack)
///   ┕ Mesh5 (UnlitBlack)
///   ┕ Mesh6 (UnlitBlack)
///
/// Output Structure:
/// ─ Selected Root GameObject(s)
///   ┕ Unmerged (Inactive)
///     ┕ Mesh1 (Unlit)
///     ┕ Mesh2 (Unlit)
///     ┕ Mesh3 (Glass)
///     ┕ Mesh3Bad (Glass but inactive)
///     ┕ Mesh4 (UnlitBlack)
///     ┕ Mesh5 (UnlitBlack)
///     ┕ Mesh6 (UnlitBlack)
///   ┕ Merged
///     ┕ Unlit
///     ┕ Glass
///     ┕ UnlitBlack
/// </summary>
public static class MergeProBuilderMeshesRecursivelyTool {
    private static readonly HashSet<string> ALLOWED_COMPONENT_NAMES = new HashSet<string>() {
        "Transform",
        "ProBuilderMesh",
        "ProBuilderShape",
        "MeshFilter",
        "MeshCollider",
        "MeshRenderer"
    };

    private const string MERGED_ROOT_NAME = "Merged";
    private const string UNMERGED_ROOT_NAME = "Unmerged";

    [MenuItem("GameObject/Unmerge ProBuilder Objects %#m")] // // % = Ctrl, # = Shift M
    public static void UnmergeProBuilderMeshes() {
        static void UnmergeProBuilderMeshesInRoot(Transform root) {
            foreach (Transform child in root) {
                UnmergeProBuilderMeshesInRoot(child);
            }
            if (!IsRootOfMergeableObject(root) || !root.gameObject.activeSelf) return;
            
            Transform unmergedRoot = root.transform.Find(UNMERGED_ROOT_NAME);
            Transform mergedRoot = root.transform.Find(MERGED_ROOT_NAME);
            
            if (unmergedRoot != null) {
                unmergedRoot.gameObject.SetActive(true);
            }
            if (mergedRoot != null) {
                Object.DestroyImmediate(mergedRoot.gameObject);
            }
        }
        
        foreach (Transform selectedRoot in Selection.GetFiltered<Transform>(SelectionMode.Editable)) {
            UnmergeProBuilderMeshesInRoot(selectedRoot);
        }
    }
    
    [MenuItem("GameObject/Merge ProBuilder Objects By Material %m")] // % = Ctrl M
    public static void MergeProBuilderMeshesByMaterial () {
        foreach (Transform selectedRoot in Selection.GetFiltered<Transform>(SelectionMode.Editable)) {
            MergeProBuilderMeshesByMaterialUnderRoot(selectedRoot);
        }
    }

    private static bool IsRootOfMergeableObject(Transform objectInQuestion) {
        bool isJustEmptyTransform = objectInQuestion.GetComponents<Component>().Length == 1;
        bool hasUnmergedRoot = objectInQuestion.Find(UNMERGED_ROOT_NAME);
        bool hasMergedRoot = objectInQuestion.Find(MERGED_ROOT_NAME);
        bool hasPbMeshesInChildren = objectInQuestion.GetComponentsInChildrenRecursively<PBMesh>().Length > 0;
        // Don't treat "Unmerged" and "Merged" objects created by this tool as root objects (their parent would be though)
        bool isNotNamedUnmergedOrMerged = objectInQuestion.name != MERGED_ROOT_NAME && objectInQuestion.name != UNMERGED_ROOT_NAME;

        return isNotNamedUnmergedOrMerged && hasPbMeshesInChildren && (isJustEmptyTransform || hasMergedRoot || hasUnmergedRoot);
    }

    private static Transform GetOrCreateChild(Transform t, string name) {
        if (t.Find(name) == null) {
            var o = new GameObject(name) {
                name = name // Gets rid of the (clone) suffix
            };
            o.transform.SetParent(t, false);
        }
        return t.Find(name);
    }

    private static void MergeProBuilderMeshesByMaterialUnderRoot(Transform root) {
        foreach (Transform child in root) {
            MergeProBuilderMeshesByMaterialUnderRoot(child);
        }
        if (!IsRootOfMergeableObject(root) || !root.gameObject.activeSelf) return;
        
        Dictionary<Material, List<PBMesh>> allPbMeshesByMaterial = GetPBMeshesByMaterialInSelection(root, true);
        List<Transform> allChildren = root.GetComponentsInChildrenOnly<Transform>().ToList();

        Transform unmergedRoot = GetOrCreateChild(root, UNMERGED_ROOT_NAME);
        Transform mergedRoot = GetOrCreateChild(root, MERGED_ROOT_NAME);
        unmergedRoot.SetSiblingIndex(0);
        mergedRoot.SetSiblingIndex(1);
        
        foreach (Transform child in root) {
            if (child.name == UNMERGED_ROOT_NAME || child.name == MERGED_ROOT_NAME) continue;
            
            child.SetParent(unmergedRoot.transform);
        }
        
        Transform unmergedRootDuplicate = GameObject.Instantiate(unmergedRoot, root.transform);
        unmergedRootDuplicate.name = "Temporary Duplicate of Unmerged";
        
        Dictionary<Material, List<PBMesh>> duplicatedPbMeshesToBeMerged = GetPBMeshesByMaterialInSelection(unmergedRootDuplicate);
        foreach (var material in duplicatedPbMeshesToBeMerged.Keys) {
            string materialName = material.name;
            if (mergedRoot.Find(materialName)) {
                Object.DestroyImmediate(mergedRoot.Find(materialName).gameObject);
            }
        
            // For some reason cannot seem to programmatically create a new ProBuilderMesh without errors,
            // so we just use the first PBMesh in the List as the mergedTo mesh
            Transform materialRoot = duplicatedPbMeshesToBeMerged[material][0].transform;
            materialRoot.gameObject.name = materialName;
            materialRoot.SetParent(mergedRoot);
            PBMesh materialRootPbMesh = materialRoot.GetOrAddComponent<PBMesh>();
            CombineMeshes.Combine(duplicatedPbMeshesToBeMerged[material], materialRootPbMesh);
            materialRootPbMesh.SetPivot(mergedRoot.position);
        }
        
        unmergedRoot.gameObject.SetActive(false);
        Object.DestroyImmediate(unmergedRootDuplicate.gameObject);
        
        Debug.Log($"Merged {allPbMeshesByMaterial.Values.ToList().SelectMany(pbMeshes => pbMeshes).ToList().Count} ProBuilderMeshes into {allPbMeshesByMaterial.Keys.Count}.");
    }

    private static Dictionary<Material, List<PBMesh>> GetPBMeshesByMaterialInSelection(Transform selection, bool debugLog = false) {
        List<PBMesh> allPbMeshes = selection
            .GetComponentsInChildrenRecursively<PBMesh>()
            .Where(pbMesh => {
                // Filter out and log objects with multiple Materials
                bool hasMultipleMaterials = pbMesh.GetComponent<Renderer>().sharedMaterials.Length > 1;
                if (hasMultipleMaterials) {
                    if (debugLog) Debug.LogWarning($"{pbMesh.FullPath()} has more than one Material. Please separate out into separate ProBuilder meshes per Material to include.");
                    return false;
                }

                // Transform, ProBuilderMesh, MeshRenderer, MeshFilter, and MeshCollider are the only Components allowed
                // (Don't want to merge objects that have game-relevant scripts attached)
                List<string> allComponentTypes = pbMesh.GetComponents<Component>().Select(c => c.GetType().Name).ToList();
                bool hasInvalidComponent = allComponentTypes.Exists(cName => !ALLOWED_COMPONENT_NAMES.Contains(cName));
                if (hasInvalidComponent) {
                    if (debugLog && pbMesh.gameObject.activeSelf) {
                        Debug.LogWarning($"{pbMesh.FullPath()} has non-ProBuilder Components: {string.Join(", ", allComponentTypes.Where(cName => !ALLOWED_COMPONENT_NAMES.Contains(cName)))}. Is this a game-relevant object? Skipping...");
                    }
                    return false;
                }

                Transform pbMeshParent = pbMesh.transform.parent;
                bool parentIsMergedRoot = pbMeshParent != null && pbMeshParent.name == MERGED_ROOT_NAME && pbMeshParent.parent == selection;
                // Filter out already Merged
                if (parentIsMergedRoot) {
                    return false;
                }

                bool parentIsUnmergedRoot = pbMeshParent != null && pbMeshParent.name == UNMERGED_ROOT_NAME && pbMeshParent.parent == selection;
                return pbMesh.gameObject.activeInHierarchy || (parentIsUnmergedRoot && !pbMeshParent.gameObject.activeSelf && pbMesh.gameObject.activeSelf);
            })
            .ToList();

        Dictionary<Material, List<PBMesh>> allPbMeshesByMaterial = new Dictionary<Material, List<PBMesh>>();
        foreach (var pbMesh in allPbMeshes) {
            Material material = pbMesh.GetComponent<Renderer>().sharedMaterial;
            if (!allPbMeshesByMaterial.ContainsKey(material)) {
                allPbMeshesByMaterial.Add(material, new List<PBMesh>());
            }
                
            allPbMeshesByMaterial[material].Add(pbMesh);
        }

        return allPbMeshesByMaterial;
    }

    private static void DebugLogHierarchy(Transform fromRoot) {
        static void PrintHierarchyRecursive(Transform currentTransform, int depth, StringBuilder stringBuilder) {
            if (currentTransform.gameObject.activeInHierarchy) {
                // Append the name of the current object with proper indentation
                stringBuilder.AppendLine($"{new string(' ', depth * 4)}{currentTransform.name}");
            }

            // Recursively append the children
            foreach (Transform child in currentTransform) {
                PrintHierarchyRecursive(child, depth + 1, stringBuilder);
            }
        }
        
        StringBuilder hierarchyStringBuilder = new StringBuilder();
        PrintHierarchyRecursive(fromRoot, 0, hierarchyStringBuilder);

        // Output the entire hierarchy in a single Debug.Log statement
        Debug.Log(hierarchyStringBuilder.ToString());
    }
}
