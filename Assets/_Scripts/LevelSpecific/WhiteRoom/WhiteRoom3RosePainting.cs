using MagicTriggerMechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

public class WhiteRoom3RosePainting : MonoBehaviour {
    public GameObject bigFrame;
    GlobalMagicTrigger frameDisableTrigger;
    ViewLockObject viewLockObject;
    public Vector3 targetPosition;
    public Vector3 targetRotation;
    public Vector3 targetCameraPosition;
    public Vector3 targetCameraRotation;
    public float targetLookY = 90f;

    // Change the SSAO to blend the teleport
    ScreenSpaceAmbientOcclusion ssao;
    private float startSsaoIntensity;
    private float ssaoMultiplier = .75f;

    void Start() {
        ssao = EpitaphScreen.instance.playerCamera.GetComponent<ScreenSpaceAmbientOcclusion>();
        startSsaoIntensity = ssao.m_OcclusionIntensity;
        frameDisableTrigger = GetComponent<GlobalMagicTrigger>();
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
        bigFrame.SetActive(true);
        Player.instance.transform.position = targetPosition;
        Player.instance.transform.rotation = Quaternion.Euler(targetRotation);
        EpitaphScreen.instance.playerCamera.transform.parent.localPosition = targetCameraPosition;
        EpitaphScreen.instance.playerCamera.transform.parent.rotation = Quaternion.Euler(targetCameraRotation);
        ssao.m_OcclusionIntensity = startSsaoIntensity;
        PlayerLook.instance.rotationY = targetLookY;
        PlayerLook.instance.rotationBeforeViewLock = Quaternion.Euler(targetCameraRotation);
        Physics.gravity = Physics.gravity.magnitude * -Player.instance.transform.up;
        Invoke(nameof(EnableTrigger), 0.1f);
	}

    void EnableTrigger() {
        frameDisableTrigger.enabled = true;
	}
}
