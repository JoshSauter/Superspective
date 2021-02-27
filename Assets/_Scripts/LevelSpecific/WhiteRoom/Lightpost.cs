using PowerTrailMechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
    public class Lightpost : MonoBehaviour {
        public PowerTrail powerTrail;
        [ColorUsage(true, true)]
        public Color emissiveColor;
        Color startEmission;
        public float turnOnAtDistance;

        float t = 0f;
        float turnOnSpeed = 4f;
        SuperspectiveRenderer r;

        const string emissionColorKey = "_EmissionColor";

        bool powered => powerTrail.distance > turnOnAtDistance;

        IEnumerator Start() {
            r = GetComponent<SuperspectiveRenderer>();
            if (r == null) {
                r = gameObject.AddComponent<SuperspectiveRenderer>();
            }

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