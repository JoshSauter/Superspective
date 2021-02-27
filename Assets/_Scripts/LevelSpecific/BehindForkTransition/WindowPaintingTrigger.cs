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
        
        IEnumerator Start() {
            yield return new WaitUntil(() => gameObject.IsInActiveScene());
            yield return new WaitWhile(() => LevelManager.instance.IsCurrentlyLoadingScenes);
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
        }
    }
}