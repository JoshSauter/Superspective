using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using PowerTrailMechanics;
using Saving;
using StateUtils;
using UnityEngine;
using UnityEngine.Serialization;

namespace LevelSpecific.WhiteRoom {
    [RequireComponent(typeof(UniqueId))]
    public class RaiseLowerPlatform : SaveableObject<RaiseLowerPlatform, RaiseLowerPlatformSave>, AudioJobOnGameObject {
        public PowerTrail triggeredByPowerTrail;
        public CubeReceptacle cubeReceptacle;
        
        public float raiseLowerSpeed = 1f;
        public float maxHeight = -19.5f;
        public float minHeight = -29f;
        private float height => Mathf.Abs(maxHeight - minHeight);
        private float timeToMove => height / raiseLowerSpeed;

        const float juiceTime = 1.2f;
        const float juiceFrequency = 8;
        const float juiceAmplitude = 0.0625f;
        
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

            StartCoroutine(StartCo());
        }

        IEnumerator StartCo() {
            yield return new WaitWhile(() => !GameManager.instance.gameHasLoaded);
            if (gameObject == null) yield break;
            
            state.AddStateTransition(State.Raising, State.Raised, timeToMove);
            state.AddStateTransition(State.Lowering, State.Lowered, timeToMove);
            state.AddTrigger(State.Raised, 0f, () => SetHeight(maxHeight));
            state.AddTrigger(State.Lowered, 0f, () => SetHeight(minHeight));
            
            // SFX triggers
            state.AddTrigger((state) => state is State.Raised or State.Lowered,
                () => {
                    AudioManager.instance.PlayOnGameObject(AudioName.MachineClick, ID, this);
                    AudioManager.instance.GetAudioJob(AudioName.MachineOn, ID).Stop();
                });
            state.AddTrigger((state) => state is State.Raised or State.Lowered, 0.5f, () => AudioManager.instance.PlayOnGameObject(AudioName.MachineOff, ID, this));
            state.AddTrigger((state) => state is State.Raising or State.Lowering, () => {
                    AudioManager.instance.PlayOnGameObject(AudioName.MachineClick, ID, this);
                    AudioManager.instance.PlayOnGameObject(AudioName.MachineOn, ID, this);
                }
            );

            if (triggeredByPowerTrail) {
                triggeredByPowerTrail.OnPowerFinish += TriggeredByPowerOn;
                triggeredByPowerTrail.OnDepowerBegin += TriggeredByPowerOff;
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            if (triggeredByPowerTrail) {
                triggeredByPowerTrail.OnPowerFinish -= TriggeredByPowerOn;
                triggeredByPowerTrail.OnDepowerBegin -= TriggeredByPowerOff;
            }
        }

        void TriggeredByPowerOn() {
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

        void TriggeredByPowerOff() {
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
                    if (state.timeSinceStateChanged < juiceTime) {
                        float t = state.timeSinceStateChanged / juiceTime;
                        float target = minHeight - juiceAmplitude * Mathf.Pow((1-t), 2) * Mathf.Sin(juiceFrequency * Mathf.PI * t);
                        SetHeight(target);
                    }
                    else {
                        SetHeight(minHeight);
                    }
                    break;
                case State.Raised:
                    if (state.timeSinceStateChanged < juiceTime) {
                        float t = state.timeSinceStateChanged / juiceTime;
                        float target = maxHeight + juiceAmplitude * Mathf.Pow((1-t), 2) * Mathf.Sin(juiceFrequency * Mathf.PI * t);
                        SetHeight(target);
                    }
                    else {
                        SetHeight(maxHeight);
                    }
                    break;
                case State.Raising: {
                    float t = state.timeSinceStateChanged / timeToMove;
                    float targetHeight = Mathf.Lerp(minHeight, maxHeight, t);
                    float delta = SetHeight(targetHeight);
                    if (cubeReceptacle?.isCubeInReceptacle ?? false) {
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

        public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob audioJob) => transform;
    }
    
    #region Saving

    [Serializable]
    public class RaiseLowerPlatformSave : SerializableSaveObject<RaiseLowerPlatform> {
        private StateMachine<RaiseLowerPlatform.State>.StateMachineSave stateSave;

        public RaiseLowerPlatformSave(RaiseLowerPlatform script) : base(script) {
            stateSave = script.state.ToSave();
        }

        public override void LoadSave(RaiseLowerPlatform script) {
            script.state.LoadFromSave(stateSave);
        }
    }
    #endregion
}
