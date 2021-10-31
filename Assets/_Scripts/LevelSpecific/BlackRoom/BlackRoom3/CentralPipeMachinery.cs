using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Saving;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.BlackRoom.BlackRoom3 {
    public class CentralPipeMachinery : SaveableObject, CustomAudioJob, AudioJobOnGameObject {
        float maxShakeDistance = 32f;
        float minShakeDistance = 8f;
        float shakeIntensity = 0.4f;
        float shakeDuration = 0.75f;
        float shakePeriod = 4f;

#region Shutters
        const int NUM_SHUTTERS = 32;
        public Transform shutter;
        // Tuple to pair inner and outer shutters to move together
        List<Tuple<Transform, Transform>> shutters = new List<Tuple<Transform, Transform>>();
        float[] shutterGPUBuffer = new float[NUM_SHUTTERS];
        public enum ShutterState {
            Shuttered,
            Opening,
            Open
        }

        ShutterState _shutterState = ShutterState.Shuttered;
        public ShutterState shutterState {
            get => _shutterState;
            set {
                if (_shutterState != value) {
                    _shutterState = value;
                    timeSinceShutterStateChanged = 0f;
                }
            }
        }
        float timeSinceShutterStateChanged = 0f;
        float timeToOpenEachShutter = 2.4f;
        float timeBetweenEachShutter = 0.15f;
        float totalShutterOpenTime => NUM_SHUTTERS * timeBetweenEachShutter + timeToOpenEachShutter;
#endregion

        public Button powerButton;
        const float timeToTurnOffEntranceCollidersAt = 0.25f;
        public Collider entrancePlatformCollider;
        public Collider entrancePlatformWallCollider;

        float loopingMachinerySoundMaxVolume = .65f;
        public Transform rainstickAudioLeft, rainstickAudioRight;

        public override string ID => "BlackRoom3Machinery";

        protected override void Awake() {
            base.Awake();
            Transform outerShutter = shutter.GetChild(0);
            Transform innerShutter = shutter.GetChild(1);
            for (int i = 0; i < outerShutter.childCount; i++) {
                shutters.Add(new Tuple<Transform, Transform>(outerShutter.GetChild(i), innerShutter.GetChild(i)));
            }
        }

        protected override void Start() {
            base.Start();
            Shader.SetGlobalVector("_ShutterCenter", transform.position);
            Shader.SetGlobalFloat("_ShutterHeight", shutters[0].Item2.GetComponent<MeshRenderer>().bounds.size.y);
            powerButton.OnButtonPressBegin += (ctx) => NextShutterState();
        }

        void NextShutterState() {
            shutterState = (ShutterState)(((int) shutterState + 1) % 3);
            if (shutterState != ShutterState.Open) {
                entrancePlatformCollider.enabled = true;
                entrancePlatformWallCollider.enabled = true;
            }

            if (shutterState == ShutterState.Opening) {
                Vector3 soundPos = transform.position;
                soundPos.y += 25;
                AudioManager.instance.PlayWithUpdate(AudioName.LoopingMachinery, ID, this, true, UpdateAudioJob);
                AudioManager.instance.PlayAtLocation(AudioName.DrumSingleHitLow, ID, soundPos, true);
                AudioManager.instance.PlayOnGameObject(AudioName.Rainstick, $"{ID}_Left", this, true);
                AudioManager.instance.PlayOnGameObject(AudioName.Rainstick, $"{ID}_Right", this, true);
                AudioManager.instance.PlayOnGameObject(AudioName.LowPulse, $"{ID}_Left", this, true);
                AudioManager.instance.PlayOnGameObject(AudioName.LowPulse, $"{ID}_Right", this, true);
            }
        }

        void Update() {
            if (Input.GetKeyDown("m")) {
                NextShutterState();
            }
            
            switch (shutterState) {
                case ShutterState.Shuttered:
                    if (timeSinceShutterStateChanged == 0) {
                        UpdateShutters(0f);
                    }
                    break;
                case ShutterState.Open:
                    if (timeSinceShutterStateChanged == 0) {
                        UpdateShutters(Mathf.Infinity);
                    }
                    UpdateAudioTransforms(totalShutterOpenTime);
                    break;
                case ShutterState.Opening:
                    UpdateShutters(timeSinceShutterStateChanged);
                    UpdateAudioTransforms(timeSinceShutterStateChanged);
                    break;
            }
            timeSinceShutterStateChanged += Time.deltaTime;
        }

        void UpdateAudioTransforms(float time) {
            Vector3 curPos = rainstickAudioLeft.position;
            curPos.y = Player.instance.transform.position.y;
            rainstickAudioLeft.position = curPos;

            curPos = rainstickAudioRight.position;
            curPos.y = Player.instance.transform.position.y;
            rainstickAudioRight.position = curPos;

            float extraTime = 1.5f;
            rainstickAudioLeft.parent.rotation = Quaternion.Slerp(
                Quaternion.identity,
                Quaternion.Euler(0, 180, 0),
                time / (totalShutterOpenTime + extraTime)
            );
            rainstickAudioRight.parent.rotation = Quaternion.Slerp(
                Quaternion.identity,
                Quaternion.Euler(0, -180, 0),
                time / (totalShutterOpenTime + extraTime)
            );
        }

        void UpdateShutters(float timeIntoAnimation) {
            for (int i = 0; i < shutters.Count; i++) {
                var tuple = shutters[i];
                float offsetTimeElapsed = timeIntoAnimation - i * timeBetweenEachShutter;
                float t = offsetTimeElapsed / timeToOpenEachShutter;

                t = Mathf.Clamp01(t);

                t = Easing.EaseInOut(t);
                        
                Transform outer = tuple.Item1;
                Transform inner = tuple.Item2;

                Vector3 outerScale = outer.localScale;
                Vector3 innerScale = inner.localScale;
                        
                float height = 1 - t;
                outerScale.y = height;
                innerScale.y = height;

                shutterGPUBuffer[i] = height;

                outer.localScale = outerScale;
                inner.localScale = innerScale;

                // When the last shutter is opening, disable the entrance platform collider
                if (i == shutters.Count - 1 && t > timeToTurnOffEntranceCollidersAt) {
                    entrancePlatformCollider.enabled = false;
                    entrancePlatformWallCollider.enabled = false;
                }

                // When the last shutter is all the way open, update the shutter state
                if (i == shutters.Count - 1 && t == 1) {
                    shutterState = ShutterState.Open;
                }
            }
            
            Shader.SetGlobalFloatArray("_Shutters", shutterGPUBuffer);
        }

        public void UpdateAudioJob(AudioManager.AudioJob job) {
            switch (shutterState) {
                case ShutterState.Shuttered:
                    job.Stop();
                    break;
                case ShutterState.Opening:
                    job.audio.volume = Mathf.Lerp(0f, loopingMachinerySoundMaxVolume, timeSinceShutterStateChanged / totalShutterOpenTime);
                    break;
                case ShutterState.Open:
                    job.audio.volume = loopingMachinerySoundMaxVolume;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            Vector3 curPos = transform.position;
            curPos.y = Player.instance.transform.position.y;
            job.audio.transform.position = curPos;

            float distance = Vector3.Distance(job.audio.transform.position, Player.instance.transform.position);
            if (distance < maxShakeDistance && job.audio.time % shakePeriod < 0.5f) {
                float intensity = shakeIntensity * Mathf.InverseLerp(maxShakeDistance, minShakeDistance, distance);
                CameraShake.instance.Shake(shakeDuration, intensity, 0f);
            }
        }

        public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob audioJob) {
            if (audioJob.id.Contains("Left")) {
                audioJob.audio.panStereo = -1;
                return rainstickAudioLeft;
            }
            else {
                audioJob.audio.panStereo = 1;
                return rainstickAudioRight;
            }
        }
    }
}