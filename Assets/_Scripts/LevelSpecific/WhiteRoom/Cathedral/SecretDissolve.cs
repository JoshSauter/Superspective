using PowerTrailMechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
    public class SecretDissolve : MonoBehaviour {
        public PowerTrail powerTrail;
        public DissolveObject dissolveObject;
        public float turnOnAtDistance;

        bool powered => powerTrail.distance > turnOnAtDistance;

        void Update() {
            if (powered && dissolveObject.state == DissolveObject.State.Dematerialized) {
                dissolveObject.Materialize();
            }
            else if (!powered && dissolveObject.state == DissolveObject.State.Materialized) {
                dissolveObject.Dematerialize();
            }
        }
    }
}
