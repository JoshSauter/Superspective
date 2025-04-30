using System;
using Audio;
using Interactables;
using Saving;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine;
using static Audio.AudioManager;

[RequireComponent(typeof(UniqueId))]
public class Panel : SuperspectiveObject<Panel, Panel.PanelSave>, CustomAudioJob {
    public enum State : byte {
        Deactivated,
        Activating,
        Activated,
        Deactivating
    }

    public Color gemColor;
    public Button gemButton;
    public float colorLerpTime = .75f;
    const float MAX_PITCH = 1f;
    const float MAX_VOLUME = 1f;
    const float MIN_PITCH = 0.5f;
    const float MIN_VOLUME = 0.25f;
    
    State _state;
    float timeSinceStateChange;

    // Sound settings
    bool soundActivated;

    Color startColor, endColor;
    SuperspectiveRenderer thisRenderer;

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

    public bool Activated => state == State.Activated || state == State.Activating;

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
        base.Init();
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

        if (soundActivated && audioJob.audio.volume < MAX_VOLUME) {
            float soundLerpSpeedOn = 1f;
            float newPitch = Mathf.Clamp(audioJob.basePitch + Time.deltaTime * soundLerpSpeedOn, MIN_PITCH, MAX_PITCH);
            float newVolume = Mathf.Clamp(
                audioJob.audio.volume + Time.deltaTime * soundLerpSpeedOn,
                MIN_VOLUME,
                MAX_VOLUME
            );

            audioJob.basePitch = newPitch;
            audioJob.audio.volume = newVolume;
        }

        if (!soundActivated && audioJob.audio.volume > MIN_VOLUME) {
            float soundLerpSpeedOff = .333f;
            float newPitch = Mathf.Clamp(audioJob.basePitch - Time.deltaTime * soundLerpSpeedOff, MIN_PITCH, MAX_PITCH);
            float newVolume = Mathf.Clamp(
                audioJob.audio.volume - Time.deltaTime * soundLerpSpeedOff,
                MIN_VOLUME,
                MAX_VOLUME
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

    public override void LoadSave(PanelSave save) { }

    [Serializable]
    public class PanelSave : SaveObject<Panel> {
        public PanelSave(Panel script) : base(script) { }
    }
#endregion
}