using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using NaughtyAttributes;
using Saving;
using System;
using LevelManagement;
using SerializableClasses;
using UnityEngine.Events;

namespace PictureTeleportMechanics {
    [RequireComponent(typeof(UniqueId))]
    [RequireComponent(typeof(ViewLockObject))]
    public class PictureTeleport : SuperspectiveObject<PictureTeleport, PictureTeleport.PictureTeleportSave> {
        public static Dictionary<string, BigFrame> bigFrames = new Dictionary<string, BigFrame>();

        public static string BigFrameKey(string scene, string name) {
            return scene + " " + name;
        }
        
        public UnityEvent onTeleport;

        public bool bigFrameIsInSameScene = true;
        [ShowIf("bigFrameIsInSameScene")]
        public BigFrame bigFrame;
        [HideIf("bigFrameIsInSameScene")]
        public Levels bigFrameLevel;
        [HideIf("bigFrameIsInSameScene")]
        public string bigFrameName;

        ViewLockObject viewLockObject;

        [Header("Target position/rotation is local to the BigFrame destination\n(so that it is still valid if moved around)")]
        [OnValueChanged(nameof(SetDragNDropTransform))]
        public Transform dragNDropCameraTransform; // Drag a desired Transform into this to set the position & rotation from it
        public Vector3 targetPosition;
        public Vector3 targetRotation;
        public Vector3 targetCameraPosition;
        public Vector3 targetCameraRotation;
        public float targetLookY = 90f;
        
        private void SetDragNDropTransform() {
            Transform t = dragNDropCameraTransform;
            if (t == null) return;

            targetCameraPosition = t.localPosition;
            targetCameraRotation = t.localRotation.eulerAngles;

            dragNDropCameraTransform = null;
        }

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
        public bool enabledSsaoBlending = true;
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
            viewLockObject.interactableObject.enabledHelpText = "Take a closer look";
        }

        void Update() {
            if (enabledSsaoBlending && ssaoBlendTimeRemaining > 0f) {
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
                // Rose Room title card is delayed until the player moves from the initial position
                bool playLevelChangeBanner = bigFrameLevel != Levels.ForkWhiteRoom3;
                LevelManager.instance.SwitchActiveScene(bigFrameLevel, playLevelChangeBanner);
            }
            Transform player = Player.instance.transform;
            Transform camContainer = SuperspectiveScreen.instance.playerCamera.transform.parent;

            player.position = bigFrameToTeleportTo.frameRenderer.transform.TransformPoint(targetPosition);
            player.rotation = bigFrameToTeleportTo.frameRenderer.transform.rotation * Quaternion.Euler(targetRotation);
            camContainer.localPosition = targetCameraPosition;
            camContainer.localRotation = Quaternion.Euler(targetCameraRotation);
            if (enabledSsaoBlending) {
                ssao.m_OcclusionIntensity = startSsaoIntensity;
            }
            PlayerLook.instance.RotationY = targetLookY;
            PlayerLook.instance.rotationBeforeViewLock = camContainer.rotation;
            Physics.gravity = Physics.gravity.magnitude * -Player.instance.transform.up;
            
            OnPictureTeleport?.Invoke();
            onTeleport?.Invoke();
        }

        #region Saving

        public override void LoadSave(PictureTeleportSave save) {
            targetPosition = save.targetPosition;
            targetRotation = save.targetRotation;
            targetCameraPosition = save.targetCameraPosition;
            targetCameraRotation = save.targetCameraRotation;
            targetLookY = save.targetLookY;
            startSsaoIntensity = save.startSsaoIntensity;
            ssao.m_OcclusionIntensity = save.curSsaoIntensity;
            ssaoBlendTimeRemaining = save.ssaoBlendTimeRemaining;
        }

        [Serializable]
        public class PictureTeleportSave : SaveObject<PictureTeleport> {
            public SerializableVector3 targetPosition;
            public SerializableVector3 targetRotation;
            public SerializableVector3 targetCameraPosition;
            public SerializableVector3 targetCameraRotation;
            public float targetLookY;
            public float startSsaoIntensity;
            public float curSsaoIntensity;
            public float ssaoBlendTimeRemaining;

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
        }
        #endregion
    }
}