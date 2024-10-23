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
        "MeshRenderer",
        "BoxCollider"       // Used for double-sided colliders for plane objects
    };

    private const string MERGED_ROOT_NAME = "Merged";
    private const string UNMERGED_ROOT_NAME = "Unmerged";
    private const string DUPLICATES_ROOT_NAME = "Temporary Duplicate of Unmerged";

    [MenuItem("GameObject/Unmerge ProBuilder Objects %#m")] // // % = Ctrl, # = Shift M
    public static void UnmergeProBuilderMeshes() {
        static void UnmergeProBuilderMeshesInRoot(Transform root) {
            // foreach (Transform child in root) {
            //     UnmergeProBuilderMeshesInRoot(child);
            // }
            if (!IsRootOfMergeableObject(root, false) || !root.gameObject.activeSelf) return;
            
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
            MergeProBuilderMeshesByMaterialUnderRootWithUndo(selectedRoot);
        }
    }

    private static bool IsRootOfMergeableObject(Transform objectInQuestion, bool onlyConsiderUnmergedRoots = true) {
        // Cannot merge prefabs since they don't allow children moving around
        if (PrefabUtility.GetPrefabAssetType(objectInQuestion.gameObject) != PrefabAssetType.NotAPrefab) return false;
        
        bool isJustEmptyTransform = objectInQuestion.GetComponents<Component>().Length == 1;
        bool hasUnmergedRoot = objectInQuestion.Find(UNMERGED_ROOT_NAME);
        bool hasMergedRoot = objectInQuestion.Find(MERGED_ROOT_NAME);
        bool hasPbMeshesInChildren = objectInQuestion.GetComponentsInChildrenRecursively<PBMesh>().Length > 0;
        // Don't treat "Unmerged" and "Merged" objects created by this tool as root objects (their parent would be though)
        bool isNotNamedUnmergedOrMerged = objectInQuestion.name != MERGED_ROOT_NAME && objectInQuestion.name != UNMERGED_ROOT_NAME;
        
        bool isNotAlreadyMerged = !hasMergedRoot;

        return isNotNamedUnmergedOrMerged && hasPbMeshesInChildren && (isJustEmptyTransform || hasMergedRoot || hasUnmergedRoot) && (!onlyConsiderUnmergedRoots || isNotAlreadyMerged);
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

    private static bool IsValidRoot(Transform maybeRoot) {
        if (maybeRoot.GetComponent<PBMesh>()) {
            Debug.LogError($"Cannot merge {maybeRoot.gameObject.FullPath()}, root not allowed to have ProBuilderMesh!");
            return false;
        }

        return true;
    }

    private static void MergeProBuilderMeshesByMaterialUnderRootWithUndo(Transform root) {
        if (!IsValidRoot(root)) {
            return;
        }

        // Step 1: Find all PBMeshes under the root, or unmerged root if it exists
        Transform searchFrom = root;
        Transform preexistingUnmergedRoot = root.Find(UNMERGED_ROOT_NAME);
        if (preexistingUnmergedRoot) {
            preexistingUnmergedRoot.gameObject.SetActive(true);
            searchFrom = preexistingUnmergedRoot;
        }
        Dictionary<Material, List<PBMesh>> pbMeshesByMaterial = GetPBMeshesByMaterialInSelection(searchFrom, true);
        
        // Step 2: If we need to, create the unmerged root
        Transform unmergedRoot = GetOrCreateChild(root, UNMERGED_ROOT_NAME);
        
        // Step 3: If we need to, move all the existing direct children of the root to the unmerged root
        if (!preexistingUnmergedRoot) {
            List<Transform> children = root.GetChildren();
            foreach (Transform child in children) {
                if (child.name is UNMERGED_ROOT_NAME or MERGED_ROOT_NAME or DUPLICATES_ROOT_NAME) continue;
                
                //Debug.Log($"Setting {child.name} to unmerged root");
                child.SetParent(unmergedRoot);
            }
        }
        
        // Step 4: If we need to, delete and recreate the merged root
        Transform preexistingMergedRoot = root.Find(MERGED_ROOT_NAME);
        if (preexistingMergedRoot) {
            Object.DestroyImmediate(preexistingMergedRoot.gameObject);
        }
        
        // Step 5: Create the new merged root
        Transform mergedRoot = GetOrCreateChild(root, MERGED_ROOT_NAME);
        
        // Step 6: If we need to, delete and recreate the duplicates root
        Transform preexistingDuplicatesRoot = root.Find(DUPLICATES_ROOT_NAME);
        if (preexistingDuplicatesRoot) {
            Object.DestroyImmediate(preexistingDuplicatesRoot.gameObject);
        }
        Transform duplicatesRoot = GetOrCreateChild(root, DUPLICATES_ROOT_NAME);
        
        // Step 7: Duplicate all of the PBMeshes and move them to the duplicates root
        var duplicatedPbMeshesByMaterial = pbMeshesByMaterial.Select(kv => {
            Material material = kv.Key;
            List<PBMesh> pbMeshes = kv.Value;
            List<PBMesh> duplicatedPbMeshes = pbMeshes.Select(originalPbMesh => {
                PBMesh duplicate = GameObject.Instantiate(originalPbMesh, duplicatesRoot);
                duplicate.transform.position = originalPbMesh.transform.position;
                duplicate.transform.rotation = originalPbMesh.transform.rotation;
                duplicate.transform.localScale = duplicate.transform.LossyToLocalScale(originalPbMesh.transform.lossyScale);
                duplicate.gameObject.name = originalPbMesh.gameObject.FullPath();
                
                if (PrefabUtility.GetPrefabAssetType(duplicate.gameObject) != PrefabAssetType.NotAPrefab) {
                    PrefabUtility.UnpackPrefabInstance(duplicate.gameObject, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                }

                duplicate.transform.SetParent(duplicatesRoot);
                return duplicate;
            }).ToList();

            return new KeyValuePair<Material, List<PBMesh>>(material, duplicatedPbMeshes);
        }).ToDictionary();
        
        // Step 8: Combine duplicated meshes by material and move them to the merged root
        foreach (var material in duplicatedPbMeshesByMaterial.Keys) {
            string materialName = material.name;

            // Use the first PBMesh in the List as the merged mesh
            Transform materialRoot = duplicatedPbMeshesByMaterial[material][0].transform;
            materialRoot.gameObject.name = $"{root.name}_{materialName}";
            materialRoot.SetParent(mergedRoot);
            PBMesh materialRootPbMesh = materialRoot.GetOrAddComponent<PBMesh>();

            // Combine the meshes and update the pivot
            CombineMeshes.Combine(duplicatedPbMeshesByMaterial[material], materialRootPbMesh);
            materialRootPbMesh.SetPivot(mergedRoot.position);
        }
        
        // Step 9: Delete the duplicates root
        Object.DestroyImmediate(duplicatesRoot.gameObject);
        
        // Step 10: Deactivate the unmerged root
        unmergedRoot.gameObject.SetActive(false);
        
        Debug.Log($"Merged {pbMeshesByMaterial.Values.ToList().SelectMany(pbMeshes => pbMeshes).ToList().Count} ProBuilderMeshes into {pbMeshesByMaterial.Keys.Count}.");
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
                return pbMesh.gameObject.activeInHierarchy || (parentIsUnmergedRoot && pbMesh.gameObject.activeSelf);
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
