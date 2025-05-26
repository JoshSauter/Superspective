using System;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ChangeVisibilityStateAction : TriggerAction {
        public DimensionObject[] dimensionObjects;
        public VisibilityState visibilityState;
        public DimensionObject.RefreshMode refreshMode = DimensionObject.RefreshMode.All;
        public bool ignoreTransitionRules = true;
        
        public override void Execute(MagicTrigger triggerScript) {
            foreach (var dimensionObject in dimensionObjects) {
                dimensionObject.SwitchVisibilityState(visibilityState, refreshMode, ignoreTransitionRules);
            }
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            foreach (var dimensionObject in dimensionObjects) {
                dimensionObject.SwitchVisibilityState(dimensionObject.startingVisibilityState, refreshMode, ignoreTransitionRules);
            }
        }
    }
}
