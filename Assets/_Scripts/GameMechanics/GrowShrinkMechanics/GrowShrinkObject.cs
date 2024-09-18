using System;
using NaughtyAttributes;
using PortalMechanics;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityStandardAssets.ImageEffects;

namespace GrowShrink {
    [RequireComponent(typeof(UniqueId))]
    public class GrowShrinkObject : SaveableObject<GrowShrinkObject, GrowShrinkObject.GrowShrinkObjectSave> {
        public GravityObject thisGravityObj;
        public Rigidbody thisRigidbody;
        // These values change/get set when this object enters a GrowShrinkHallway
        public float minScale, maxScale;
        public float _currentScale = 1;

        public float startingScale = 1f;
        
        // Change the SSAO to blend the teleport
        ScreenSpaceAmbientOcclusion _ssao;
        // SSAO will automatically clamp the intensity to >= 0.5,
        // so we need to keep track of what the value actually is separately from what it is in the SSAO script
        float ssaoActualIntensity;
        ScreenSpaceAmbientOcclusion SSAO {
            get {
                if (_ssao == null) {
                    _ssao = SuperspectiveScreen.instance?.playerCamera?.GetComponent<ScreenSpaceAmbientOcclusion>();

                    if (_ssao != null) {
                        ssaoActualIntensity = _ssao.m_OcclusionIntensity;
                    }
                }

                return _ssao;
            }
        }

        [ShowNativeProperty]
        public float CurrentScale {
            get => _currentScale;
            private set {
                debug.Log($"Setting currentScale to {value}");
                _currentScale = value;
            }
        }
        // Version of currentScale which never goes above 1 (for tracking effects that should only happen when smaller and not larger)
        public float CurrentScaleClamped => Mathf.Clamp01(CurrentScale);

        public enum State {
            NotInHallway,
            EnteredSmallSide,
            EnteredLargeSide
        }

        public StateMachine<State> state;

        protected override void Awake() {
            base.Awake();

            state = this.StateMachine(State.NotInHallway);

            if (thisRigidbody == null) thisRigidbody = GetComponent<Rigidbody>();
            if (thisGravityObj == null) thisGravityObj = GetComponent<GravityObject>();

            state.OnStateChangeSimple += () => debug.Log($"GrowShrinkObject state changed to {state.State}");
        }

        protected override void Start() {
            base.Start();

            if (Math.Abs(startingScale - CurrentScale) > float.Epsilon) {
                SetScaleDirectly(startingScale);
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            Portal.BeforeAnyPortalTeleport += HandlePortalTeleport;
        }

        private void OnDisable() {
            Portal.BeforeAnyPortalTeleport -= HandlePortalTeleport;
        }

        void HandlePortalTeleport(Portal inPortal, Collider objTeleported) {
            if (!inPortal.changeScale) return;
            
            if (objTeleported == thisRigidbody.GetComponent<Collider>()) {
                SetScaleDirectly(CurrentScale * inPortal.ScaleFactor);
                minScale *= inPortal.ScaleFactor;
                maxScale *= inPortal.ScaleFactor;
            }
        }

        private float MinScale(float scaleFactor) {
            switch (state.State) {
                case State.NotInHallway:
                case State.EnteredSmallSide:
                    return 1f;
                case State.EnteredLargeSide:
                    return 1 / scaleFactor;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float MaxScale(float scaleFactor) {
            switch (state.State) {
                case State.NotInHallway:
                case State.EnteredLargeSide:
                    return 1f;
                case State.EnteredSmallSide:
                    return scaleFactor;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void EnteredHallway(GrowShrinkHallway hallway, bool enteredSmallSide) {
            state.Set(enteredSmallSide ? State.EnteredSmallSide : State.EnteredLargeSide);
            minScale = CurrentScale * MinScale(hallway.scaleFactor);
            debug.Log($"minScale = {CurrentScale} * {MinScale(hallway.scaleFactor)}");
            maxScale = CurrentScale * MaxScale(hallway.scaleFactor);
        }

        public void ExitedHallway(GrowShrinkHallway hallway, bool exitedSmallSide) {
            SetScaleFromHallway(hallway, exitedSmallSide ? 0 : 1);
            state.Set(State.NotInHallway);
        }

        public void SetScaleFromHallway(GrowShrinkHallway hallway, float percentThroughHallway) {
            if (state == State.NotInHallway) return;
            
            float targetScale = Mathf.Lerp(minScale, maxScale, percentThroughHallway);
            SetScaleDirectly(targetScale);
        }

        public void SetScaleDirectly(float targetScale) {
            // Debug.LogWarning($"{ID}: SetScaleDirectly: setting scale to {targetScale} from {currentScale}");
            float targetScaleClamped = Mathf.Clamp01(targetScale);
            Vector3 scaleWithoutMultiplier = transform.localScale / CurrentScale;
            transform.localScale = scaleWithoutMultiplier * targetScale;

            if (thisRigidbody != null) {
                // Technically square-cube law should apply but we're not going to worry about that
                float massWithoutMultiplier = thisRigidbody.mass / CurrentScale;
                thisRigidbody.mass = massWithoutMultiplier * targetScale;

                if (!thisRigidbody.isKinematic) {
                    Vector3 velocityWithoutMultiplier = thisRigidbody.velocity / CurrentScale;
                    thisRigidbody.velocity = velocityWithoutMultiplier * targetScale;
                }
            }

            if (this.TaggedAsPlayer()) {
                Vector3 gravityWithoutMultiplier = Physics.gravity / CurrentScale;
                Physics.gravity = gravityWithoutMultiplier * targetScale;

                Vector3 lastGroundVelocityWithoutMultiplier = PlayerMovement.instance.groundMovement.lastGroundVelocity / CurrentScale;
                PlayerMovement.instance.groundMovement.lastGroundVelocity = lastGroundVelocityWithoutMultiplier * targetScale;
            }
            else if (thisGravityObj != null) {
                float gravityMagnitudeWithoutMultiplier = thisGravityObj.gravityMagnitude / CurrentScale;
                thisGravityObj.gravityMagnitude = gravityMagnitudeWithoutMultiplier * targetScale;
            }

            CurrentScale = targetScale;
        }

#region Saving

        [Serializable]
        public class GrowShrinkObjectSave : SerializableSaveObject<GrowShrinkObject> {
            private float minScale;
            private float maxScale;
            private float currentScale;
            private StateMachine<State>.StateMachineSave stateSave;

            public GrowShrinkObjectSave(GrowShrinkObject script) : base(script) {
                this.minScale = script.minScale;
                this.maxScale = script.maxScale;
                this.currentScale = script.CurrentScale;
                this.stateSave = script.state.ToSave();
            }

            public override void LoadSave(GrowShrinkObject script) {
                script.minScale = this.minScale;
                script.maxScale = this.maxScale;
                script.CurrentScale = this.currentScale;
                script.state.LoadFromSave(this.stateSave);
            }
        }

#endregion
    }
}