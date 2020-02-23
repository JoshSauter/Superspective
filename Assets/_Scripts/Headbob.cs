using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Headbob : MonoBehaviour {
	public AnimationCurve viewBobCurve;
	PlayerMovement playerMovement;
	CameraFollow cameraFollow;

	Camera playerCam;
	float curBobAmount = 0f;

	// Time in the animation curve
	float t = 0f;
	float curPeriod = 1f;
	float minPeriod = .24f;
	float maxPeriod = .87f;
	public float headbobAmount = .5f;
	float curAmplitude = 1f;
	float minAmplitude = .5f;
	float maxAmplitude = 1.25f;

    void Start() {
		playerMovement = GetComponent<PlayerMovement>();
		playerCam = EpitaphScreen.instance.playerCamera;
		cameraFollow = playerCam.GetComponent<CameraFollow>();
    }

    void Update() {
		Vector2 playerVelocity = playerMovement.HorizontalVelocity();
		float playerSpeed = playerVelocity.magnitude;
		if (playerMovement.grounded && playerSpeed > 0.1f) {
			curPeriod = Mathf.Lerp(maxPeriod, minPeriod, Mathf.InverseLerp(0, 20f, playerSpeed));
			curAmplitude = headbobAmount * Mathf.Lerp(minAmplitude, maxAmplitude, Mathf.InverseLerp(0, 20f, playerSpeed));

			t += Time.deltaTime / curPeriod;
			t = Mathf.Repeat(t, 1f);

			//Vector3 camToFocusedObj = Interact.instance.lastObjectHoveredAnyDistance.transform.position - playerCam.transform.position;

			float thisFrameBobAmount = viewBobCurve.Evaluate(t) * curAmplitude;
			float thisFrameOffset = thisFrameBobAmount - curBobAmount;
			curBobAmount = thisFrameBobAmount;
			playerCam.transform.position += Vector3.up * thisFrameOffset;
			cameraFollow.worldPositionLastFrame += Vector3.up * thisFrameOffset;
			cameraFollow.relativePositionLastFrame += Vector3.up * thisFrameOffset;
		}
		else {
			t = 0;
			float nextBobAmount = Mathf.Lerp(curBobAmount, 0f, 10f * Time.deltaTime);
			float thisFrameOffset = nextBobAmount - curBobAmount;

			playerCam.transform.position += Vector3.up * thisFrameOffset;
			cameraFollow.worldPositionLastFrame += Vector3.up * thisFrameOffset;
			cameraFollow.relativePositionLastFrame += Vector3.up * thisFrameOffset;
			curBobAmount = nextBobAmount;
		}
    }
}
