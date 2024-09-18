using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using StateUtils;
using UnityEngine;

namespace Telemetry {
    public class InputTelemetry : Singleton<InputTelemetry>, ITelemetry {
        public static bool KeyPressSimulated(InputType inputType) {
            if (instance.telemetryState != TelemetryState.PlayingBack) return false;

            return instance.simulatedKeyPressed.ContainsKey(inputType) && instance.simulatedKeyPressed[inputType];
        }

        public static bool KeyReleaseSimulated(InputType inputType) {
            if (instance.telemetryState != TelemetryState.PlayingBack) return false;

            return instance.simulatedKeyReleased.ContainsKey(inputType) && instance.simulatedKeyReleased[inputType];
        }

        public static bool KeyHeldSimulated(InputType inputType) {
            if (instance.telemetryState != TelemetryState.PlayingBack) return false;

            return instance.simulatedKeyHeld.ContainsKey(inputType) && instance.simulatedKeyHeld[inputType];
        }
        
        /// <summary>
        /// Returns the simulated stick value for the given input type, which must be one of LeftStick or RightStick
        /// </summary>
        /// <param name="inputType">Which stick to read the value for. Must be LeftStick or RightStick</param>
        /// <returns>The interpolated stick value at the current time</returns>
        public static Vector2 StickSimulated(InputType inputType) {
            return Vector2.zero;
            if (instance.telemetryState != TelemetryState.PlayingBack) return Vector2.zero;
            if (!KeyHeldSimulated(inputType)) return Vector2.zero;

            switch (inputType) {
                case InputType.Up:
                case InputType.Down:
                case InputType.Right:
                case InputType.Left:
                case InputType.Interact:
                case InputType.Zoom:
                case InputType.AlignObject:
                case InputType.Pause:
                case InputType.Jump:
                case InputType.Sprint:
                    Debug.LogWarning($"Attempting to get stick values for... not a stick? {inputType}");
                    return Vector2.zero;
                case InputType.LeftStick:
                    return instance.GetStickHeldValue(instance.telemetryState.Time, instance.leftStickHeldData);
                case InputType.RightStick:
                    return instance.GetStickHeldValue(instance.telemetryState.Time, instance.rightStickHeldData);
                default:
                    throw new ArgumentOutOfRangeException(nameof(inputType), inputType, null);
            }
        }

        readonly Dictionary<InputType, bool> simulatedKeyPressed = new Dictionary<InputType, bool>();
        readonly Dictionary<InputType, bool> simulatedKeyHeld = new Dictionary<InputType, bool>();
        readonly Dictionary<InputType, bool> simulatedKeyReleased = new Dictionary<InputType, bool>();

        public StateMachine<TelemetryState> telemetryState;

        public enum EventType {
            // ButtonHeld is presumed to be the time after Pressed and before Released
            Pressed,
            Released
        }

        public struct InputTelemetryData : ITelemetryData {
            public float time;
            public InputType inputType;
            public EventType eventType;
            public float Time => time;
        }

        public struct StickHeldTelemetryData : ITelemetryData {
            public float time;
            public Vector2 stickValue;
            public float Time => time;
        }

        readonly List<InputTelemetryData> telemetryData = new List<InputTelemetryData>();
        private List<StickHeldTelemetryData> leftStickHeldData = new List<StickHeldTelemetryData>();
        private List<StickHeldTelemetryData> rightStickHeldData = new List<StickHeldTelemetryData>();
        public bool HasTelemetryDataRecorded => telemetryData.Count > 0 || leftStickHeldData.Count > 0 || rightStickHeldData.Count > 0;

        private PlayerButtonInput input;

        void Awake() {
            telemetryState = this.StateMachine(TelemetryState.Stopped);

            // Want to wipe the simulated button presses when we start or stop playing back (basically any state change)
            telemetryState.OnStateChangeSimple += ClearSimulatedInput;

            input = PlayerButtonInput.instance;
        }

        public void ClearRecordedData() {
            telemetryData.Clear();
            leftStickHeldData.Clear();
            rightStickHeldData.Clear();
        }

        void ClearSimulatedInput() {
            simulatedKeyPressed.Clear();
            simulatedKeyHeld.Clear();
            simulatedKeyReleased.Clear();
        }

        /// <summary>
        /// Stops the recording or playback of telemetry data
        /// </summary>
        [Button("Stop")]
        public void Stop() {
            telemetryState.Set(TelemetryState.Stopped);
        }

        /// <summary>
        /// Starts a coroutine to listen for input and record it to telemetryData
        /// </summary>
        [Button("Record")]
        public void Record() {
            StartCoroutine(RecordCoroutine());
        }

