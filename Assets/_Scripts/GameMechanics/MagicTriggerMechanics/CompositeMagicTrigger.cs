using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicTriggerMechanics {
    public class CompositeMagicTrigger : MagicTrigger {
        public Collider[] colliders;
        public CompositeMagicTriggerPiece[] triggerColliders;
        public HashSet<Collider> hasTriggeredEnterThisFrameForCollider = new HashSet<Collider>();
        public HashSet<Collider> hasTriggeredExitThisFrameForCollider = new HashSet<Collider>();
        public HashSet<Collider> hasTriggeredStayThisFrameForCollider = new HashSet<Collider>();
        
        protected override void Init() {
            base.Init();
            InitializeColliders();
        }

        public void InitializeColliders() {
            if (colliders == null || colliders.Length == 0) {
                colliders = transform.GetComponentsInChildren<Collider>();
            }

            CompositeMagicTriggerPiece GetOrCreatePiece(Collider c) {
                if (c.gameObject.TryGetComponent(out CompositeMagicTriggerPiece trigger)) {
                    return trigger;
                }
                else {
                    return c.gameObject.AddComponent<CompositeMagicTriggerPiece>();
                }
            }
            triggerColliders = colliders.Select(GetOrCreatePiece).ToArray();
            
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