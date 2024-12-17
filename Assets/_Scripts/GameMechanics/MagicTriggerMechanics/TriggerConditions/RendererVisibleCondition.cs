using System;
using SuperspectiveUtils;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerConditions {
    [Serializable]
    public class RendererVisibleCondition : TriggerCondition {
        public Renderer targetRenderer;
        
        protected override float Evaluate(Transform triggerTransform) {
            return targetRenderer.IsVisibleFrom(Player.instance.PlayerCam) ? 1 : -1;
        }
        
        public override string GetDebugInfo(Transform transform) {
            string debugString = base.GetDebugInfo(transform);
            debugString += "--------\n";
            return debugString;
        }
    }
}
