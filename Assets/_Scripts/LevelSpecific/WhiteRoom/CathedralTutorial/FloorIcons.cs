using System;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    public class FloorIcons : MonoBehaviour {
        public FloorManager.Floor floor;
        public SpriteRenderer[] icons;

        private float colorLerpSpeed = 4.5f;
        private float revealDelay = 1.25f;

        private void OnValidate() {
            icons = transform.GetComponentsInChildrenRecursively<SpriteRenderer>();
        }

        private void Update() {
            StateMachine<FloorManager.Floor> currentFloorState = FloorManager.instance.floor;
            float desiredAlpha = currentFloorState == floor && currentFloorState.Time > revealDelay ? 1 : 0;
            foreach (var icon in icons) {
                Color curColor = icon.color;
                curColor.a = Mathf.Lerp(curColor.a, desiredAlpha, colorLerpSpeed * Time.deltaTime);
                icon.color = curColor;
            }
        }
    }
}