        /// <summary>
        /// Plays back the recorded telemetry data by simulating input presses/holds/releases
        /// </summary>
        [Button("Play")]
        public void Play() {
            if (!HasTelemetryDataRecorded) {
                Debug.LogError("Can't play back input telemetry data, no data recorded.");
                return;
            }

            StartCoroutine(PlayCoroutine());
        }

        IEnumerator RecordCoroutine() {
            void RecordPress(InputType inputType) {
                switch (inputType) {
                    case InputType.Up:
                    case InputType.Down:
                    case InputType.Right:
                    case InputType.Left:
                        // Up/Down/Left/Right are covered already by LeftStick, don't double up on the input
                        break;
                    case InputType.Interact:
                    case InputType.Zoom:
                    case InputType.AlignObject:
                    case InputType.Pause:
                    case InputType.Jump:
                    case InputType.Sprint:
                    case InputType.LeftStick:
                    case InputType.RightStick:
                        telemetryData.Add(new InputTelemetryData() {
                            time = telemetryState.Time,
                            inputType = inputType,
                            eventType = EventType.Pressed
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(inputType), inputType, null);
                }
            }

            void RecordRelease(InputType inputType) {
                telemetryData.Add(new InputTelemetryData() {
                    time = telemetryState.Time,
                    inputType = inputType,
                    eventType = EventType.Released
                });
            }

            ClearRecordedData();

            input.OnAnyPress += RecordPress;
            input.OnAnyRelease += RecordRelease;

            Debug.Log("Recording input telemetry data...");
            telemetryState.Set(TelemetryState.Recording);
            while (telemetryState == TelemetryState.Recording) {
                
                yield return null;
            }

            input.OnAnyPress -= RecordPress;
            input.OnAnyRelease -= RecordRelease;

            Debug.Log("Stopped recording input telemetry data.");
        }

        IEnumerator PlayCoroutine() {
            Debug.Log("Playing back input telemetry data...");
            ClearSimulatedInput();
            telemetryState.Set(TelemetryState.PlayingBack, true);

            // Updates to next datapoint after each one is played back
            int index = 0;
            InputTelemetryData dataPoint = telemetryData[index];
            while (telemetryState == TelemetryState.PlayingBack) {
                // Press and release only happen for one frame (hold is more dynamic)
                simulatedKeyPressed.Clear();
                simulatedKeyReleased.Clear();

                if (telemetryState.Time >= dataPoint.time) {
                    switch (dataPoint.eventType) {
                        case EventType.Pressed:
                            simulatedKeyPressed[dataPoint.inputType] = true;
                            simulatedKeyHeld[dataPoint.inputType] = true;
                            break;
                        case EventType.Released:
                            simulatedKeyReleased[dataPoint.inputType] = true;
                            simulatedKeyHeld[dataPoint.inputType] = false;
                            break;
                    }

                    // If we're at the last index, stop playing back
                    if (index == telemetryData.Count - 1) {
                        Stop();
                        break;
                    }

                    index++;
                    dataPoint = telemetryData[index];
                }

                yield return null;
            }

            Debug.Log("Stopped playing back input telemetry data.");
        }
        
        Vector2 GetStickHeldValue(float time, List<StickHeldTelemetryData> data) {
            int FindNearestIndex(float time) {
                // Binary search to find the index of the nearest data point
                int low = 0;
                int high = data.Count - 1;

                while (low <= high) {
                    int mid = (low + high) / 2;
                    if (Mathf.Approximately(data[mid].time, time)) {
                        return mid;
                    } else if (data[mid].time < time) {
                        low = mid + 1;
                    } else {
                        high = mid - 1;
                    }
                }

                // Return the index of the nearest data point
                return Mathf.Clamp(low, 0, data.Count - 1);
            }
                
            // Binary search to find the two data points surrounding the given time
            int index = FindNearestIndex(time);
        
            if (index != -1) {
                // Interpolate between the two nearest data points
                if (index == 0) {
                    return data[0].stickValue;
                } else if (index == data.Count) {
                    return data[data.Count - 1].stickValue;
                } else {
                    StickHeldTelemetryData prevData = data[index - 1];
                    StickHeldTelemetryData nextData = data[index];
                    float t = (time - prevData.time) / (nextData.time - prevData.time);
                    return Vector2.Lerp(prevData.stickValue, nextData.stickValue, t);
                }
            }

            // If no data found, return default Vector2
            return Vector2.zero;
        }
    }
}