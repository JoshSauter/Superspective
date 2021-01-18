using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicTriggerMechanics {
    public class CompositeMagicTrigger : MagicTrigger {
        public CompositeMagicTriggerPiece[] triggerColliders;
        public HashSet<Collider> hasTriggeredEnterThisFrameForCollider = new HashSet<Collider>();
        public HashSet<Collider> hasTriggeredExitThisFrameForCollider = new HashSet<Collider>();
        public HashSet<Collider> hasTriggeredStayThisFrameForCollider = new HashSet<Collider>();

        protected override void Awake() {
            base.Awake();
            Collider[] colliders = transform.GetComponentsInChildren<Collider>();
            triggerColliders = colliders.Select(c => c.gameObject.AddComponent<CompositeMagicTriggerPiece>()).ToArray();
            
            foreach (var trigger in triggerColliders) {
                trigger.compositeTrigger = this;
            }
        }

        public void OnAnyTriggerEnter(Collider other) {
            if (hasTriggeredEnterThisFrameForCollider.Contains(other)) return;
            hasTriggeredEnterThisFrameForCollider.Add(other);

            OnTriggerEnter(other);
        }

        public void OnAnyTriggerExit(Collider other) {
            if (hasTriggeredExitThisFrameForCollider.Contains(other)) return;
            hasTriggeredExitThisFrameForCollider.Add(other);

            OnTriggerExit(other);
        }

        public void OnAnyTriggerStay(Collider other) {
            if (hasTriggeredStayThisFrameForCollider.Contains(other)) return;
            hasTriggeredStayThisFrameForCollider.Add(other);

            OnTriggerStay(other);
        }

        void LateUpdate() {
            hasTriggeredEnterThisFrameForCollider.Clear();
            hasTriggeredExitThisFrameForCollider.Clear();
            hasTriggeredStayThisFrameForCollider.Clear();
        }
    }
}