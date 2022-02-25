using System;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    public class FloorIcons : MonoBehaviour {
        public FloorManager.Floor floor;
        public SpriteRenderer[] icons;

        private float colorLerpSpeed = 4.5f;

        private void OnValidate() {
            icons = transform.GetComponentsInChildrenRecursively<SpriteRenderer>();
        }

        private void Update() {
            float desiredAlpha = FloorManager.instance.floor == floor ? 0 : 1;
            foreach (var icon in icons) {
                Color curColor = icon.color;
                curColor.a = Mathf.Lerp(curColor.a, desiredAlpha, colorLerpSpeed * Time.deltaTime);
                icon.color = curColor;
            }
        }
    }
}
