using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SuperspectiveUtils;
using LevelManagement;
using MagicTriggerMechanics;
using MagicTriggerMechanics.TriggerActions;
using PortalMechanics;
using SerializableClasses;
using UnityEngine;

namespace LevelSpecific.BehindForkTransition {
    public class WindowPaintingTrigger : MonoBehaviour {
        public SuperspectiveReference<Portal, Portal.PortalSave> serializedPortalReference;
        // GetOrNull valid here because we wait until we are in the active scene to reference it
        Portal portal => serializedPortalReference.GetOrNull();
        [SerializeReference]
        public TriggerAction trigger;
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
            // TriggerAction actionDeprecated = trigger.actionsToTrigger.Find(a => a.action == triggerType);
            // if (actionDeprecated.objectsToDisable == null) {
            //     if (portal != null) {
            //         actionDeprecated.objectsToDisable = new[] { portal.gameObject };
            //     }
            // }
            // else if (!actionDeprecated.objectsToDisable.Contains(portal.gameObject)) {
            //     List<GameObject> objects = actionDeprecated.objectsToDisable.ToList();
            //     objects.Add(portal.gameObject);
            //     actionDeprecated.objectsToDisable = objects.ToArray();
            // }
            inAsyncReferenceSetup = false;
        }

        private void Update() {
            if (!inAsyncReferenceSetup) {
                MagicTrigger trigger = GetComponent<MagicTrigger>();
                // TriggerAction_Deprecated actionDeprecated = trigger.actionsToTriggerOld.Find(a => a.action == triggerType);
                //
                // // Restore references broken by scene loads
                // if (actionDeprecated.objectsToDisable == null || actionDeprecated.objectsToDisable.ToList().Exists(o => o == null)) {
                //     actionDeprecated.objectsToDisable = null;
                //     StartCoroutine(AsyncReferenceSetup());
                // }
            }
        }
    }
}