using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SuperspectiveUtils;
using LevelManagement;
using MagicTriggerMechanics;
using PortalMechanics;
using SerializableClasses;
using UnityEngine;

namespace LevelSpecific.BehindForkTransition {
    public class WindowPaintingTrigger : MonoBehaviour {
        public SerializableReference<Portal, Portal.PortalSave> serializedPortalReference;
        // GetOrNull valid here because we wait until we are in the active scene to reference it
        Portal portal => serializedPortalReference.GetOrNull();
        public TriggerActionType triggerType = TriggerActionType.ToggleGameObjects;
        private bool inAsyncReferenceSetup = false;

        private void Start() {
            StartCoroutine(AsyncReferenceSetup());
        }

        IEnumerator AsyncReferenceSetup() {
            inAsyncReferenceSetup = true;
            yield return new WaitUntil(() => gameObject.IsInActiveScene());
            yield return new WaitWhile(() => LevelManager.instance.IsCurrentlySwitchingScenes);
            yield return new WaitUntil(() => portal != null);
            MagicTrigger trigger = GetComponent<MagicTrigger>();
            TriggerAction action = trigger.actionsToTrigger.Find(a => a.action == triggerType);
            if (action.objectsToDisable == null) {
                if (portal != null) {
                    action.objectsToDisable = new[] { portal.gameObject };
                }
            }
            else if (!action.objectsToDisable.Contains(portal.gameObject)) {
                List<GameObject> objects = action.objectsToDisable.ToList();
                objects.Add(portal.gameObject);
                action.objectsToDisable = objects.ToArray();
            }
            inAsyncReferenceSetup = false;
        }

        private void Update() {
            if (!inAsyncReferenceSetup) {
                MagicTrigger trigger = GetComponent<MagicTrigger>();
                TriggerAction action = trigger.actionsToTrigger.Find(a => a.action == triggerType);

                // Restore references broken by scene loads
                if (action.objectsToDisable == null || action.objectsToDisable.ToList().Exists(o => o == null)) {
                    action.objectsToDisable = null;
                    StartCoroutine(AsyncReferenceSetup());
                }
            }
        }
    }
}