using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor {
    public class SelectAllChildrenWithMaterialTool : ScriptableWizard {
        public bool selectInactive = true;
        public Material material;

        void OnWizardCreate() {
            List<GameObject> newSelection = new List<GameObject>();
            foreach (GameObject go in Selection.gameObjects) {
                SelectAllChildrenRecusivelyWithMaterial(go, ref newSelection);
            }

            Selection.objects = newSelection.ToArray();
        }

        [MenuItem("My Tools/Select All Children With Material Recursively")]
        static void SelectAllChildren() {
            DisplayWizard<SelectAllChildrenWithMaterialTool>(
                "Select All Children Recursively",
                "Select All With Matching Material"
            );
        }

        public void SelectAllChildrenRecusivelyWithMaterial(GameObject curNode, ref List<GameObject> selectionSoFar) {
            bool containsMaterial = false;
            if (curNode.TryGetComponent(out Renderer renderer)) {
                containsMaterial = renderer.sharedMaterials.ToList().Contains(material);
            }
            
            if (containsMaterial) selectionSoFar.Add(curNode);

            foreach (Transform child in curNode.transform.GetComponentsInChildren<Transform>(selectInactive)) {
                if (child.gameObject != curNode) SelectAllChildrenRecusivelyWithMaterial(child.gameObject, ref selectionSoFar);
            }
        }
    }
}