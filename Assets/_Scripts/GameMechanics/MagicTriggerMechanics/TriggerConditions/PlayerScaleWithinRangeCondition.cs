using System;
using NaughtyAttributes;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerConditions {
    [Serializable]
    public class PlayerScaleWithinRangeCondition : TriggerCondition {
        [MinMaxSlider(0.0f, 64f)]
        public Vector2 targetPlayerScaleRange;
        
        protected override float Evaluate(Transform triggerTransform) {
            return Player.instance.Scale >= targetPlayerScaleRange.x && Player.instance.Scale <= targetPlayerScaleRange.y ? 1 : -1;
        }
        
        public override string GetDebugInfo(Transform transform) {
            string debugString = base.GetDebugInfo(transform);
            debugString += "--------\n";
            return debugString;
        }
    }
}
