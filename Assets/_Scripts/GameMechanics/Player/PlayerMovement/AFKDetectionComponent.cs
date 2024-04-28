using System;
using StateUtils;
using UnityEngine;

partial class PlayerMovement {
    [Serializable]
    public class AFKDetectionComponent : PlayerMovementComponent {
        public AFKDetectionComponent(PlayerMovement movement) : base(movement) { }

        private const float AFK_TIME = 120f;
        private const float AFK_BLACK_OVERLAY_ALPHA = 0.9f;
        
        public enum AFKState {
            Active,
            AFK
        }
        public StateMachine<AFKState> state;
        
        private bool AnyInputThisFrame => PlayerButtonInput.instance.AnyInputHeld;

        public override void Init() {
            state = m.StateMachine(AFKState.Active);
            
            // Transition to AFK state after AFK_TIME seconds of inactivity
            state.AddStateTransition(AFKState.Active, AFKState.AFK, AFK_TIME);
            // Transition back to Active state if any input is detected
            state.AddStateTransition(AFKState.AFK, AFKState.Active, () => AnyInputThisFrame);
            
            // Reset the timer if any input is detected
            state.WithUpdate(AFKState.Active, (_) => {
                if (AnyInputThisFrame) {
                    state.timeSinceStateChanged = 0;
                }
            });
            
            state.OnStateChangeSimple += () => {
                if (state == AFKState.AFK) {
                    MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.On;
                    MainCanvas.instance.BlackOverlayAlpha = AFK_BLACK_OVERLAY_ALPHA;
                }
                else {
                    MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.Off;
                    MainCanvas.instance.BlackOverlayAlpha = 0;
                }
            };
        }
    }
}