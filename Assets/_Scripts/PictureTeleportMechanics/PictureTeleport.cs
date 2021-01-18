using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using NaughtyAttributes;
using Saving;
using System;
using SerializableClasses;

namespace PictureTeleportMechanics {
    [RequireComponent(typeof(UniqueId))]
    [RequireComponent(typeof(ViewLockObject))]
    public class PictureTeleport : MonoBehaviour, SaveableObject {
        UniqueId _id;
        UniqueId id {
            get {
                if (_id == null) {
                    _id = GetComponent<UniqueId>();
                }
                return _id;
            }
        }

        public static Dictionary<string, BigFrame> bigFrames = new Dictionary<string, BigFrame>();

        public static string BigFrameKey(string scene, string name) {
            return scene + " " + name;
        }

        public bool bigFrameIsInSameScene = true;
        [ShowIf("bigFrameIsInSameScene")]
        public BigFrame bigFrame;
        [HideIf("bigFrameIsInSameScene")]
        public Level bigFrameLevel;
        [HideIf("bigFrameIsInSameScene")]
        public string bigFrameName;

        ViewLockObject viewLockObject;
        public Vector3 targetPosition;
        public Vector3 targetRotation;
        public Vector3 targetCameraPosition;
        public Vector3 targetCameraRotation;
        public float targetLookY = 90f;

        // Change the SSAO to blend the teleport
        ScreenSpaceAmbientOcclusion ssao;
        float startSsaoIntensity;
        const float ssaoMultiplier = .75f;
        public float ssaoBlendTimeRemaining = 0f;

        void Awake() {
            viewLockObject = GetComponent<ViewLockObject>();
        }

        void Start() {
            ssao = EpitaphScreen.instance.playerCamera.GetComponent<ScreenSpaceAmbientOcclusion>();
            startSsaoIntensity = ssao.m_OcclusionIntensity;
            viewLockObject.OnViewLockEnterBegin += () => ssaoBlendTimeRemaining = viewLockObject.viewLockTime;
            viewLockObject.OnViewLockEnterFinish += TeleportPlayer;
        }

        void Update() {
            if (ssaoBlendTimeRemaining > 0f) {
                UpdateSSAOBlend();
            }
		}

        void UpdateSSAOBlend() {
            ssaoBlendTimeRemaining -= Time.deltaTime;
            float t = (viewLockObject.viewLockTime - ssaoBlendTimeRemaining) / viewLockObject.viewLockTime;

            ssao.m_OcclusionIntensity = Mathf.Lerp(startSsaoIntensity, startSsaoIntensity * ssaoMultiplier, Mathf.Clamp01(t));
        }

        void TeleportPlayer() {
            if (bigFrameIsInSameScene) {
                bigFrame.TurnOnFrame();
            }
            else {
                bigFrames[BigFrameKey(LevelManager.instance.GetSceneName(bigFrameLevel), bigFrameName)].TurnOnFrame();
                LevelManager.instance.SwitchActiveScene(bigFrameLevel);
            }
            Transform player = Player.instance.transform;
            Transform camContainer = EpitaphScreen.instance.playerCamera.transform.parent;

            player.position = targetPosition;
            player.rotation = Quaternion.Euler(targetRotation);
            camContainer.localPosition = targetCameraPosition;
            camContainer.localRotation = Quaternion.Euler(targetCameraRotation);
            ssao.m_OcclusionIntensity = startSsaoIntensity;
            PlayerLook.instance.rotationY = targetLookY;
            PlayerLook.instance.rotationBeforeViewLock = camContainer.rotation;
            Physics.gravity = Physics.gravity.magnitude * -Player.instance.transform.up;
        }

        #region Saving
        public bool SkipSave { get; set; }

        public string ID => $"PictureTeleport_{id.uniqueId}";

        [Serializable]
        class PictureTeleportSave {
            SerializableVector3 targetPosition;
            SerializableVector3 targetRotation;
            SerializableVector3 targetCameraPosition;
            SerializableVector3 targetCameraRotation;
            float targetLookY;
            float startSsaoIntensity;
            float curSsaoIntensity;
            float ssaoBlendTimeRemaining;

            public PictureTeleportSave(PictureTeleport script) {
                this.targetPosition = script.targetPosition;
                this.targetRotation = script.targetRotation;
                this.targetCameraPosition = script.targetCameraPosition;
                this.targetCameraRotation = script.targetCameraRotation;
                this.targetLookY = script.targetLookY;
                this.startSsaoIntensity = script.startSsaoIntensity;
                this.curSsaoIntensity = script.ssao.m_OcclusionIntensity;
                this.ssaoBlendTimeRemaining = script.ssaoBlendTimeRemaining;
            }

            public void LoadSave(PictureTeleport script) {
                script.targetPosition = this.targetPosition;
                script.targetRotation = this.targetRotation;
                script.targetCameraPosition = this.targetCameraPosition;
                script.targetCameraRotation = this.targetCameraRotation;
                script.targetLookY = this.targetLookY;
                script.startSsaoIntensity = this.startSsaoIntensity;
                script.ssao.m_OcclusionIntensity = this.curSsaoIntensity;
                script.ssaoBlendTimeRemaining = this.ssaoBlendTimeRemaining;
            }
        }

        public object GetSaveObject() {
            return new PictureTeleportSave(this);
        }

        public void LoadFromSavedObject(object savedObject) {
            PictureTeleportSave save = savedObject as PictureTeleportSave;

            save.LoadSave(this);
        }
        #endregion
    }
}