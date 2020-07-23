using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicTriggerMechanics {
    public class CompositeMagicTriggerPiece : MonoBehaviour {
        public CompositeMagicTrigger compositeTrigger;

        private void OnTriggerEnter(Collider other) {
            compositeTrigger.OnAnyTriggerEnter(other);
        }

        private void OnTriggerExit(Collider other) {
            compositeTrigger.OnAnyTriggerExit(other);
        }

        private void OnTriggerStay(Collider other) {
            compositeTrigger.OnAnyTriggerStay(other);
        }
    }
}