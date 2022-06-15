using System;
using System.Collections;
using System.Collections.Generic;
using PowerTrailMechanics;
using Saving;
using StateUtils;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
    
    public class RaiseLowerCubeReceptacle : SaveableObject<RaiseLowerCubeReceptacle, RaiseLowerCubeReceptacleSave> {
        public PowerTrail powerTrail;
        public CubeReceptacle cubeReceptacle;
        
        private float raiseLowerSpeed = 1f;
        private float maxHeight = -19.5f;
        private float minHeight = -29f;
        private float height => maxHeight - minHeight;
        private float timeToMove => height / raiseLowerSpeed;
        
        public enum State {
            Lowered,
            Raising,
            Raised,
            Lowering
        }

        public StateMachine<State> state = new StateMachine<State>(State.Lowered);

        // Start is called before the first frame update
        protected override void Start() {
            base.Start();
            state.AddStateTransition(State.Raising, State.Raised, timeToMove);
            state.AddStateTransition(State.Lowering, State.Lowered, timeToMove);
            state.AddTrigger(State.Raised, 0f, () => SetHeight(maxHeight));
            state.AddTrigger(State.Lowered, 0f, () => SetHeight(minHeight));

            powerTrail.OnPowerFinish += PowerOn;
            powerTrail.OnDepowerBegin += PowerOff;
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            powerTrail.OnPowerFinish -= PowerOn;
            powerTrail.OnDepowerBegin -= PowerOff;
        }

        void PowerOn() {
            switch (state.state) {
                case State.Lowered:
                    state.Set(State.Raising);
                    break;
                case State.Raising:
                case State.Raised:
                case State.Lowering:
                    float t = Mathf.InverseLerp(minHeight, maxHeight, transform.localPosition.y);
                    state.Set(State.Raising);
                    state.timeSinceStateChanged = t * timeToMove;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void PowerOff() {
            switch (state.state) {
                case State.Lowering:
                case State.Lowered:
                case State.Raising:
                    float t = 1-Mathf.InverseLerp(minHeight, maxHeight, transform.localPosition.y);
                    state.Set(State.Lowering);
                    state.timeSinceStateChanged = t * timeToMove;
                    break;
                case State.Raised:
                    state.Set(State.Lowering);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Update is called once per frame
        void Update() {
            switch (state.state) {
                case State.Lowered:
                case State.Raised:
                    break;
                case State.Raising: {
                    float t = state.timeSinceStateChanged / timeToMove;
                    float targetHeight = Mathf.Lerp(minHeight, maxHeight, t);
                    float delta = SetHeight(targetHeight);
                    if (cubeReceptacle.isCubeInReceptacle) {
                        cubeReceptacle.cubeInReceptacle.transform.Translate(Vector3.up * delta, Space.World);
                    }
                    break;
                }
                case State.Lowering: {
                    float t = state.timeSinceStateChanged / timeToMove;
                    float targetHeight = Mathf.Lerp(maxHeight, minHeight, t);
                    SetHeight(targetHeight);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Returns delta between old height and new
        float SetHeight(float to) {
            Vector3 transformLocalPosition = transform.localPosition;
            transformLocalPosition.y = to;
            float delta = transformLocalPosition.y - transform.localPosition.y;
            transform.localPosition = transformLocalPosition;
            return delta;
        }
    }
    
    #region Saving

    [Serializable]
    public class RaiseLowerCubeReceptacleSave : SerializableSaveObject<RaiseLowerCubeReceptacle> {
        private StateMachine<RaiseLowerCubeReceptacle.State>.StateMachineSave stateSave;

        public RaiseLowerCubeReceptacleSave(RaiseLowerCubeReceptacle script) : base(script) {
            stateSave = script.state.ToSave();
        }

        public override void LoadSave(RaiseLowerCubeReceptacle script) {
            script.state.FromSave(stateSave);
        }
    }
    #endregion
}
