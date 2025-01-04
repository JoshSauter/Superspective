using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using Saving;
using StateUtils;

namespace Telemetry {
    public class TransformTelemetry : Singleton<TransformTelemetry>, ITelemetry {
        public StateMachine<TelemetryState> telemetryState;

        public struct TransformTelemetryData : ITelemetryData {
            public float time;
            public Player.PlayerSave player;
            public CameraFollow.CameraFollowSave playerCam;
            public PlayerLook.PlayerLookSave playerLook;
            public float Time => time;
        }

        public bool HasTelemetryDataRecorded => telemetryData.Count > 0;
        public bool HasTelemetryDataBeforePlayRecorded => telemetryDataBeforePlay.player != null && telemetryDataBeforePlay.playerCam != null && telemetryDataBeforePlay.playerLook != null;
        
        private readonly List<TransformTelemetryData> telemetryData = new List<TransformTelemetryData>();
        private TransformTelemetryData telemetryDataBeforePlay;

        private Player.PlayerSave GetPlayerTelemetryData => Player.instance.CreateSave() as Player.PlayerSave;
        private CameraFollow.CameraFollowSave GetCamTelemetryData => Player.instance.cameraFollow.CreateSave() as CameraFollow.CameraFollowSave;
        private PlayerLook.PlayerLookSave GetPlayerLookTelemetryData => Player.instance.look.CreateSave() as PlayerLook.PlayerLookSave;
        
        protected void Awake() {
            telemetryState = this.StateMachine(TelemetryState.Stopped);
        }

        /// <summary>
        /// Sets the transform telemetry data to the current state of the player and camera.
        /// </summary>
        [Button("Record")]
        public void Record() {
            ClearRecordedData();

            StartCoroutine(RecordCoroutine());
        }

        IEnumerator RecordCoroutine() {
            telemetryState.Set(TelemetryState.Recording);
            while (telemetryState == TelemetryState.Recording) {
                var playerData = GetPlayerTelemetryData;
                var camData = GetCamTelemetryData;
                var lookData = GetPlayerLookTelemetryData;
                
                telemetryData.Add(new TransformTelemetryData() {
                    time = telemetryState.Time,
                    player = playerData,
                    playerCam = camData,
                    playerLook = lookData
                });
                
                yield return null;
            }
        }

        /// <summary>
        /// Remembers where the player was before the playback started, then sets the player's position and rotation to the recorded data.
        /// After this, we rely on simulated input from InputTelemetry to move the player around.
        /// </summary>
        [Button("Play")]
        public void Play() {
            if (!HasTelemetryDataRecorded) {
                Debug.LogError("No telemetry data recorded. Cannot play back TransformTelemetry.");
            }

            StartCoroutine(PlayCoroutine());
        }

        IEnumerator PlayCoroutine() {
            telemetryState.Set(TelemetryState.PlayingBack);

            telemetryDataBeforePlay.player = GetPlayerTelemetryData;
            telemetryDataBeforePlay.playerCam = GetCamTelemetryData;
            telemetryDataBeforePlay.playerLook = GetPlayerLookTelemetryData;
            
            Player.instance.LoadFromSave(telemetryData[0].player);
            Player.instance.cameraFollow.LoadFromSave(telemetryData[0].playerCam);
            Player.instance.look.LoadFromSave(telemetryData[0].playerLook);

            // Updates to next datapoint after each one is played back
            int index = 0;
            TransformTelemetryData dataPoint = telemetryData[index];
            var lateUpdate = new WaitForEndOfFrame();
            while (telemetryState == TelemetryState.PlayingBack) {
                float time = telemetryState.Time;
                
                Player.instance.LoadFromSave(dataPoint.player);
                Player.instance.cameraFollow.LoadFromSave(dataPoint.playerCam);
                Player.instance.look.LoadFromSave(dataPoint.playerLook);
                
                // If we're at the last index, stop playing back
                if (index == telemetryData.Count - 1) {
                    Stop();
                    break;
                }
                
                yield return lateUpdate;
                
                time = telemetryState.Time;
                while (index < telemetryData.Count-1 && telemetryData[index].Time < time) {
                    index++;
                }
                dataPoint = telemetryData[index];
            }
        }

        /// <summary>
        /// If we have telemetry data from before we started playing back, we load it back in here.
        /// </summary>
        [Button("Stop")]
        public void Stop() {
            telemetryState.Set(TelemetryState.Stopped);
        }

        public void ClearRecordedData() {
            telemetryData.Clear();
        }
    }
}