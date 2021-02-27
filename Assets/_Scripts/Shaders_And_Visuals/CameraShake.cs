using Saving;
using SerializableClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : SingletonSaveableObject<CameraShake, CameraShake.CameraShakeSave> {
	bool isUsingCurve;
	bool _isShaking = false;
	bool isShaking {
		get { return _isShaking; }
		set {
			if (value && !_isShaking) {
				timeShaking = 0f;
				appliedOffset = Vector2.zero;
			}
			_isShaking = value;
		}
	}

	Vector2 appliedOffset;
	float duration;
	float startIntensity;
	float endIntensity;
	AnimationCurve curve;
	float intensityMultiplier;

	float intensity;
	float timeShaking = 0f;
	const float returnToCenterLerpSpeed = 2f;
	public Vector2 totalOffsetApplied = Vector2.zero;

	void Update() {
		if (Time.timeScale == 0) return;

#if UNITY_EDITOR
		if (DEBUG && Input.GetKeyDown("c")) {
			if (!isShaking) {
				Shake(2, 1, 0);
			}
			else {
				CancelShake();
			}
		}
#endif

		if (!isShaking) {
			if (totalOffsetApplied.magnitude > 0.001f) {
				Vector2 nextTotalOffsetApplied = Vector2.Lerp(totalOffsetApplied, Vector2.zero, returnToCenterLerpSpeed * Time.deltaTime);
				Vector2 offset = nextTotalOffsetApplied - totalOffsetApplied;
				transform.localPosition += new Vector3(offset.x, offset.y, 0);
				totalOffsetApplied = nextTotalOffsetApplied;
			}
			else if (totalOffsetApplied.magnitude > 0f) {
				transform.localPosition -= new Vector3(totalOffsetApplied.x, totalOffsetApplied.y, 0);
				totalOffsetApplied = Vector2.zero;
			}
		}
		else {
			if (timeShaking < duration) {
				float t = timeShaking / duration;

				intensity = isUsingCurve ? curve.Evaluate(t) * intensityMultiplier : Mathf.Lerp(startIntensity, endIntensity, t);
				Vector2 random = UnityEngine.Random.insideUnitCircle * intensity / 10f;
				Vector2 offset = Vector2.Lerp(Vector2.zero, -appliedOffset, t) + random;

				appliedOffset += offset;
				transform.localPosition += new Vector3(offset.x, offset.y, 0);
				totalOffsetApplied += offset;

				timeShaking += Time.deltaTime;
			}
			else {
				transform.localPosition -= new Vector3(appliedOffset.x, appliedOffset.y, 0);
				totalOffsetApplied -= appliedOffset;

				isShaking = false;
			}
		}
	}

	public void Shake(float duration, float intensityMultiplier, AnimationCurve curve) {
		if (!isShaking) {
			this.duration = duration;
			this.intensityMultiplier = intensityMultiplier;
			this.curve = curve;

			isUsingCurve = true;
			isShaking = true;
		}
	}

	public void Shake(float duration, float startIntensity, float endIntensity) {
		if (!isShaking) {
			this.duration = duration;
			this.startIntensity = startIntensity;
			this.endIntensity = endIntensity;

			isUsingCurve = false;
			isShaking = true;
		}
	}

	public void CancelShake() {
		isUsingCurve = false;
		isShaking = false;
	}

	#region Saving
	// There's only one player so we don't need a UniqueId here
	public override string ID => "CameraShake";

	[Serializable]
	public class CameraShakeSave : SerializableSaveObject<CameraShake> {
		bool isUsingCurve;
		bool isShaking;

		SerializableVector2 appliedOffset;
		float duration;
		float startIntensity;
		float endIntensity;
		SerializableAnimationCurve curve;
		float intensityMultiplier;

		float intensity;
		float timeShaking;
		SerializableVector2 totalOffsetApplied;

		public CameraShakeSave(CameraShake cameraShake) : base(cameraShake) {
			this.isUsingCurve = cameraShake.isUsingCurve;
			this.isShaking = cameraShake.isShaking;
			this.appliedOffset = cameraShake.appliedOffset;
			this.duration = cameraShake.duration;
			this.startIntensity = cameraShake.startIntensity;
			this.endIntensity = cameraShake.endIntensity;
			if (cameraShake.curve != null) {
				this.curve = cameraShake.curve;
			}
			this.intensityMultiplier = cameraShake.intensityMultiplier;
			this.intensity = cameraShake.intensity;
			this.timeShaking = cameraShake.timeShaking;
			this.totalOffsetApplied = cameraShake.totalOffsetApplied;
		}

		public override void LoadSave(CameraShake cameraShake) {
			cameraShake.isUsingCurve = this.isUsingCurve;
			cameraShake._isShaking = this.isShaking;
			cameraShake.appliedOffset = this.appliedOffset;
			cameraShake.duration = this.duration;
			cameraShake.startIntensity = this.startIntensity;
			cameraShake.endIntensity = this.endIntensity;
			cameraShake.curve = this.curve;
			cameraShake.intensityMultiplier = this.intensityMultiplier;
			cameraShake.intensity = this.intensity;
			cameraShake.timeShaking = this.timeShaking;
			cameraShake.totalOffsetApplied = this.totalOffsetApplied;
		}
	}
	#endregion
}
