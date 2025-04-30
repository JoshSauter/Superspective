using System;
using System.Collections.Generic;
using PortalMechanics;
using UnityEngine;
using Saving;
using SerializableClasses;
using Sirenix.OdinInspector;
using StateUtils;
using SuperspectiveUtils;
using UnityStandardAssets.ImageEffects;

namespace GrowShrink {
    [RequireComponent(typeof(UniqueId))]
    public class GrowShrinkObject : SuperspectiveObject<GrowShrinkObject, GrowShrinkObject.GrowShrinkObjectSave> {
        public static readonly Dictionary<Collider, GrowShrinkObject> collidersAffectedByGrowShrinkObjects = new Dictionary<Collider, GrowShrinkObject>();
        
        public GravityObject thisGravityObj;
        public Rigidbody thisRigidbody;
        public Collider thisCollider;
        
        // These values change/get set when this object enters a GrowShrinkHallway
        public float minScale, maxScale;
        private float _currentScale = 1;

        private bool hasInitializedStartingScale = false;
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

        private BladeEdgeDetection _bladeEdgeDetection;
        private BladeEdgeDetection BladeEdgeDetection {
            get {
                if (_bladeEdgeDetection == null) {
                    _bladeEdgeDetection = MaskBufferRenderTextures.instance.edgeDetection;
                }

                return _bladeEdgeDetection;
            }
        }

        [ShowInInspector]
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
            Growing,
            Shrinking
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

            if (!hasInitializedStartingScale) {
                if (Math.Abs(startingScale - CurrentScale) > float.Epsilon) {
                    SetScaleDirectly(startingScale);
                }

                hasInitializedStartingScale = true;
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            Portal.BeforeAnyPortalTeleport += HandlePortalTeleport;
            
            collidersAffectedByGrowShrinkObjects.Add(thisCollider, this);
        }

        protected override void OnDisable() {
            base.OnDisable();
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
                case State.Growing:
                    return 1f;
                case State.Shrinking:
                    return 1 / scaleFactor;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float MaxScale(float scaleFactor) {
            switch (state.State) {
                case State.NotInHallway:
                case State.Shrinking:
                    return 1f;
                case State.Growing:
                    return scaleFactor;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void EnteredHallway(GrowShrinkHallway hallway, bool enteredSmallSide) {
            if (state != State.NotInHallway) return;
            
            state.Set(enteredSmallSide ? State.Growing : State.Shrinking);
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
            //Debug.LogWarning($"{ID}: SetScaleDirectly: setting scale to {targetScale} from {CurrentScale}");
            float targetScaleClamped = Mathf.Clamp01(targetScale);
            // Calculate scale change ratio
            float scaleChangeRatio = targetScale / CurrentScale;
            transform.localScale *= scaleChangeRatio;

            if (thisRigidbody != null) {
                // Scale mass according to the cube of the scale change (square-cube law)
                thisRigidbody.mass *= Mathf.Pow(scaleChangeRatio, 3);

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
                
                // Change the EdgeDetection sensitivity to show small geometry outlines properly as player shrinks (no change for growing)
                if (targetScale < 1) {
                    BladeEdgeDetection.depthSensitivity = 1 / targetScale;
                }
                else {
                    // TODO: Instead of hardcoded 1, determine the original sensitivity
                    BladeEdgeDetection.depthSensitivity = 1;
                }
            }
            else if (thisGravityObj != null) {
                thisGravityObj.gravityMagnitude *= scaleChangeRatio;
            }

            CurrentScale = targetScale;
        }

#region Saving

        public override void LoadSave(GrowShrinkObjectSave save) {
            transform.localScale = save.transformLocalScale;
            thisRigidbody.mass = save.thisRigidbodyMass;
            if (!thisRigidbody.isKinematic) {
                thisRigidbody.velocity = save.thisRigidbodyVelocity;
            }
        }

        [Serializable]
        public class GrowShrinkObjectSave : SaveObject<GrowShrinkObject> {
            public SerializableVector3 transformLocalScale;
            public float thisRigidbodyMass;
            public SerializableVector3 thisRigidbodyVelocity;

            public GrowShrinkObjectSave(GrowShrinkObject script) : base(script) {
                transformLocalScale = script.transform.localScale;
                thisRigidbodyMass = script.thisRigidbody.mass;
                thisRigidbodyVelocity = script.thisRigidbody.velocity;
            }
        }

#endregion
    }
}