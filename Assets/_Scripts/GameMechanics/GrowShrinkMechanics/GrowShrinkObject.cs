using System;
using System.Collections;
using System.Collections.Generic;
using LevelSpecific.Fork;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

namespace GrowShrink {
    [RequireComponent(typeof(UniqueId))]
    public class GrowShrinkObject : SaveableObject<GrowShrinkObject, GrowShrinkObject.GrowShrinkObjectSave> {
        public GravityObject thisGravityObj;
        public Rigidbody thisRigidbody;
        // These values change/get set when this object enters a GrowShrinkHallway
        public float minScale, maxScale;
        public float currentScale = 1;

        public enum State {
            NotInHallway,
            EnteredSmallSide,
            EnteredLargeSide
        }

        public StateMachine<State> state = new StateMachine<State>(State.NotInHallway);

        protected override void Awake() {
            base.Awake();

            if (thisRigidbody == null) thisRigidbody = GetComponent<Rigidbody>();
            if (thisGravityObj == null) thisGravityObj = GetComponent<GravityObject>();
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
            maxScale = currentScale * MaxScale(hallway.scaleFactor);
        }

        public void ExitedHallway(GrowShrinkHallway hallway, bool exitedSmallSide) {
            SetScale(hallway, exitedSmallSide ? 0 : 1);
            state.Set(State.NotInHallway);
        }

        public void SetScale(GrowShrinkHallway hallway, float percentThroughHallway) {
            float targetScale = Mathf.Lerp(minScale, maxScale, percentThroughHallway);
            Vector3 scaleWithoutMultiplier = transform.localScale / currentScale;
            transform.localScale = scaleWithoutMultiplier * targetScale;

            if (thisRigidbody != null) {
                float massWithoutMultiplier = thisRigidbody.mass / currentScale;
                thisRigidbody.mass = massWithoutMultiplier * targetScale;
            }

            if (this.TaggedAsPlayer()) {
                Vector3 gravityWithoutMultiplier = Physics.gravity / currentScale;
                Physics.gravity = gravityWithoutMultiplier * targetScale;

                ColorfulFog fog = Player.instance.playerCam.GetComponent<ColorfulFog>();
                float fogStartWithoutMultiplier = fog.startDistance / currentScale;
                fog.startDistance = fogStartWithoutMultiplier * targetScale;

                // Camera cam = Player.instance.playerCam;
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
                script.state.FromSave(this.stateSave);
            }
        }

#endregion
    }
}