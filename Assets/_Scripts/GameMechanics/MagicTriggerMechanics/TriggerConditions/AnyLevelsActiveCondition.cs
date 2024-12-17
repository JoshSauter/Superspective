using System;
using System.Linq;
using LevelManagement;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerConditions {
    [Serializable]
    public class AnyLevelsActiveCondition : TriggerCondition {
        public Levels[] targetLevels;
        
        // Will return 1 if any of the target levels are active, -1 otherwise
        protected override float Evaluate(Transform triggerTransform) {
            return targetLevels.Contains(LevelManager.instance.ActiveScene) ? 1 : -1;
        }
        
        public override string GetDebugInfo(Transform transform) {
            string debugString = base.GetDebugInfo(transform);
            debugString += "--------\n";
            return debugString;
        }
    }
}
