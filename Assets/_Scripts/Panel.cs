using System;
using Audio;
using Saving;
using SerializableClasses;
using UnityEngine;
using static Audio.AudioManager;

[RequireComponent(typeof(UniqueId))]
public class Panel : SaveableObject<Panel, Panel.PanelSave> {
    public enum State {
        Deactivated,
        Activating,
        Activated,
        Deactivating
    }

    public Color gemColor;
    public Button gemButton;
    public float colorLerpTime = .75f;
    readonly float maxPitch = 1f;
    readonly float maxVolume = 1f;
    readonly float minPitch = 0.5f;
    readonly float minVolume = 0.25f;
    UniqueId _id;
    State _state;

    // Sound settings
    bool soundActivated;

    Color startColor, endColor;
    EpitaphRenderer thisRenderer;
    float timeSinceStateChange;

    UniqueId id {
        get {
            if (_id == null) _id = GetComponent<UniqueId>();
            return _id;
        }
    }

    public State state {
        get => _state;
        set {
            if (_state == value) return;
            timeSinceStateChange = 0f;
            switch (value) {
                case State.Deactivated:
                    OnPanelDeactivateFinish?.Invoke();
                    break;
                case State.Activating:
                    OnPanelActivateBegin?.Invoke();
                    startColor = thisRenderer.GetMainColor();
                    endColor = gemColor;
                    break;
                case State.Activated:
                    OnPanelActivateFinish?.Invoke();
                    break;
                case State.Deactivating:
                    OnPanelDeactivateBegin?.Invoke();
                    startColor = gemColor;
                    endColor = thisRenderer.GetMainColor();
                    break;
            }

            _state = value;
        }
    }

    public bool activated => state == State.Activated || state == State.Activating;

    protected override void Awake() {
        base.Awake();
        // Set up references
        thisRenderer = gameObject.GetComponent<EpitaphRenderer>();
        if (thisRenderer == null) thisRenderer = gameObject.AddComponent<EpitaphRenderer>();

        gemButton = GetComponentInChildren<Button>();
        EpitaphRenderer gemButtonRenderer = gemButton.GetComponent<EpitaphRenderer>();
        if (gemButtonRenderer == null) gemButtonRenderer = gemButton.gameObject.AddComponent<EpitaphRenderer>();
        gemColor = gemButtonRenderer.GetMainColor();
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();
        gemButton.OnButtonPressFinish += ctx => PanelActivate();
        gemButton.OnButtonDepressBegin += ctx => PanelDeactivate();

        gemButton.OnButtonPressBegin += ctx => soundActivated = true;
        gemButton.OnButtonDepressBegin += ctx => soundActivated = false;

        AudioManager.instance.PlayWithUpdate(AudioName.PanelHum, ID, UpdateSound);
    }

    void Update() {
        UpdatePanel();
    }

    void UpdatePanel() {
        timeSinceStateChange += Time.deltaTime;
        float t = timeSinceStateChange / colorLerpTime;
        Color curColor = Color.Lerp(startColor, endColor, t);
        switch (state) {
            case State.Deactivated:
                break;
            case State.Activating:
                if (timeSinceStateChange < colorLerpTime)
                    thisRenderer.SetMainColor(curColor);
                else {
                    thisRenderer.SetMainColor(endColor);
                    state = State.Activated;
                }

                break;
            case State.Activated:
                break;
            case State.Deactivating:
                if (timeSinceStateChange < colorLerpTime)
                    thisRenderer.SetMainColor(curColor);
                else {
                    thisRenderer.SetMainColor(endColor);
                    state = State.Deactivated;
                }

                break;
        }
    }

    void UpdateSound(AudioJob audioJob) {
        if (this == null || gameObject == null) {
            audioJob.removeSound = true;
            return;
        }

        audioJob.audio.transform.position = transform.position;

        if (soundActivated && audioJob.audio.volume < maxVolume) {
            float soundLerpSpeedOn = 1f;
            float newPitch = Mathf.Clamp(audioJob.audio.pitch + Time.deltaTime * soundLerpSpeedOn, minPitch, maxPitch);
            float newVolume = Mathf.Clamp(
                audioJob.audio.volume + Time.deltaTime * soundLerpSpeedOn,
                minVolume,
                maxVolume
            );

            audioJob.audio.pitch = newPitch;
            audioJob.audio.volume = newVolume;
        }

        if (!soundActivated && audioJob.audio.volume > minVolume) {
            float soundLerpSpeedOff = .333f;
            float newPitch = Mathf.Clamp(audioJob.audio.pitch - Time.deltaTime * soundLerpSpeedOff, minPitch, maxPitch);
            float newVolume = Mathf.Clamp(
                audioJob.audio.volume - Time.deltaTime * soundLerpSpeedOff,
                minVolume,
                maxVolume
            );

            audioJob.audio.pitch = newPitch;
            audioJob.audio.volume = newVolume;
        }
    }

    protected virtual void PanelActivate() {
        if (state == State.Deactivated) state = State.Activating;
    }

    protected virtual void PanelDeactivate() {
        if (state == State.Activated) state = State.Deactivating;
    }
    //public SoundEffectOld electricalHumSound;

#region events
    public delegate void PanelAction();

    public event PanelAction OnPanelActivateBegin;
    public event PanelAction OnPanelActivateFinish;
    public event PanelAction OnPanelDeactivateBegin;
    public event PanelAction OnPanelDeactivateFinish;
#endregion

#region Saving
    public override string ID => $"Panel_{id.uniqueId}";

    [Serializable]
    public class PanelSave : SerializableSaveObject<Panel> {
        float colorLerpTime;
        SerializableColor endColor;
        SerializableColor gemColor;
        bool soundActivated;
        SerializableColor startColor;
        State state;
        float timeSinceStateChange;

        public PanelSave(Panel script) {
            state = script.state;
            timeSinceStateChange = script.timeSinceStateChange;
            gemColor = script.gemColor;
            startColor = script.startColor;
            endColor = script.endColor;
            colorLerpTime = script.colorLerpTime;
            soundActivated = script.soundActivated;
        }

        public override void LoadSave(Panel script) {
            script.state = state;
            script.timeSinceStateChange = timeSinceStateChange;
            script.gemColor = gemColor;
            script.startColor = startColor;
            script.endColor = endColor;
            script.colorLerpTime = colorLerpTime;
            script.soundActivated = soundActivated;
        }
    }
#endregion
}