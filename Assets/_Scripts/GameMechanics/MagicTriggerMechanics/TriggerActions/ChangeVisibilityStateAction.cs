using System;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ChangeVisibilityStateAction : TriggerAction {
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
