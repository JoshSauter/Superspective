using System;
using PoweredObjects;
using SerializableClasses;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class PowerOrDepowerAction : TriggerAction {
        public SerializableReference<PoweredObject, PoweredObject.PoweredObjectSave> poweredObject;
        public bool setPowerIsOn = true;
        
        public override void Execute(MagicTrigger triggerScript) {
            poweredObject.Reference.MatchAction(
                pwr => pwr.PowerIsOn = setPowerIsOn,
                pwrSave => pwrSave.PowerIsOn = setPowerIsOn
            );
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            poweredObject.Reference.MatchAction(
                pwr => pwr.PowerIsOn = !setPowerIsOn,
                pwrSave => pwrSave.PowerIsOn = !setPowerIsOn
            );
        }
    }
}
