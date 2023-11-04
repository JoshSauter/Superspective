using System;
using System.Collections;
using Audio;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

partial class PlayerMovement {
    [Serializable]
    public class JumpMovement : PlayerMovementComponent {
        public JumpMovement(global::PlayerMovement movement) : base(movement) { }
        
        // Jump Settings
        public enum JumpState {
            JumpReady,
            Jumping,
            JumpOnCooldown
        }
        const float JUMP_FORCE = 936;
        const float JUMP_COOLDOWN = 0.2f; // Time after landing before jumping is available again
        const float MIN_JUMP_TIME = 0.5f; // as long as underMinJumpTime
        const float DESIRED_JUMP_HEIGHT = 2.672f;
        
        // Properties
        float JumpForce => CalculatedJumpForce(DESIRED_JUMP_HEIGHT * m.scale, m.thisRigidbody.mass, Physics.gravity.magnitude);
        public bool UnderMinJumpTime => jumpState == JumpState.Jumping && jumpState.timeSinceStateChanged < MIN_JUMP_TIME;
        
        float jumpCooldownRemaining; // Prevents player from jumping again while > 0
        public StateMachine<JumpState> jumpState;
        
        public override void Init() {
            InitStateMachine();
        }

        void InitStateMachine() {
            jumpState = m.StateMachine(JumpState.JumpReady, true);
            
            jumpState.AddStateTransition(JumpState.JumpOnCooldown, JumpState.JumpReady, JUMP_COOLDOWN);
        }

        public void UpdateJumping() {
            switch (jumpState.state) {
                case JumpState.JumpReady:
                    if (m.input.JumpHeld && m.IsGrounded && !m.Grounded.StandingOnHeldObject) {
                        Jump();
                    }
                    return;
                case JumpState.Jumping:
                    if (UnderMinJumpTime) return;
                    
                    if (m.IsGrounded) {
                        m.OnJumpLanding?.Invoke();
                        jumpState.Set(JumpState.JumpOnCooldown);
                    }
                    return;
                case JumpState.JumpOnCooldown:
                    return;
            }
        }

        /// <summary>
        ///     Removes any current y-direction movement on the player, applies a one time impulse force to the player upwards,
        ///     then waits jumpCooldown seconds to be ready again.
        /// </summary>
        void Jump() {
            m.OnJump?.Invoke();
            AudioManager.instance.PlayOnGameObject(AudioName.PlayerJump, m.ID, m);
            m.Grounded.IsGrounded = false;

            Vector3 jumpVector = -Physics.gravity.normalized * JumpForce;
            
            if (m.stairMovement.stepState != StairMovement.StepState.StepReady) {
                m.stairMovement.stepState.Set(StairMovement.StepState.StepReady);
            }
            m.thisRigidbody.isKinematic = false;
            
            m.thisRigidbody.velocity = m.thisRigidbody.velocity.WithY(0);
            m.thisRigidbody.AddForce(jumpVector, ForceMode.Impulse);
            m.StartCoroutine(PrintMaxHeight(transform.position));

            jumpState.Set(JumpState.Jumping);
        }
        
        float CalculatedJumpForce(float wantedHeight, float mass, float g){
            return mass * Mathf.Sqrt( 2 * wantedHeight * g);
        }

        IEnumerator PrintMaxHeight(Vector3 startPosition) {
            float maxHeight = 0;
            float maxAdjustedHeight = 0;
            yield return new WaitForSeconds(MIN_JUMP_TIME / 2f);
            while (!m.IsGrounded) {
                float height = Vector3.Dot(transform.up, transform.position - startPosition);
                if (height > maxHeight) maxHeight = height;
                float adjustedHeight = Vector3.Dot(transform.up, transform.position - startPosition) / Player.instance.scale;
                if (adjustedHeight > maxAdjustedHeight) maxAdjustedHeight = adjustedHeight;
                yield return new WaitForFixedUpdate();
            }

            m.debug.Log($"Highest jump height: {maxHeight}, (adjusted: {maxAdjustedHeight})");
        }
    }
}
