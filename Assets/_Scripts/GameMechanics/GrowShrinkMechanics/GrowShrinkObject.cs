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
        ScreenSpaceAmbientOcclusion ssao {
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
        public float currentScale {
            get => _currentScale;
            set {
                debug.Log($"Setting currentScale to {value}");
                _currentScale = value;
            }
        }
        // Version of currentScale which never goes above 1 (for tracking effects that should only happen when smaller and not larger)
        public float currentScaleClamped => Mathf.Clamp01(currentScale);

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

            state.OnStateChangeSimple += () => debug.Log($"GrowShrinkObject state changed to {state.state}");
        }

        protected override void Start() {
            base.Start();

            if (Math.Abs(startingScale - currentScale) > float.Epsilon) {
                SetScaleDirectly(startingScale);
            }
        }

        private void OnEnable() {
            Portal.BeforeAnyPortalTeleport += HandlePortalTeleport;
        }

        private void OnDisable() {
            Portal.BeforeAnyPortalTeleport -= HandlePortalTeleport;
        }

        void HandlePortalTeleport(Portal inPortal, Collider objTeleported) {
            if (!inPortal.changeScale) return;
            
            if (objTeleported == thisRigidbody.GetComponent<Collider>()) {
                SetScaleDirectly(currentScale * inPortal.scaleFactor);
                minScale *= inPortal.scaleFactor;
                maxScale *= inPortal.scaleFactor;
            }
        }

        private float MinScale(float scaleFactor) {
            switch (state.state) {
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
            switch (state.state) {
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
            minScale = currentScale * MinScale(hallway.scaleFactor);
            debug.Log($"minScale = {currentScale} * {MinScale(hallway.scaleFactor)}");
            maxScale = currentScale * MaxScale(hallway.scaleFactor);
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
            float targetScaleClamped = Mathf.Clamp01(targetScale);
            Vector3 scaleWithoutMultiplier = transform.localScale / currentScale;
            transform.localScale = scaleWithoutMultiplier * targetScale;

            if (thisRigidbody != null) {
                float massWithoutMultiplier = thisRigidbody.mass / currentScale;
                thisRigidbody.mass = massWithoutMultiplier * targetScale;

                if (!thisRigidbody.isKinematic) {
                    Vector3 velocityWithoutMultiplier = thisRigidbody.velocity / currentScale;
                    thisRigidbody.velocity = velocityWithoutMultiplier * targetScale;
                }
            }

            if (this.TaggedAsPlayer()) {
                Vector3 gravityWithoutMultiplier = Physics.gravity / currentScale;
                Physics.gravity = gravityWithoutMultiplier * targetScale;

                Vector3 lastGroundVelocityWithoutMultiplier = PlayerMovement.instance.groundMovement.lastGroundVelocity / currentScale;
                PlayerMovement.instance.groundMovement.lastGroundVelocity = lastGroundVelocityWithoutMultiplier * targetScale;

                if (targetScale < 1f || currentScale < 1f) {
                    ColorfulFog fog = Player.instance.playerCam.GetComponent<ColorfulFog>();
                    float fogStartWithoutMultiplier = fog.startDistance / currentScaleClamped;
                    fog.startDistance = fogStartWithoutMultiplier * targetScaleClamped;

                    // Camera cam = Player.instance.playerCam;
                    BladeEdgeDetection edges = MaskBufferRenderTextures.instance.edgeDetection;
                    float depthSensWithoutMultiplier = edges.depthSensitivity * currentScaleClamped;
                    edges.depthSensitivity = depthSensWithoutMultiplier / targetScaleClamped;

                    float ssaoIntensityWithoutMultiplier = ssaoActualIntensity / currentScaleClamped;
                    ssaoActualIntensity = ssaoIntensityWithoutMultiplier * targetScale;
                    ssao.m_OcclusionIntensity = ssaoActualIntensity;
                }
                // float nearClipPlaneWithoutMultiplier = cam.nearClipPlane / currentScale;
                // float farClipPlaneWithoutMultiplier = cam.farClipPlane / currentScale;
                // cam.nearClipPlane = nearClipPlaneWithoutMultiplier * targetScale;
                // cam.farClipPlane = farClipPlaneWithoutMultiplier * targetScale;
            }
            else if (thisGravityObj != null) {
                float gravityMagnitudeWithoutMultiplier = thisGravityObj.gravityMagnitude / currentScale;
                thisGravityObj.gravityMagnitude = gravityMagnitudeWithoutMultiplier * targetScale;
            }

            currentScale = targetScale;
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
                this.currentScale = script.currentScale;
                this.stateSave = script.state.ToSave();
            }

            public override void LoadSave(GrowShrinkObject script) {
                script.minScale = this.minScale;
                script.maxScale = this.maxScale;
                script.currentScale = this.currentScale;
                script.state.LoadFromSave(this.stateSave);
            }
        }

#endregion
    }
}