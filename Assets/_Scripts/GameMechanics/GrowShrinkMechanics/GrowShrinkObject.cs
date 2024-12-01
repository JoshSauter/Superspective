using System;
using System.Collections.Generic;
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
        public static readonly Dictionary<Collider, GrowShrinkObject> collidersAffectedByGrowShrinkObjects = new Dictionary<Collider, GrowShrinkObject>();
        
        public GravityObject thisGravityObj;
        public Rigidbody thisRigidbody;
        public Collider thisCollider;
        
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
            if (thisCollider == null) thisCollider = thisRigidbody.GetComponent<Collider>();

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
            
            collidersAffectedByGrowShrinkObjects.Add(thisCollider, this);
        }

        private void OnDisable() {
            Portal.BeforeAnyPortalTeleport -= HandlePortalTeleport;
            
            collidersAffectedByGrowShrinkObjects.Remove(thisCollider);
        }

        void HandlePortalTeleport(Portal inPortal, Collider objTeleported) {
            if (!inPortal.changeScale) return;
            
            if (objTeleported == thisRigidbody.GetComponent<Collider>()) {
                SetScaleDirectly(CurrentScale * inPortal.ScaleFactor, false);
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

        public void SetScaleDirectly(float targetScale, bool adjustPlayerPosition = true) {
            // Debug.LogWarning($"{ID}: SetScaleDirectly: setting scale to {targetScale} from {currentScale}");
            float targetScaleClamped = Mathf.Clamp01(targetScale);
            // Calculate scale change ratio
            float scaleChangeRatio = targetScale / CurrentScale;
            transform.localScale *= scaleChangeRatio;

            if (thisRigidbody != null) {
                // Technically square-cube law should apply, but we're not going to worry about that
                thisRigidbody.mass *= scaleChangeRatio;

                if (!thisRigidbody.isKinematic) {
                    thisRigidbody.velocity *= scaleChangeRatio;
                }
            }

            if (this.TaggedAsPlayer()) {
                Physics.gravity *= scaleChangeRatio;

                if (adjustPlayerPosition) {
                    // When the player's scale changes, we need to move the player up or down so that the player's feet stay on the ground
                    Vector3 topOfPlayer = Player.instance.movement.TopOfPlayer;
                    Vector3 bottomOfPlayer = Player.instance.movement.BottomOfPlayer;
                    float offsetMultiplier = Utils.Vector3InverseLerp(bottomOfPlayer, topOfPlayer, Player.instance.transform.position);
                    float playerHeight = (topOfPlayer - bottomOfPlayer).magnitude;
                    float verticalOffset = (scaleChangeRatio - 1) * playerHeight * offsetMultiplier;
                    Player.instance.transform.position += Player.instance.transform.up * verticalOffset;
                }

                PlayerMovement.instance.groundMovement.lastGroundVelocity *= scaleChangeRatio;
            }
            else if (thisGravityObj != null) {
                thisGravityObj.gravityMagnitude *= scaleChangeRatio;
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