using System;
using NaughtyAttributes;

namespace MagicTriggerMechanics.TriggerActions {
    public interface ITriggerAction {
        public void Execute(MagicTrigger triggerScript);
        public void NegativeExecute(MagicTrigger triggerScript);
    }

    [Serializable]
    public abstract class TriggerAction : ITriggerAction {
        [EnumFlags]
        [ValidateInput(nameof(HasTiming), "Action requires at least one timing set to activate")]
        public ActionTiming actionTiming = ActionTiming.OnceWhileOnStay;

        public abstract void Execute(MagicTrigger triggerScript);
        public virtual void NegativeExecute(MagicTrigger triggerScript) { }

        private bool HasTiming(ActionTiming timing) {
            return (int)timing > 0;
        }

        [Serializable]
        private class SaveData {
            public ActionTiming actionTiming;
        }

        public virtual object GetSaveData(MagicTrigger triggerScript) {
            return new SaveData {
                actionTiming = actionTiming
            };
        }
        
        public virtual void LoadSaveData(object saveData, MagicTrigger triggerScript) {
            SaveData data = (SaveData)saveData;
            actionTiming = data.actionTiming;
        }
    }
}
