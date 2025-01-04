using UnityEngine;

namespace MagicTriggerMechanics {
    public class CompositeMagicTriggerPiece : MonoBehaviour {
        public CompositeMagicTrigger compositeTrigger;

        void OnTriggerEnter(Collider other) {
            compositeTrigger.OnAnyTriggerEnter(other);
        }

        void OnTriggerExit(Collider other) {
            compositeTrigger.OnAnyTriggerExit(other);
        }

        void OnTriggerStay(Collider other) {
            compositeTrigger.OnAnyTriggerStay(other);
        }
    }
}