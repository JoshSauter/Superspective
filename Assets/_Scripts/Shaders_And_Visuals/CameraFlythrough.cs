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
    public Camera flythroughCamera;
    Dictionary<Levels, CameraFlythroughPath> levelPaths = new Dictionary<Levels, CameraFlythroughPath>();
    TransformInfo initialCameraTransform;

    Levels currentlyPlayingLevel = Levels.ManagerScene;
    public bool isPlayingFlythrough => currentlyPlayingLevel != Levels.ManagerScene;
    
    void Start() {
        levelPaths = GetComponentsInChildren<CameraFlythroughPath>().ToDictionary(p => p.level, p => p);
        //flythroughCamera = Player.instance.playerCam; //GetComponentInChildren<Camera>();
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
            initialCameraTransform = new TransformInfo(flythroughCamera.transform);
            levelPaths[level].Play();
            flythroughCameraAnimator.enabled = true;
            flythroughCameraAnimator.SetBool(level.ToName(), true);
            
            Letterboxing.instance.state.Set(Letterboxing.State.On);
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
            initialCameraTransform.ApplyToTransform(flythroughCamera.transform);
            
            Letterboxing.instance.state.Set(Letterboxing.State.Off);
        }
    }
}
