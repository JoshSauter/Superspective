using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using NaughtyAttributes;
using Saving;
using System;
using LevelManagement;
using SerializableClasses;

namespace PictureTeleportMechanics {
    [RequireComponent(typeof(UniqueId))]
    [RequireComponent(typeof(ViewLockObject))]
    public class PictureTeleport : SaveableObject<PictureTeleport, PictureTeleport.PictureTeleportSave> {
        public static Dictionary<string, BigFrame> bigFrames = new Dictionary<string, BigFrame>();

        public static string BigFrameKey(string scene, string name) {
            return scene + " " + name;
        }

        public bool bigFrameIsInSameScene = true;
        [ShowIf("bigFrameIsInSameScene")]
        public BigFrame bigFrame;
        [HideIf("bigFrameIsInSameScene")]
        public Levels bigFrameLevel;
        [HideIf("bigFrameIsInSameScene")]
        public string bigFrameName;

        ViewLockObject viewLockObject;
        [Header("Target position/rotation is local to the BigFrame destination\n(so that it is still valid if moved around)")]
        public Vector3 targetPosition;
        public Vector3 targetRotation;
        public Vector3 targetCameraPosition;
        public Vector3 targetCameraRotation;
        public float targetLookY = 90f;

        // Change the SSAO to blend the teleport
        private ScreenSpaceAmbientOcclusion _ssao;

        ScreenSpaceAmbientOcclusion ssao {
            get {
                if (_ssao == null) {
                    _ssao = SuperspectiveScreen.instance?.playerCamera?.GetComponent<ScreenSpaceAmbientOcclusion>();
                }

                return _ssao;
            }
        }
        
        float startSsaoIntensity;
        const float ssaoMultiplier = .75f;
        public float ssaoBlendTimeRemaining = 0f;

        public delegate void PictureTeleportEvent();

        public PictureTeleportEvent OnPictureTeleport;

        new void Awake() {
            base.Awake();
            viewLockObject = GetComponent<ViewLockObject>();
            if (ssao != null) {
                startSsaoIntensity = ssao.m_OcclusionIntensity;
            }
        }

        protected override void Start() {
            base.Start();
            viewLockObject.cursorIsStationaryOnLock = true;
            viewLockObject.OnViewLockEnterBegin += () => ssaoBlendTimeRemaining = viewLockObject.viewLockTime;
            viewLockObject.OnViewLockEnterFinish += TeleportPlayer;
            viewLockObject.interactableObject.enabledHelpText = "Look closer";
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
            BigFrame bigFrameToTeleportTo = bigFrameIsInSameScene
                ? bigFrame
                : bigFrames[BigFrameKey(bigFrameLevel.ToName(), bigFrameName)];
            if (bigFrameIsInSameScene) {
                bigFrameToTeleportTo.TurnOnFrame();
            }
            else {
                bigFrameToTeleportTo.TurnOnFrame();
                LevelManager.instance.SwitchActiveScene(bigFrameLevel);
            }
            Transform player = Player.instance.transform;
            Transform camContainer = SuperspectiveScreen.instance.playerCamera.transform.parent;

            player.position = bigFrameToTeleportTo.transform.TransformPoint(targetPosition);
            player.rotation = bigFrameToTeleportTo.transform.rotation * Quaternion.Euler(targetRotation);
            camContainer.localPosition = targetCameraPosition;
            camContainer.localRotation = Quaternion.Euler(targetCameraRotation);
            ssao.m_OcclusionIntensity = startSsaoIntensity;
            PlayerLook.instance.rotationY = targetLookY;
            PlayerLook.instance.rotationBeforeViewLock = camContainer.rotation;
            Physics.gravity = Physics.gravity.magnitude * -Player.instance.transform.up;
            
            OnPictureTeleport?.Invoke();
        }

        #region Saving

        [Serializable]
        public class PictureTeleportSave : SerializableSaveObject<PictureTeleport> {
            SerializableVector3 targetPosition;
            SerializableVector3 targetRotation;
            SerializableVector3 targetCameraPosition;
            SerializableVector3 targetCameraRotation;
            float targetLookY;
            float startSsaoIntensity;
            float curSsaoIntensity;
            float ssaoBlendTimeRemaining;

            public PictureTeleportSave(PictureTeleport script) : base(script) {
                this.targetPosition = script.targetPosition;
                this.targetRotation = script.targetRotation;
                this.targetCameraPosition = script.targetCameraPosition;
                this.targetCameraRotation = script.targetCameraRotation;
                this.targetLookY = script.targetLookY;
                this.startSsaoIntensity = script.startSsaoIntensity;
                this.curSsaoIntensity = script.ssao.m_OcclusionIntensity;
                this.ssaoBlendTimeRemaining = script.ssaoBlendTimeRemaining;
            }

            public override void LoadSave(PictureTeleport script) {
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
        #endregion
    }
}