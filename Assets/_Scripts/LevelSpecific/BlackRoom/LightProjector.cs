using UnityEngine;
using Saving;
using System;
using SerializableClasses;

namespace LevelSpecific.BlackRoom {
	public class LightProjector : SaveableObject<LightProjector, LightProjector.LightProjectorSave> {
		const float minSize = .5f;
		const float maxSize = 2.5f;
		float currentSize = 1;
		const float frustumSizeChangeSpeed = 200;

		Animator sideToSideAnim;
		public float curSideToSideAnimTime = 0.5f;
		float desiredSideToSideAnimTime = 0.5f;
		const float sideToSideAnimLerpSpeed = 10f;
		const float rotationSpeed = .4f;

		Quaternion desiredCircumferenceRotation;
		Quaternion curCircumferenceRotation {
			get { return transform.parent.parent.localRotation; }
			set { transform.parent.parent.localRotation = value; }
		}
		const float circumferenceRotationLerpSpeed = 2.5f;

		Animator upAndDownAnim;
		public float curUpAndDownAnimTime = 0.15f;
		float desiredUpAndDownAnimTime = 0.15f;
		const float upAndDownAnimLerpSpeed = 10f;
		const float verticalMovespeed = .15f;

		protected override void Awake() {
			base.Awake();
			upAndDownAnim = GetComponent<Animator>();
			sideToSideAnim = transform.parent.GetComponent<Animator>();
			desiredUpAndDownAnimTime = curUpAndDownAnimTime;
			desiredCircumferenceRotation = curCircumferenceRotation;
		}

		protected override void Start() {
			base.Start();
			if (upAndDownAnim != null) {
				upAndDownAnim.Play("ProjectorUpDown", 0, curUpAndDownAnimTime);
			}
			if (sideToSideAnim != null) {
				sideToSideAnim.Play("ProjectorSideToSide", 1, curSideToSideAnimTime);
			}
		}

		// Update is called once per frame
		void Update() {
			// Debug controls
			if (DEBUG) {
				if (Input.GetKey("f")) {
					if (Input.GetKey(KeyCode.LeftShift)) {
						DecreaseFrustumSize();
					}
					else {
						IncreaseFrustumSize();
					}
				}
				if (Input.GetKey("g")) {
					ChangeAngle(Input.GetKey(KeyCode.LeftShift) ? -rotationSpeed : rotationSpeed);
				}
				if (Input.GetKey("h")) {
					RotateAroundCircumference(Input.GetKey(KeyCode.LeftShift) ? -1 : 1);
				}
				if (Input.GetKey("j")) {
					MoveProjectorVertical(Input.GetKey(KeyCode.LeftShift) ? -verticalMovespeed : verticalMovespeed);
				}
			}

			if (upAndDownAnim != null) {
				curUpAndDownAnimTime = Mathf.Lerp(curUpAndDownAnimTime, desiredUpAndDownAnimTime, upAndDownAnimLerpSpeed * Time.deltaTime);
				upAndDownAnim.Play("ProjectorUpDown", 0, curUpAndDownAnimTime);
			}
			if (sideToSideAnim != null) {
				curSideToSideAnimTime = Mathf.Lerp(curSideToSideAnimTime, desiredSideToSideAnimTime, sideToSideAnimLerpSpeed * Time.deltaTime);
				sideToSideAnim.Play("ProjectorSideToSide", 1, curSideToSideAnimTime);
			}

			curCircumferenceRotation = Quaternion.Lerp(curCircumferenceRotation, desiredCircumferenceRotation, circumferenceRotationLerpSpeed * Time.deltaTime);
		}

		public void IncreaseFrustumSize() {
			ChangeFrustumSize(1 + frustumSizeChangeSpeed * Time.deltaTime / 100f);
		}

		public void DecreaseFrustumSize() {
			ChangeFrustumSize(1 - frustumSizeChangeSpeed * Time.deltaTime / 100f);
		}

