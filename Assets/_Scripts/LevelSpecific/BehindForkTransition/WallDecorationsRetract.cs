using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.BehindForkTransition {
    public class WallDecorationsRetract : MonoBehaviour {
        public ViewLockObject painting;

        void Update() {
            if (painting.state == PlayerLook.State.ViewLocking) {
                float timeSinceViewLockingStart = PlayerLook.instance.timeSinceStateChange;

                float t = Mathf.Clamp01(timeSinceViewLockingStart / (painting.viewLockTime / 2f));
                Vector3 scale = transform.localScale;
                scale.z = 1 - 0.5f * t;
                transform.localScale = scale;
            }
            else {
                transform.localScale = Vector3.one;
            }
        }
    }
}