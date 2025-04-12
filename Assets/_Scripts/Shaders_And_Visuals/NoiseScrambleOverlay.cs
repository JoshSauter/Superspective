using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using NaughtyAttributes;
using Saving;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine;
using ScramblerReference = SerializableClasses.SuperspectiveReference<NoiseScrambleOverlayObject, NoiseScrambleOverlayObject.NoiseScrambleOverlayObjectSave>;

public class NoiseScrambleOverlay : SingletonSuperspectiveObject<NoiseScrambleOverlay, NoiseScrambleOverlay.NoiseScrambleOverlaySave>, CustomAudioJob {
    public static readonly Dictionary<string, ScramblerReference> scramblers = new Dictionary<string, ScramblerReference>();
    [SerializeField]
    Shader noiseShader;

    private Material mat;

    public const float MAX_VOLUME_DISTANCE = 10f;
    public const float ZERO_VOLUME_DISTANCE = 75;

    private const float GLOBAL_VALUE_LIFETIME = 0.15f;

    [Serializable]
    public struct GlobalValueSetter {
        public float time;
        public float value;
    }
    private Dictionary<string, GlobalValueSetter> globalValueSetters = new Dictionary<string, GlobalValueSetter>();
    private float GlobalValue => globalValueSetters.Any() ? globalValueSetters.Max(kv => kv.Value.value) : 0f;
    
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
        globalValueSetters = globalValueSetters
            .Where(kv => Time.time - kv.Value.time <= GLOBAL_VALUE_LIFETIME)
            .ToDictionary();
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

        float intensity = Intensity;
        debug.Log($"Intensity: {intensity}");
        mat.SetFloat("_Intensity", intensity);
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
    public void SetNoiseScrambleOverlayValue(string id, float overrideValue) {
        globalValueSetters[id] = new GlobalValueSetter() {
            time = Time.time,
            value = overrideValue
        };
    }

    public override void LoadSave(NoiseScrambleOverlaySave save) {
        this.globalValueSetters = save.globalValueSetters;
    }

    [Serializable]
    public class NoiseScrambleOverlaySave : SaveObject<NoiseScrambleOverlay> {
        public SerializableDictionary<string, GlobalValueSetter> globalValueSetters;

        public NoiseScrambleOverlaySave(NoiseScrambleOverlay script) : base(script) {
            this.globalValueSetters = script.globalValueSetters;
        }
    }
}
