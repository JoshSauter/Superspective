using System;
using Saving;
using SerializableClasses;
using UnityEngine;

namespace LevelSpecific.BehindForkTransition {
    public class WallDecorationsRetract : SuperspectiveObject<WallDecorationsRetract, WallDecorationsRetract.WallDecorationsRetractSave> {
        public ViewLockObject painting;

        void Update() {
            if (painting.state == PlayerLook.ViewLockState.ViewLocking) {
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

        [Serializable]
        public class WallDecorationsRetractSave : SaveObject<WallDecorationsRetract> {
            public SerializableVector3 localScale;

            public WallDecorationsRetractSave(WallDecorationsRetract script) : base(script) {
                localScale = script.transform.localScale;
            }
        }
        
        public override void LoadSave(WallDecorationsRetractSave save) {
            transform.localScale = save.localScale;
        }
    }
}
