using System;
using System.Collections;
using Audio;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

partial class PlayerMovement {
    [Serializable]
    public class JumpMovement : PlayerMovementComponent {
        public JumpMovement(PlayerMovement movement) : base(movement) { }
        
        // Jump Settings
        public enum JumpState : byte {
            JumpReady,
            Jumping,
            JumpOnCooldown
        }
        const float JUMP_COOLDOWN = 0.2f; // Time after landing before jumping is available again
        const float MIN_JUMP_TIME = 0.5f; // as long as underMinJumpTime
        const float DESIRED_JUMP_HEIGHT = 2.672f; // TODO: Slightly lower jump height in GrowShrink2
        
        // Properties
        float JumpForce => CalculatedJumpForce(DESIRED_JUMP_HEIGHT * m.Scale, m.thisRigidbody.mass, Physics.gravity.magnitude);
        public bool UnderMinJumpTime => jumpState == JumpState.Jumping && jumpState.Time < MIN_JUMP_TIME;
        
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
            switch (jumpState.State) {
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
            
            if (m.staircaseMovement.stepState != StaircaseMovement.StepState.Idle) {
                m.staircaseMovement.stepState.Set(StaircaseMovement.StepState.Idle);
            }
            m.thisRigidbody.isKinematic = false;
            
            // Cancel out any existing y-velocity (projected)
            Vector3 gravityDirection = Physics.gravity.normalized;
            Vector3 velocity = m.thisRigidbody.velocity;
            Vector3 velocityParallelToGravity = Vector3.Project(velocity, gravityDirection);
            m.thisRigidbody.velocity = velocity - velocityParallelToGravity;

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
                float adjustedHeight = Vector3.Dot(transform.up, transform.position - startPosition) / Player.instance.Scale;
                if (adjustedHeight > maxAdjustedHeight) maxAdjustedHeight = adjustedHeight;
                yield return new WaitForFixedUpdate();
            }

            m.debug.Log($"Highest jump height: {maxHeight}, (adjusted: {maxAdjustedHeight})");
        }
    }
}
