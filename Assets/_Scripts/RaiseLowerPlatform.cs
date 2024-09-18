using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using PowerTrailMechanics;
using Saving;
using SerializableClasses;
using StateUtils;
using UnityEngine;
using UnityEngine.Serialization;

namespace LevelSpecific.WhiteRoom {
    [RequireComponent(typeof(UniqueId))]
    public class RaiseLowerPlatform : SaveableObject<RaiseLowerPlatform, RaiseLowerPlatformSave>, AudioJobOnGameObject {
        public SerializableReference<PowerTrail, PowerTrail.PowerTrailSave> triggeredByPowerTrailRef;
        public PowerTrail TriggeredByPowerTrail => triggeredByPowerTrailRef.GetOrNull(); // Assumes the power trail is loaded
        public CubeReceptacle cubeReceptacle;
        
        public float raiseLowerSpeed = 1f;
        public float maxHeight = -19.5f;
        public float minHeight = -29f;
        private float Height => Mathf.Abs(maxHeight - minHeight);
        private float TimeToMove => Height / raiseLowerSpeed;

        const float juiceTime = 1.2f;
        const float juiceFrequency = 8;
        const float juiceAmplitude = 0.0625f;
        
        public enum State {
            Lowered,
            Raising,
            Raised,
            Lowering
        }

        public bool playSfx = true;
        public StateMachine<State> state;

        // Start is called before the first frame update
        protected override void Start() {
            base.Start();

            state = this.StateMachine(State.Lowered);

            StartCoroutine(StartCo());
        }

        protected override void OnValidate() {
            base.OnValidate();
            if (state.State is State.Lowered or State.Raising) {
                SetHeight(minHeight);
            }
            else {
                SetHeight(maxHeight);
            }
        }

        IEnumerator StartCo() {
            yield return new WaitWhile(() => !GameManager.instance.gameHasLoaded);
            if (gameObject == null) yield break;
            
            state.AddStateTransition(State.Raising, State.Raised, TimeToMove);
            state.AddStateTransition(State.Lowering, State.Lowered, TimeToMove);
            state.AddTrigger(State.Raised, 0f, () => SetHeight(maxHeight));
            state.AddTrigger(State.Lowered, 0f, () => SetHeight(minHeight));
            
            // SFX triggers
            state.AddTrigger((state) => state is State.Raised or State.Lowered,
                () => {
                    if (playSfx) {
                        AudioManager.instance.PlayOnGameObject(AudioName.MachineClick, ID, this);
                    }
                    AudioManager.instance.GetAudioJob(AudioName.MachineOn, ID)?.Stop();
                });
            state.AddTrigger((state) => state is State.Raised or State.Lowered, 0.5f, () => {
                    if (playSfx) {
                        AudioManager.instance.PlayOnGameObject(AudioName.MachineOff, ID, this);
                    }
                }
            );
            state.AddTrigger((state) => state is State.Raising or State.Lowering, () => {
                    if (playSfx) {
                        AudioManager.instance.PlayOnGameObject(AudioName.MachineClick, ID, this);
                        AudioManager.instance.PlayOnGameObject(AudioName.MachineOn, ID, this);
                    }
                }
            );
        }

        protected override void OnEnable() {
            base.OnEnable();

            var powerTrail = TriggeredByPowerTrail;
            if (powerTrail && powerTrail.pwr) {
                powerTrail.pwr.OnPowerFinish += Raise;
                powerTrail.pwr.OnDepowerBegin += Lower;
            }
        }

        protected void OnDisable() {
            var powerTrail = TriggeredByPowerTrail;
            if (powerTrail && powerTrail.pwr) {
                powerTrail.pwr.OnPowerFinish -= Raise;
                powerTrail.pwr.OnDepowerBegin -= Lower;
            }
        }

        public void Raise() {
            switch (state.State) {
                case State.Lowered:
                    state.Set(State.Raising);
                    break;
                case State.Raising:
                case State.Raised:
                case State.Lowering:
                    float t = Mathf.InverseLerp(minHeight, maxHeight, transform.localPosition.y);
                    state.Set(State.Raising);
                    state.Time = t * TimeToMove;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Lower() {
            switch (state.State) {
                case State.Lowering:
                case State.Lowered:
                case State.Raising:
                    float t = 1-Mathf.InverseLerp(minHeight, maxHeight, transform.localPosition.y);
                    state.Set(State.Lowering);
                    state.Time = t * TimeToMove;
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
            switch (state.State) {
                case State.Lowered:
                    if (state.Time < juiceTime) {
                        float t = state.Time / juiceTime;
                        float target = minHeight - juiceAmplitude * Mathf.Pow((1-t), 2) * Mathf.Sin(juiceFrequency * Mathf.PI * t);
                        float delta = SetHeight(target);
                        if (cubeReceptacle?.isCubeInReceptacle ?? false) {
                            cubeReceptacle.cubeInReceptacle.transform.Translate(transform.up * delta, Space.World);
                        }
                    }
                    else {
                        SetHeight(minHeight);
                    }
                    break;
                case State.Raised:
                    if (state.Time < juiceTime) {
                        float t = state.Time / juiceTime;
                        float target = maxHeight + juiceAmplitude * Mathf.Pow((1-t), 2) * Mathf.Sin(juiceFrequency * Mathf.PI * t);
                        float delta = SetHeight(target);
                        if (cubeReceptacle?.isCubeInReceptacle ?? false) {
                            cubeReceptacle.cubeInReceptacle.transform.Translate(transform.up * delta, Space.World);
                        }
                    }
                    else {
                        SetHeight(maxHeight);
                    }
                    break;
                case State.Raising: {
                    float t = state.Time / TimeToMove;
                    float targetHeight = Mathf.Lerp(minHeight, maxHeight, t);
                    float delta = SetHeight(targetHeight);
                    if (cubeReceptacle?.isCubeInReceptacle ?? false) {
                        cubeReceptacle.cubeInReceptacle.transform.Translate(transform.up * delta, Space.World);
                    }
                    break;
                }
                case State.Lowering: {
                    float t = state.Time / TimeToMove;
                    float targetHeight = Mathf.Lerp(maxHeight, minHeight, t);
                    float delta = SetHeight(targetHeight);
                    if (cubeReceptacle?.isCubeInReceptacle ?? false) {
                        cubeReceptacle.cubeInReceptacle.transform.Translate(transform.up * delta, Space.World);
                    }
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
