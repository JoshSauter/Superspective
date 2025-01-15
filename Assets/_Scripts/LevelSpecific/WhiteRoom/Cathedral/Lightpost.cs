using PowerTrailMechanics;
using System.Collections;
using System.Collections.Generic;
using PoweredObjects;
using Sirenix.OdinInspector;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
    public class Lightpost : MonoBehaviour {
        [ShowIf(nameof(DistanceMode))]
        public PowerTrail powerTrail;
        [HideIf(nameof(DistanceMode))]
        public PoweredObject powerSource;
        
        [ColorUsage(true, true)]
        public Color emissiveColor;
        Color startEmission;

        public enum TriggerMode : byte {
            Distance,
            PowerFinish
        }
        public TriggerMode triggerMode;
        private bool DistanceMode => triggerMode == TriggerMode.Distance;
        [ShowIf(nameof(DistanceMode))]
        public float turnOnAtDistance;

        [ShowInInspector, ReadOnly]
        float t = 0f;
        float turnOnSpeed = 4f;
        SuperspectiveRenderer r;

        const string emissionColorKey = "_EmissionColor";

        bool powered {
            get {
                switch (triggerMode) {
                    case TriggerMode.Distance:
                        return powerTrail.distance > turnOnAtDistance;
                    case TriggerMode.PowerFinish:
                        return powerSource.FullyPowered;
                    default:
                        return false;
                }
            }
        }

        IEnumerator Start() {
            r = this.GetOrAddComponent<SuperspectiveRenderer>();

            yield return null;
            startEmission = r.GetColor(emissionColorKey);
        }

        void Update() {
            if (powered) {
                float delta = Mathf.Clamp01(t + Time.deltaTime * turnOnSpeed) - t;
                if (delta > 0) {
                    t += delta;
                    r.SetColor(emissionColorKey, Color.Lerp(startEmission, emissiveColor, t));
                }
            }
            else {
                float delta = Mathf.Clamp01(t - Time.deltaTime * turnOnSpeed) - t;
                if (delta < 0) {
                    t += delta;
                    r.SetColor(emissionColorKey, Color.Lerp(startEmission, emissiveColor, t));
                }
            }
        }
    }
}