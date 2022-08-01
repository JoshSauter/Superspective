using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Editor {
    public class SwapMaterialsTool : ScriptableWizard {
        public bool swapBothWays = false;
        public bool selectInactive = true;
        [FormerlySerializedAs("material1")] public Material material;
        [FormerlySerializedAs("material2")] public Material swapTo;

        void OnWizardCreate() {
            List<Renderer> materialSelection = new List<Renderer>();
            List<Renderer> swapToSelection = new List<Renderer>();
            foreach (GameObject go in Selection.gameObjects) {
                SelectAllChildrenRecusivelyWithMaterial(go, material, ref materialSelection);
                if (swapBothWays) {
                    SelectAllChildrenRecusivelyWithMaterial(go, swapTo, ref swapToSelection);
                }
            }

            foreach (var material1Renderer in materialSelection) {
                material1Renderer.sharedMaterial = swapTo;
            }

            if (swapBothWays) {
                foreach (var material2Renderer in swapToSelection) {
                    material2Renderer.sharedMaterial = material;
                }
            }
        }

        [MenuItem("My Tools/Swap Materials Recursively")]
        static void SelectAllChildren() {
            if (Application.isPlaying) return; // Only allow this while in edit mode

            DisplayWizard<SwapMaterialsTool>(
                "Select All Children Recursively",
                "Select All With Matching Material"
            );
        }

        public void SelectAllChildrenRecusivelyWithMaterial(GameObject curNode, Material material, ref List<Renderer> selectionSoFar) {
            bool containsMaterial = false;
            if (curNode.TryGetComponent(out Renderer renderer)) {
                containsMaterial = renderer.sharedMaterials.ToList().Exists(m => m.name.StripSuffix(" (Instance)") == material.name);
            }
            
            if (containsMaterial) selectionSoFar.Add(renderer);

            foreach (Transform child in curNode.transform.GetComponentsInChildren<Transform>(selectInactive)) {
                if (child.gameObject != curNode) SelectAllChildrenRecusivelyWithMaterial(child.gameObject, material, ref selectionSoFar);
            }
        }
    }
}
