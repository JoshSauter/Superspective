using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerConditions {
    public interface ITriggerCondition {
        public bool IsTriggered(Transform triggerTransform);
        public bool IsReverseTriggered(Transform triggerTransform);
    }
    
    [Serializable]
    public abstract class TriggerCondition : ITriggerCondition {
        public bool mustBeTriggeredForPeriodOfTime = false;
        // If mustBeTriggeredForPeriodOfTime is true, then triggerTime is the time that the condition must be true for
        [ShowIf(nameof(mustBeTriggeredForPeriodOfTime))]
        public float triggerTime = 1.0f;
        [ShowIf(nameof(mustBeTriggeredForPeriodOfTime))]
        [ReadOnly]
        public float timeTriggered = 0.0f;
        [ShowIf(nameof(mustBeTriggeredForPeriodOfTime))]
        [ReadOnly]
        public float timeTriggeredReverse = 0.0f;
        
        protected abstract float Evaluate(Transform triggerTransform);

        public virtual string GetDebugInfo(Transform transform) {
            float triggerValue = Evaluate(transform);
            return $"Type: {GetType().Name}\nPass ?: {Evaluate(transform) > 0}\nTriggerValue: {triggerValue}\n";
        }

        protected bool IsTriggeredHelper(Func<bool> condition, ref float time) {
            if (mustBeTriggeredForPeriodOfTime) {
                if (condition.Invoke()) {
                    time += Time.deltaTime;
                    if (time >= triggerTime) {
                        return true;
                    }
                }
                else {
                    time = 0.0f;
                }

                return false;
            }
            
            return condition.Invoke();
        }
        
        public virtual bool IsTriggered(Transform triggerTransform) {
            return IsTriggeredHelper(() => Evaluate(triggerTransform) > 0, ref timeTriggered);
        }

        public virtual bool IsReverseTriggered(Transform triggerTransform) {
            return IsTriggeredHelper(() => Evaluate(triggerTransform) < 0, ref timeTriggeredReverse);
        }
    }

    [Serializable]
    public abstract class ThresholdTriggerCondition : TriggerCondition {
        [Range(-1.0f,1.0f)]
        public float triggerThreshold = 0.01f;
        
        public override bool IsTriggered(Transform triggerTransform) {
            return IsTriggeredHelper(() => Evaluate(triggerTransform) > triggerThreshold, ref timeTriggered);
        }
        
        public override bool IsReverseTriggered(Transform triggerTransform) {
            return IsTriggeredHelper(() => Evaluate(triggerTransform) < triggerThreshold, ref timeTriggeredReverse);
        }

        public override string GetDebugInfo(Transform transform) {
            string debugString = base.GetDebugInfo(transform);
            debugString += $"Threshold: {triggerThreshold}\n";
            return debugString;
        }
    }
}
