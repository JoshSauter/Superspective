using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LevelManagement;
using NaughtyAttributes;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.UI;

// TODO: Make saveable
public class CameraFlythrough : Singleton<CameraFlythrough> {
    public Animator flythroughCameraAnimator;
    public Image topLetterboxBar, bottomLetterboxBar;
    float letterboxHeight = 0.15f; // Fraction of the screen that each bar takes up
    float letterboxAppearTime = 2f;
    public Camera flythroughCamera;
    Dictionary<Levels, CameraFlythroughPath> levelPaths = new Dictionary<Levels, CameraFlythroughPath>();
    TransformInfo initialCameraTransform;

    Levels currentlyPlayingLevel = Levels.ManagerScene;
    public bool isPlayingFlythrough => currentlyPlayingLevel != Levels.ManagerScene;
    float timeSinceStateChange = 0f;
    
    void Start() {
        levelPaths = GetComponentsInChildren<CameraFlythroughPath>().ToDictionary(p => p.level, p => p);
        //flythroughCamera = Player.instance.playerCam; //GetComponentInChildren<Camera>();
    }

    void Update() {
        if (timeSinceStateChange < letterboxAppearTime) {
            timeSinceStateChange += Time.deltaTime;
            
            float t = timeSinceStateChange / letterboxAppearTime;
            t = Easing.EaseInOut(t);
            
            Color targetColor = Color.black;
            targetColor.a = isPlayingFlythrough ? 1 : 0;
            Color startColor = targetColor;
            startColor.a = 1 - startColor.a;

            topLetterboxBar.color = Color.Lerp(startColor, targetColor, t);
            bottomLetterboxBar.color = Color.Lerp(startColor, targetColor, t);

            float botTarget = isPlayingFlythrough ? Mathf.Lerp(0, letterboxHeight, t) : Mathf.Lerp(letterboxHeight, 0, t);
            float topTarget = 1 - botTarget;

            Vector2 topAnchorMin = topLetterboxBar.rectTransform.anchorMin;
            topAnchorMin.y = topTarget;
            topLetterboxBar.rectTransform.anchorMin = topAnchorMin;

            Vector2 botAnchorMax = bottomLetterboxBar.rectTransform.anchorMax;
            botAnchorMax.y = botTarget;
            bottomLetterboxBar.rectTransform.anchorMax = botAnchorMax;
        }
    }

#if UNITY_EDITOR
    [SerializeField]
    Levels playForLevel = Levels.ManagerScene;
    
    [ContextMenu("Play")]
    [Button("Play")]
    void PlayForLevel() {
        PlayForLevel(playForLevel);
    }
#endif
    
    public void PlayForLevel(Levels level) {
        if (levelPaths.ContainsKey(level)) {
            currentlyPlayingLevel = level;
            timeSinceStateChange = 0f;
            initialCameraTransform = new TransformInfo(flythroughCamera.transform);
            levelPaths[level].Play();
            flythroughCameraAnimator.enabled = true;
            flythroughCameraAnimator.SetBool(level.ToName(), true);
        }
        else {
            Debug.LogWarning($"Attempting to play for scene with no CameraFlythrough set: {level}");
        }
    }

    [Button("Stop")]
    public void Stop() {
        if (isPlayingFlythrough) {
            levelPaths[currentlyPlayingLevel].Stop();
            flythroughCameraAnimator.SetBool(currentlyPlayingLevel.ToName(), false);
            flythroughCameraAnimator.enabled = false;
            currentlyPlayingLevel = Levels.ManagerScene;
            timeSinceStateChange = 0f;
            initialCameraTransform.ApplyToTransform(flythroughCamera.transform);
        }
    }
}
