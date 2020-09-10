using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using NaughtyAttributes;

namespace PictureTeleportMechanics {
    [RequireComponent(typeof(ViewLockObject))]
    public class PictureTeleport : MonoBehaviour {
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
        private float startSsaoIntensity;
        public float ssaoMultiplier = .75f;

        void Start() {
            ssao = EpitaphScreen.instance.playerCamera.GetComponent<ScreenSpaceAmbientOcclusion>();
            startSsaoIntensity = ssao.m_OcclusionIntensity;
            viewLockObject = GetComponent<ViewLockObject>();
            viewLockObject.OnViewLockEnterBegin += () => StartCoroutine(SSAOBlend());
            viewLockObject.OnViewLockEnterFinish += TeleportPlayer;
        }

        IEnumerator SSAOBlend() {
            float timeElapsed = 0f;
            while (timeElapsed < viewLockObject.viewLockTime) {
                timeElapsed += Time.deltaTime;
                float t = timeElapsed / viewLockObject.viewLockTime;

                ssao.m_OcclusionIntensity = Mathf.Lerp(startSsaoIntensity, startSsaoIntensity * ssaoMultiplier, t);

                yield return null;
            }
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
    }
}