using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Editor {
    namespace Editor {
        public class SelectAllChildrenWithMultipleMaterialsTool : ScriptableWizard {
            public bool selectInactive = true;

            void OnWizardCreate() {
                List<GameObject> newSelection = new List<GameObject>();
                foreach (GameObject go in Selection.gameObjects) {
                    SelectAllChildrenRecursivelyWithMultipleMaterials(go, ref newSelection);
                }

                Selection.objects = newSelection.ToArray();
                Debug.Log($"{Selection.count} objects found with more than one material.");
            }

            [MenuItem("My Tools/Selection/Select All Children With Multiple Materials Recursively")]
            static void SelectAllChildren() {
                DisplayWizard<SelectAllChildrenWithMultipleMaterialsTool>(
                    "Select All Children Recursively",
                    "Select All With Multiple Materials"
                );
            }

            public void SelectAllChildrenRecursivelyWithMultipleMaterials(GameObject curNode, ref List<GameObject> selectionSoFar) {
                if (curNode.TryGetComponent(out Renderer renderer) && renderer.sharedMaterials != null) {
                    if (renderer.sharedMaterials.Length > 1) {
                        selectionSoFar.Add(curNode);
                    }
                }

                foreach (Transform child in curNode.transform.GetComponentsInChildren<Transform>(selectInactive)) {
                    if (child.gameObject != curNode) {
                        SelectAllChildrenRecursivelyWithMultipleMaterials(child.gameObject, ref selectionSoFar);
                    }
                }
            }
        }
    }

}