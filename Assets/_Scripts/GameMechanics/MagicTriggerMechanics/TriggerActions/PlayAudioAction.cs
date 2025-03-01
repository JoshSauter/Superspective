using System;
using Audio;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    // TODO: Add more features as needed
    public class PlayAudioAction : TriggerAction {
        public AudioName audio;
        
        public override void Execute(MagicTrigger triggerScript) {
            AudioManager.instance.Play(audio, triggerScript.ID);
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {}
    }
}
