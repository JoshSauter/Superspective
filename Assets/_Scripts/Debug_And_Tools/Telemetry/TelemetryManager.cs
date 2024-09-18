using System;
using System.Collections;
using NaughtyAttributes;
using Saving;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.UI;

namespace Telemetry {
    [RequireComponent(typeof(UniqueId))]
    public class TelemetryManager : Singleton<TelemetryManager>, ITelemetry {
        private const string SAVE_FILE_NAME = "Telemetry Autosave";
        private const float ICON_FLASH_SPEED = 6;
        
        public Image recordIcon;
        public Image playIcon;
        
        private SaveMetadataWithScreenshot telemetrySaveMetadata;
        
        InputTelemetry InputTelemetry => InputTelemetry.instance;
        TransformTelemetry TransformTelemetry => TransformTelemetry.instance;
        
        // Keycodes for Shift + , (comma), Shift + . (period), Shift + / (forward slash)
        private KeyCode recordKey = KeyCode.Comma;
        private KeyCode playKey = KeyCode.Slash;
        private KeyCode stopKey = KeyCode.Period;
        
        private void Update() {
            // Check for key presses in Update
            if (DebugInput.GetKey(KeyCode.LeftShift) || DebugInput.GetKey(KeyCode.RightShift)) {
                if (Input.GetKeyDown(recordKey)) {
                    Record();
                }
                else if (Input.GetKeyDown(playKey)) {
                    Play();
                }
                else if (Input.GetKeyDown(stopKey)) {
                    Stop();
                }
            }
        }
        
        /// <summary>
        /// Records telemetry data for the player, until stopped.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        [Button("Record All")]
        public void Record() {
            StartCoroutine(RecordCoroutine());
        }

        IEnumerator RecordCoroutine() {
            telemetrySaveMetadata = AutosaveManager.instance.DoAutosave(SAVE_FILE_NAME);

            yield return new WaitWhile(() => GameManager.instance.IsCurrentlyLoading);
            
            TransformTelemetry.Record();
            InputTelemetry.Record();
            
            recordIcon.enabled = true;
            while (InputTelemetry.telemetryState == TelemetryState.Recording || TransformTelemetry.telemetryState == TelemetryState.Recording) {
                recordIcon.color = recordIcon.color.WithAlpha(0.5f + 0.5f * Mathf.Cos(ICON_FLASH_SPEED * InputTelemetry.telemetryState.Time));
                
                yield return null;
            }
            recordIcon.enabled = false;
        }

        /// <summary>
        /// Plays back the recorded telemetry data for the player.
        /// </summary>
        [Button("Play All")]
        public void Play() {
            if (!HasTelemetryDataRecorded) {
                bool hasTransformData = TransformTelemetry.HasTelemetryDataRecorded;
                bool hasInputData = InputTelemetry.HasTelemetryDataRecorded;
                Debug.LogError($"Missing telemetry data recorded. Cannot play back telemetry. TransformTelemetry has data: {hasTransformData}, InputTelemetry has data: {hasInputData}");
                return;
            }

            StartCoroutine(PlayCoroutine());
        }

        IEnumerator PlayCoroutine() {
            Stop();
            SaveManager.Load(telemetrySaveMetadata);
            
            yield return new WaitWhile(() => GameManager.instance.IsCurrentlyLoading);
            
            TransformTelemetry.Play();
            InputTelemetry.Play();
            
            playIcon.enabled = true;
            while (InputTelemetry.telemetryState == TelemetryState.PlayingBack || TransformTelemetry.telemetryState == TelemetryState.PlayingBack) {
                yield return null;
            }
            playIcon.enabled = false;
        }

        /// <summary>
        /// Stops the recording or playback of telemetry data.
        /// </summary>
        [Button("Stop All")]
        public void Stop() {
            InputTelemetry.Stop();
            TransformTelemetry.Stop();
        }

        public void ClearRecordedData() {
            TransformTelemetry.ClearRecordedData();
            InputTelemetry.ClearRecordedData();
        }

        public bool HasTelemetryDataRecorded => telemetrySaveMetadata != null && TransformTelemetry.HasTelemetryDataRecorded && InputTelemetry.HasTelemetryDataRecorded;
    }
}
