using System;
using Audio;
using Interactables;
using Saving;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine;
using static Audio.AudioManager;

[RequireComponent(typeof(UniqueId))]
public class Panel : SaveableObject<Panel, Panel.PanelSave>, CustomAudioJob {
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
    State _state;

    // Sound settings
    bool soundActivated;

    Color startColor, endColor;
    SuperspectiveRenderer thisRenderer;
    float timeSinceStateChange;

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
        thisRenderer = gameObject.GetComponent<SuperspectiveRenderer>();
        if (thisRenderer == null) thisRenderer = gameObject.AddComponent<SuperspectiveRenderer>();

        if (gemButton == null) {
            gemButton = GetComponentInChildren<Button>();
        }
        SuperspectiveRenderer gemButtonRenderer = gemButton.GetOrAddComponent<SuperspectiveRenderer>();
        gemColor = gemButtonRenderer.GetMainColor();
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();
        gemButton.OnButtonPressFinish += ctx => PanelActivate();
        gemButton.OnButtonUnpressBegin += ctx => PanelDeactivate();

        gemButton.OnButtonPressBegin += ctx => soundActivated = true;
        gemButton.OnButtonUnpressBegin += ctx => soundActivated = false;
    }

    protected override void Init() {
        AudioManager.instance.PlayWithUpdate(AudioName.ElectricalHum, ID, this);
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

    public void UpdateAudioJob(AudioJob audioJob) {
        if (this == null || gameObject == null) {
            audioJob.Stop();
            return;
        }

        audioJob.audio.transform.position = transform.position;

        if (soundActivated && audioJob.audio.volume < maxVolume) {
            float soundLerpSpeedOn = 1f;
            float newPitch = Mathf.Clamp(audioJob.basePitch + Time.deltaTime * soundLerpSpeedOn, minPitch, maxPitch);
            float newVolume = Mathf.Clamp(
                audioJob.audio.volume + Time.deltaTime * soundLerpSpeedOn,
                minVolume,
                maxVolume
            );

            audioJob.basePitch = newPitch;
            audioJob.audio.volume = newVolume;
        }

        if (!soundActivated && audioJob.audio.volume > minVolume) {
            float soundLerpSpeedOff = .333f;
            float newPitch = Mathf.Clamp(audioJob.basePitch - Time.deltaTime * soundLerpSpeedOff, minPitch, maxPitch);
            float newVolume = Mathf.Clamp(
                audioJob.audio.volume - Time.deltaTime * soundLerpSpeedOff,
                minVolume,
                maxVolume
            );

            audioJob.basePitch = newPitch;
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

    [Serializable]
    public class PanelSave : SerializableSaveObject<Panel> {
        float colorLerpTime;
        SerializableColor endColor;
        SerializableColor gemColor;
        bool soundActivated;
        SerializableColor startColor;
        State state;
        float timeSinceStateChange;

        public PanelSave(Panel script) : base(script) {
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