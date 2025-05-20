using System;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ChangeVisibilityStateAction : TriggerAction {
        public DimensionObject[] dimensionObjects;
        public VisibilityState visibilityState;
        public bool ignoreTransitionRules = true;
        
        public override void Execute(MagicTrigger triggerScript) {
            foreach (var dimensionObject in dimensionObjects) {
                dimensionObject.SwitchVisibilityState(visibilityState, ignoreTransitionRules);
            }
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            foreach (var dimensionObject in dimensionObjects) {
                dimensionObject.SwitchVisibilityState(dimensionObject.startingVisibilityState, ignoreTransitionRules);
            }
        }
    }
}
