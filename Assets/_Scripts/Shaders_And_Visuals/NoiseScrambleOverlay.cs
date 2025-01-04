using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using NaughtyAttributes;
using Saving;
using SuperspectiveUtils;
using UnityEngine;
using ScramblerReference = SerializableClasses.SuperspectiveReference<NoiseScrambleOverlayObject, NoiseScrambleOverlayObject.NoiseScrambleOverlayObjectSave>;

public class NoiseScrambleOverlay : SingletonSuperspectiveObject<NoiseScrambleOverlay, NoiseScrambleOverlay.NoiseScrambleOverlaySave>, CustomAudioJob {
    public static Dictionary<string, ScramblerReference> scramblers = new Dictionary<string, ScramblerReference>();
    [SerializeField]
    Shader noiseShader;

    private Material mat;
    
    public override string ID => "NoiseScrambleOverlay";

    public const float MAX_VOLUME_DISTANCE = 10f;
    public const float ZERO_VOLUME_DISTANCE = 75;

    private float timeWhenGlobalValueSet = -1;
    private float _globalValue = -1;
    private float GlobalValue {
        get => _globalValue;
        set {
            if (value < 0) {
                timeWhenGlobalValueSet = -1;
            }
            else {
                timeWhenGlobalValueSet = Time.time;
            }
            _globalValue = value;
        }
    }
    
    [ShowNativeProperty]
    float MinDistance {
        get {
            if (GameManager.instance.IsCurrentlyLoading) return float.MaxValue;
            
            if (scramblers.Count > 0) {
                var validScramblersByDistance = scramblers.Values
                    .Select(scramblerRef => scramblerRef.GetOrNull())
                    // Only consider scramblers which are loaded and enabled
                    .Where(scramblerRef => scramblerRef?.enabled ?? false)
                    .Where(scrambler => scrambler.scramblerState == NoiseScrambleOverlayObject.ScramblerState.On)
                    .Select(scrambler => SuperspectivePhysics.ShortestDistance(Player.instance.PlayerCam.transform.position, scrambler.transform.position))
                    // Find the closest scrambler
                    .OrderBy(distance => distance)
                    .ToList();
                if (validScramblersByDistance.Count > 0) {
                    return validScramblersByDistance.First();
                }
                else {
                    return float.MaxValue;
                }
            }
            else {
                return float.MaxValue;
            }
        }
    }

    [ShowNativeProperty]
    private float Intensity => Mathf.Max(GlobalValue, Mathf.Pow(1 - Mathf.InverseLerp(MAX_VOLUME_DISTANCE, ZERO_VOLUME_DISTANCE, MinDistance / Player.instance.Scale), 2));

    protected override void Awake() {
        base.Awake();
        if (noiseShader == null) {
            Debug.LogError($"Noise shader is null on {gameObject.FullPath()}, disabling noise scramble overlay");
            enabled = false;
            return;
        }
        mat = new Material(noiseShader);
    }
    
    // Start is called before the first frame update
    protected override void Init() {
        base.Init();
        AudioManager.instance.PlayWithUpdate(AudioName.WhiteNoiseSpacey, ID, this, settingsOverride: (job) => job.audio.volume = 0);

        SaveManager.BeforeLoad += () => scramblers.Clear();
    }

    private void LateUpdate() {
        if (timeWhenGlobalValueSet > 0 && Time.time - timeWhenGlobalValueSet > 0.15f) {
            GlobalValue = -1;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (GameManager.instance.IsCurrentlyLoading) {
            Graphics.Blit(source, destination);
            return;
        }
        #if UNITY_EDITOR
        if (!Application.isPlaying || mat == null) {
            return;
        }
        #endif
        
        
        debug.Log($"Intensity: {Intensity}");
        mat.SetFloat("_Intensity", Intensity);
        Graphics.Blit(source, destination, mat);
    }

    public void UpdateAudioJob(AudioManager.AudioJob job) {
        if (job.audio == null) {
            job.Stop();
            return;
        }
        
        if (!job.audio.isPlaying) {
            job.audio.volume = 0f;
            job.Play();
        }

        float intensity = Intensity;
        job.audio.volume = intensity*intensity;
    }
    
    // Used to override the noise scramble overlay value with some value. Needs to be called every frame to keep the override
    public void SetNoiseScrambleOverlayValue(float overrideValue) {
        this.GlobalValue = overrideValue;
    }

    public override void LoadSave(NoiseScrambleOverlaySave save) {
        _globalValue = save.globalValue;
        timeWhenGlobalValueSet = save.timeWhenOverrideValueSet;
    }

    [Serializable]
    public class NoiseScrambleOverlaySave : SaveObject<NoiseScrambleOverlay> {
        public float globalValue;
        public float timeWhenOverrideValueSet;

        public NoiseScrambleOverlaySave(NoiseScrambleOverlay script) : base(script) {
            this.globalValue = script.GlobalValue;
            this.timeWhenOverrideValueSet = script.timeWhenGlobalValueSet;
        }
    }
}
