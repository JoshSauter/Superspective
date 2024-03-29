using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Editor {
    public class SwapMaterialsTool : ScriptableWizard {
        public bool swapBothWays = true;
        public bool selectInactive = true;
        private Material _defaultMaterial;
        private Material DefaultMaterial => _defaultMaterial == null ? _defaultMaterial = Resources.Load<Material>("Materials/Unlit/Unlit") : _defaultMaterial;
        private Material _defaultSwapTo;
        private Material DefaultSwapTo => _defaultSwapTo == null ? _defaultSwapTo = Resources.Load<Material>("Materials/Unlit/UnlitBlack") : _defaultSwapTo;
        public Material material;
        public Material swapTo;

        private void OnEnable() {
            material = DefaultMaterial;
            swapTo = DefaultSwapTo;
        }

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
                // If an object is named after the material (e.g. after a merge), rename it to reflect the new material
                if (material1Renderer.gameObject.name.Contains(material.name)) {
                    material1Renderer.gameObject.name = swapTo.name;
                } 
            }

            if (swapBothWays) {
                foreach (var material2Renderer in swapToSelection) {
                    material2Renderer.sharedMaterial = material;
                    // If an object is named after the material (e.g. after a merge), rename it to reflect the new material
                    if (material2Renderer.gameObject.name.Contains(swapTo.name)) {
                        material2Renderer.gameObject.name = material.name;
                    } 
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
