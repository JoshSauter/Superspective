using System;
using Sirenix.OdinInspector;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ChangeVisibilityStateAction : TriggerAction {
        [NonSerialized, ShowInInspector] // Fixes serialization in inspector issue
        public DimensionObject[] dimensionObjects;
        public VisibilityState visibilityState;
        
        public override void Execute(MagicTrigger triggerScript) {
            foreach (var dimensionObject in dimensionObjects) {
                dimensionObject.SwitchVisibilityState(visibilityState);
            }
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            foreach (var dimensionObject in dimensionObjects) {
                dimensionObject.SwitchVisibilityState(dimensionObject.startingVisibilityState);
            }
        }
    }
}
