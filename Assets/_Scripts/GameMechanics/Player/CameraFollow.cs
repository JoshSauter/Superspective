﻿using System;
using SuperspectiveUtils;
using NaughtyAttributes;
using PortalMechanics;
using Saving;
using SerializableClasses;
using UnityEngine;

// Player camera is already a child of the player, but we want it to act like it's lerping its position towards the player instead
public class CameraFollow : SaveableObject<CameraFollow, CameraFollow.CameraFollowSave> {
    public delegate void CameraFollowUpdate(Vector3 offset, Vector3 positionDiffFromLastFrame);

    public const float desiredLerpSpeed = 20f; // currentLerpSpeed will approach this value after not being changed for a while

    [SerializeField]
    [ReadOnly]
    float currentLerpSpeed = 15f; // Can be set by external scripts to slow the camera's lerp speed for a short time

    public Vector3 relativeStartPosition;
    public Vector3 relativePositionLastFrame; // Used in restoring position of camera after jump-cut movement of player
    public Vector3 worldPositionLastFrame;

    Headbob headbob;

    bool shouldFollow =>
        Player.instance.look.state == PlayerLook.ViewLockState.ViewUnlocked && !CameraFlythrough.instance.isPlayingFlythrough;

    float timeSinceCurrentLerpSpeedWasModified;

    protected override void Awake() {
        base.Awake();
        headbob = Player.instance.GetComponent<Headbob>();
        relativeStartPosition = transform.localPosition;
        worldPositionLastFrame = transform.position;
    }

    protected override void Start() {
        base.Start();
        TeleportEnter.OnAnyTeleportSimple += RecalculateWorldPositionLastFrame;
        Portal.OnAnyPortalTeleportSimple += obj => {
            if (obj.TaggedAsPlayer()) RecalculateWorldPositionLastFrame();
        };
        Player.instance.look.OnViewLockExitFinish += HandleViewUnlockEnd;
    }

    void FixedUpdate() {
        if (!shouldFollow) return;

        Vector3 destination = transform.parent.TransformPoint(relativeStartPosition);

        //float distanceBetweenCamAndPlayerBefore = Vector3.Distance(worldPositionLastFrame, destination);
        Vector3 nextPosition = Vector3.Lerp(
            worldPositionLastFrame,
            destination,
            currentLerpSpeed * Time.fixedDeltaTime
        );
        Vector3 offset = nextPosition - transform.position;
        Vector3 positionDiffFromLastFrame = nextPosition - worldPositionLastFrame;
        OnCameraFollowUpdate?.Invoke(offset, positionDiffFromLastFrame);
        transform.position = nextPosition;
        //float distanceBetweenCamAndPlayer = Vector3.Distance(transform.position, destination);

        //if (distanceBetweenCamAndPlayer > maxFollowDistance) {
        //	maxFollowDistance = distanceBetweenCamAndPlayer;
        //	Debug.LogWarning("New max follow distance: " + maxFollowDistance);
        //}

        Debug.DrawRay(worldPositionLastFrame, transform.position - worldPositionLastFrame, Color.magenta);

        worldPositionLastFrame = transform.position;
        relativePositionLastFrame = transform.localPosition;

        transform.position -= headbob.curBobAmount * -Player.instance.transform.up;

        if (timeSinceCurrentLerpSpeedWasModified > 0.5f)
            currentLerpSpeed = Mathf.Lerp(currentLerpSpeed, desiredLerpSpeed, 0.25f * Time.fixedDeltaTime);

        timeSinceCurrentLerpSpeedWasModified += Time.fixedDeltaTime;
    }

    public event CameraFollowUpdate OnCameraFollowUpdate;

    // DEBUG:
    //float maxFollowDistance = 0f;

    public void SetLerpSpeed(float lerpSpeed) {
        currentLerpSpeed = lerpSpeed;
        timeSinceCurrentLerpSpeedWasModified = 0f;
    }

    // Restore the relative offset of worldPositionLastFrame after a jump-cut movement of the player
    public void RecalculateWorldPositionLastFrame() {
        worldPositionLastFrame = transform.parent.TransformPoint(relativePositionLastFrame);
    }

    void HandleViewUnlockEnd() {
        RecalculateWorldPositionLastFrame();
    }

#region Saving
    // There's only one player so we don't need a UniqueId here
    public override string ID => "CameraFollow";

    [Serializable]
    public class CameraFollowSave : SerializableSaveObject<CameraFollow> {
        float currentLerpSpeed;
        SerializableVector3 relativePositionLastFrame;
        SerializableVector3 relativeStartPosition;

        float timeSinceCurrentLerpSpeedWasModified;
        SerializableVector3 worldPositionLastFrame;

        public CameraFollowSave(CameraFollow cam) : base(cam) {
            currentLerpSpeed = cam.currentLerpSpeed;
            relativeStartPosition = cam.relativeStartPosition;
            relativePositionLastFrame = cam.relativePositionLastFrame;
            worldPositionLastFrame = cam.worldPositionLastFrame;
            timeSinceCurrentLerpSpeedWasModified = cam.timeSinceCurrentLerpSpeedWasModified;
        }

        public override void LoadSave(CameraFollow cam) {
            cam.currentLerpSpeed = currentLerpSpeed;
            cam.relativeStartPosition = relativeStartPosition;
            cam.relativePositionLastFrame = relativePositionLastFrame;
            cam.worldPositionLastFrame = worldPositionLastFrame;
            cam.timeSinceCurrentLerpSpeedWasModified = timeSinceCurrentLerpSpeedWasModified;
        }
    }
#endregion
}