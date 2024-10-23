using PowerTrailMechanics;
using System.Collections;
using System.Collections.Generic;
using DissolveObjects;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
    public class SecretDissolve : MonoBehaviour {
        public PowerTrail powerTrail;
        public DissolveObject dissolveObject;
        public float turnOnAtDistance;

        bool powered => powerTrail.distance > turnOnAtDistance;

        void Update() {
            if (powered && dissolveObject.stateMachine == DissolveObject.State.Dematerialized) {
                dissolveObject.Materialize();
            }
            else if (!powered && dissolveObject.stateMachine == DissolveObject.State.Materialized) {
                dissolveObject.Dematerialize();
            }
        }
    }
}
