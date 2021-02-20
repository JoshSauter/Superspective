using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LevelManagement;
using MagicTriggerMechanics;
using PortalMechanics;
using SerializableClasses;
using UnityEngine;

namespace LevelSpecific.BehindForkTransition {
    public class WindowPaintingTrigger : MonoBehaviour {
        public SerializableReference<Portal> serializedPortalReference;
        Portal Portal => serializedPortalReference.Reference;
        public TriggerActionType triggerType = TriggerActionType.ToggleGameObjects;
        
        IEnumerator Start() {
            yield return new WaitWhile(() => LevelManager.instance.IsCurrentlyLoadingScenes);
            yield return null;
            MagicTrigger trigger = GetComponent<MagicTrigger>();
            TriggerAction action = trigger.actionsToTrigger.Find(a => a.action == triggerType);
            if (action.objectsToDisable == null) {
                if (Portal != null) {
                    action.objectsToDisable = new[] { Portal.gameObject };
                }
            }
            else if (!action.objectsToDisable.Contains(Portal.gameObject)) {
                List<GameObject> objects = action.objectsToDisable.ToList();
                objects.Add(Portal.gameObject);
                action.objectsToDisable = objects.ToArray();
            }
        }
    }
}