		public void RotateAngleLeft() {
			ChangeAngle(-rotationSpeed);
		}

		public void RotateAngleRight() {
			ChangeAngle(rotationSpeed);
		}

		public void RotateAngleDown() {
			MoveProjectorVertical(-verticalMovespeed);
		}

		public void RotateAngleUp() {
			MoveProjectorVertical(verticalMovespeed);
		}

		// Stretches the far plane of the frustum within the bounds minSize <-> maxSize
		void ChangeFrustumSize(float multiplier) {
			currentSize *= multiplier;
			if (currentSize < minSize || currentSize > maxSize) {
				currentSize = Mathf.Clamp(currentSize, minSize, maxSize);
				transform.GetChild(0).localScale = new Vector3(currentSize, transform.GetChild(0).localScale.y, currentSize);
				return;
			}
			transform.GetChild(0).localScale = new Vector3(currentSize, transform.GetChild(0).localScale.y, currentSize);
		}

		// Rotates the projector along the y-axis of rotation, within the bounds minRotation <-> maxRotation
		void ChangeAngle(float rotation) {
			rotation *= Time.deltaTime;
			if (sideToSideAnim != null) {
				desiredSideToSideAnimTime = Mathf.Clamp01(desiredSideToSideAnimTime + rotation);
				// Prevent animation wrap-around
				if (desiredSideToSideAnimTime == 1) desiredSideToSideAnimTime = 0.9999f;
			}
		}

		// Moves the projector along the circumference of the puzzle area by rotating its parent gameobject's transform
		public void RotateAroundCircumference(float rotation) {
			desiredCircumferenceRotation = Quaternion.Euler(desiredCircumferenceRotation.eulerAngles + Vector3.up * rotation);
		}

		void MoveProjectorVertical(float amount) {
			amount *= Time.deltaTime;
			if (upAndDownAnim != null) {
				desiredUpAndDownAnimTime = Mathf.Clamp01(desiredUpAndDownAnimTime + amount);
			}
		}

		#region Saving
		public override string ID => $"{gameObject.name}";

		[Serializable]
		public class LightProjectorSave : SerializableSaveObject<LightProjector> {
			float currentSize;

			float curSideToSideAnimTime;
			float desiredSideToSideAnimTime;

			SerializableQuaternion desiredCircumferenceRotation;
			SerializableQuaternion curCircumferenceRotation;

			float curUpAndDownAnimTime;
			float desiredUpAndDownAnimTime;

			public LightProjectorSave(LightProjector lightProjector) {
				this.currentSize = lightProjector.currentSize;

				this.curSideToSideAnimTime = lightProjector.curSideToSideAnimTime;
				this.desiredSideToSideAnimTime = lightProjector.desiredSideToSideAnimTime;

				this.desiredCircumferenceRotation = lightProjector.desiredCircumferenceRotation;
				this.curCircumferenceRotation = lightProjector.curCircumferenceRotation;

				this.curUpAndDownAnimTime = lightProjector.curUpAndDownAnimTime;
				this.desiredUpAndDownAnimTime = lightProjector.desiredUpAndDownAnimTime;
			}

			public override void LoadSave(LightProjector lightProjector) {
				lightProjector.currentSize = this.currentSize;

				lightProjector.curSideToSideAnimTime = this.curSideToSideAnimTime;
				lightProjector.desiredSideToSideAnimTime = this.desiredSideToSideAnimTime;
				
				lightProjector.desiredCircumferenceRotation = this.desiredCircumferenceRotation;
				lightProjector.curCircumferenceRotation = this.curCircumferenceRotation;
				
				lightProjector.curUpAndDownAnimTime = this.curUpAndDownAnimTime;
				lightProjector.desiredUpAndDownAnimTime = this.desiredUpAndDownAnimTime;

				lightProjector.ChangeFrustumSize(1);
			}
		}
		#endregion
	}
}