using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Headbob : MonoBehaviour {
	public AnimationCurve viewBobCurve;
	PlayerMovement playerMovement;
	CameraFollow cameraFollow;

	Camera playerCam;
	// This value is read from CameraFollow to apply the camera transform offset in one place
	public float curBobAmount = 0f;

	// Time in the animation curve
	public float t = 0f;
	public float curPeriod = 1f;
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
		Vector3 playerVelocity = playerMovement.ProjectedHorizontalVelocity();
		float playerSpeed = playerVelocity.magnitude;
		if (playerMovement.grounded && playerSpeed > 0.2f) {
			curPeriod = Mathf.Lerp(maxPeriod, minPeriod, Mathf.InverseLerp(0, 20f, playerSpeed));
			curAmplitude = headbobAmount * Mathf.Lerp(minAmplitude, maxAmplitude, Mathf.InverseLerp(0, 20f, playerSpeed));

			t += Time.deltaTime / curPeriod;
			t = Mathf.Repeat(t, 1f);

			float thisFrameBobAmount = viewBobCurve.Evaluate(t) * curAmplitude;
			curBobAmount = thisFrameBobAmount;
		}
		else {
			t = 0;
			float nextBobAmount = Mathf.Lerp(curBobAmount, 0f, 4f * Time.deltaTime);

			curBobAmount = nextBobAmount;
		}
    }
}
