using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicTriggerMechanics {
    public class CompositeMagicTrigger : MagicTrigger {
        public CompositeMagicTriggerPiece[] triggerColliders;
        public bool hasTriggeredEnterThisFrame = false;
        public bool hasTriggeredExitThisFrame = false;
        public bool hasTriggeredStayThisFrame = false;

        protected override void Awake() {
            base.Awake();
            Collider[] colliders = transform.GetComponentsInChildren<Collider>();
            triggerColliders = colliders.Select(c => c.gameObject.AddComponent<CompositeMagicTriggerPiece>()).ToArray();
            
            foreach (var trigger in triggerColliders) {
                trigger.compositeTrigger = this;
            }
        }

        public void OnAnyTriggerEnter(Collider other) {
            if (hasTriggeredEnterThisFrame) return;
            hasTriggeredEnterThisFrame = true;

            OnTriggerEnter(other);
        }

        public void OnAnyTriggerExit(Collider other) {
            if (hasTriggeredExitThisFrame) return;
            hasTriggeredExitThisFrame = true;

            OnTriggerExit(other);
        }

        public void OnAnyTriggerStay(Collider other) {
            if (hasTriggeredStayThisFrame) return;
            hasTriggeredStayThisFrame = true;

            OnTriggerStay(other);
        }

        private void LateUpdate() {
            hasTriggeredEnterThisFrame = false;
            hasTriggeredExitThisFrame = false;
            hasTriggeredStayThisFrame = false;
        }
    }
}