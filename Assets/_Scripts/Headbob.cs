using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Headbob : MonoBehaviour {
	public AnimationCurve viewBobCurve;
	PlayerMovement playerMovement;
	// This value is read from CameraFollow to apply the camera transform offset in one place
	public float curBobAmount = 0f;

	// Time in the animation curve
	public float t = 0f;
	public float curPeriod = 1f;
	const float minPeriod = .24f;
	const float maxPeriod = .87f;
	public float headbobAmount = .5f;
	float curAmplitude = 1f;
	const float minAmplitude = .5f;
	const float maxAmplitude = 1.25f;

    void Start() {
		playerMovement = GetComponent<PlayerMovement>();
    }

    void FixedUpdate() {
		Vector3 playerVelocity = playerMovement.ProjectedHorizontalVelocity();
		float playerSpeed = playerVelocity.magnitude;
		if (playerMovement.grounded && playerSpeed > 0.2f) {
			curPeriod = Mathf.Lerp(maxPeriod, minPeriod, Mathf.InverseLerp(0, 20f, playerSpeed));
			curAmplitude = headbobAmount * Mathf.Lerp(minAmplitude, maxAmplitude, Mathf.InverseLerp(0, 20f, playerSpeed));

			t += Time.fixedDeltaTime / curPeriod;
			t = Mathf.Repeat(t, 1f);

			float thisFrameBobAmount = viewBobCurve.Evaluate(t) * curAmplitude;
			curBobAmount = thisFrameBobAmount;
		}
		else {
			t = 0;
			float nextBobAmount = Mathf.Lerp(curBobAmount, 0f, 4f * Time.fixedDeltaTime);

			curBobAmount = nextBobAmount;
		}
    }

	#region Saving
	// There's only one player so we don't need a UniqueId here
	public string ID => "Headbob";

	[Serializable]
	class HeadbobSave {
		float curBobAmount;

		float t;
		float curPeriod;
		float headbobAmount;
		float curAmplitude;

		public HeadbobSave(Headbob headbob) {
			this.curBobAmount = headbob.curBobAmount;
			this.t = headbob.t;
			this.curPeriod = headbob.curPeriod;
			this.headbobAmount = headbob.headbobAmount;
			this.curAmplitude = headbob.curAmplitude;
		}

		public void LoadSave(Headbob headbob) {
			headbob.curBobAmount = this.curBobAmount;
			headbob.t = this.t;
			headbob.curPeriod = this.curPeriod;
			headbob.headbobAmount = this.headbobAmount;
			headbob.curAmplitude = this.curAmplitude;
		}
	}

	public object GetSaveObject() {
		return new HeadbobSave(this); ;
	}

	public void LoadFromSavedObject(object savedObject) {
		HeadbobSave save = savedObject as HeadbobSave;

		save.LoadSave(this);
	}
	#endregion
}
