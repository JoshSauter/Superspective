using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerConditions {
    public interface ITriggerCondition {
        public bool IsTriggered(Transform triggerTransform);
        public bool IsReverseTriggered(Transform triggerTransform);
    }
    
    [Serializable]
    public abstract class TriggerCondition : ITriggerCondition {
        protected abstract float Evaluate(Transform triggerTransform);

        public virtual string GetDebugInfo(Transform transform) {
            float triggerValue = Evaluate(transform);
            return $"Type: {GetType().Name}\nPass ?: {IsTriggered(transform)}\nTriggerValue: {triggerValue}\n";
        }
        
        public virtual bool IsTriggered(Transform triggerTransform) {
            return Evaluate(triggerTransform) > 0;
        }

        public virtual bool IsReverseTriggered(Transform triggerTransform) {
            return Evaluate(triggerTransform) < 0;
        }
    }

    [Serializable]
    public abstract class ThresholdTriggerCondition : TriggerCondition {
        [Range(-1.0f,1.0f)]
        public float triggerThreshold = 0.01f;
        
        public override bool IsTriggered(Transform triggerTransform) {
            return Evaluate(triggerTransform) > triggerThreshold;
        }
        
        public override bool IsReverseTriggered(Transform triggerTransform) {
            return Evaluate(triggerTransform) < triggerThreshold;
        }

        public override string GetDebugInfo(Transform transform) {
            string debugString = base.GetDebugInfo(transform);
            debugString += $"Threshold: {triggerThreshold}\n";
            return debugString;
        }
    }
}
