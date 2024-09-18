using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Telemetry {
    public interface ITelemetry {
        public void Record();
        public void Play();
        public void Stop();
        public void ClearRecordedData();
        
        public bool HasTelemetryDataRecorded { get; }
    }

    public interface ITelemetryData {
        public float Time { get; }
    }

    public enum TelemetryState {
        Stopped,
        Recording,
        PlayingBack
    }
}
