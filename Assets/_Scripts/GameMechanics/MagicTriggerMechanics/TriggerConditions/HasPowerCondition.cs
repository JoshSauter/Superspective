using System;
using PoweredObjects;
using SerializableClasses;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerConditions {
    [Serializable]
    public class HasPowerCondition : TriggerCondition {
        public SuperspectiveReference<PoweredObject, PoweredObject.PoweredObjectSave> powerObjectRef;
        
        protected override float Evaluate(Transform triggerTransform) {
            return powerObjectRef.Reference.Match(
                pwr => pwr.PowerIsOn ? 1 : 0,
                pwrSave => pwrSave.PowerIsOn ? 1 : 0
            );
        }
    }
}
