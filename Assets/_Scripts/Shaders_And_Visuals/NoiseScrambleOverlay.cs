using System.Collections.Generic;
using System.Linq;
using Audio;
using NaughtyAttributes;
using Saving;
using SuperspectiveUtils;
using UnityEngine;
using ScramblerReference = SerializableClasses.SerializableReference<NoiseScrambleOverlayObject, NoiseScrambleOverlayObject.NoiseScrambleOverlayObjectSave>;

public class NoiseScrambleOverlay : SaveableObject, CustomAudioJob {
    public static Dictionary<string, ScramblerReference> scramblers = new Dictionary<string, ScramblerReference>();
    [SerializeField]
    Shader noiseShader;

    private Material mat;
    
    public override string ID => "NoiseScrambleOverlay";

    private float maxVolumeDistance = 2f;
    private float zeroVolumeDistance = 15;
    
    [ShowNativeProperty]
    float minDistance {
        get {
            if (GameManager.instance.IsCurrentlyLoading) return float.MaxValue;
            
            if (scramblers.Count > 0) {
                var validScramblersByDistance = scramblers.Values
                    // Only consider scramblers which are loaded and enabled
                    .Where(scramblerRef => scramblerRef.GetOrNull()?.enabled ?? false)
                    .Select(scramblerRef => scramblerRef.GetOrNull())
                    .Where(scrambler => scrambler.scramblerState == NoiseScrambleOverlayObject.ScramblerState.On)
                    .Select(scrambler => RaycastUtils.MinDistanceBetweenPoints(scrambler.transform.position, Player.instance.PlayerCam.transform.position))
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
    private float intensity => Mathf.Pow(1-Mathf.InverseLerp(maxVolumeDistance, zeroVolumeDistance, minDistance), 2);

    protected override void Awake() {
        base.Awake();
        if (noiseShader == null) {
            // Debug.LogWarning($"Noise shader is null on {gameObject.FullPath()}, disabling noise scramble overlay");
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
        job.audio.volume = intensity*intensity;
    }
}