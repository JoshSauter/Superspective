using UnityEngine;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
	public class LightProjector : SuperspectiveObject<LightProjector, LightProjector.LightProjectorSave> {
		const float MIN_SIZE = .5f;
		const float MAX_SIZE = 2.5f;
		const float FRUSTUM_SIZE_CHANGE_SPEED = 200;
		const float SIDE_TO_SIDE_ANIM_LERP_SPEED = 10f;
		const float ROTATION_SPEED = .4f;
		const float CIRCUMFERENCE_ROTATION_LERP_SPEED = 2.5f;
		const float UP_AND_DOWN_ANIM_LERP_SPEED = 10f;
		const float VERTICAL_MOVESPEED = .15f;
		
		float currentSize = 1;

		Animator sideToSideAnim;
		public float curSideToSideAnimTime = 0.5f;
		float desiredSideToSideAnimTime = 0.5f;

		Quaternion desiredCircumferenceRotation;
		Quaternion curCircumferenceRotation {
			get { return transform.parent.parent.localRotation; }
			set { transform.parent.parent.localRotation = value; }
		}

		Animator upAndDownAnim;
		public float curUpAndDownAnimTime = 0.15f;
		float desiredUpAndDownAnimTime = 0.15f;

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
				if (DebugInput.GetKey("f")) {
					if (DebugInput.GetKey(KeyCode.LeftShift)) {
						DecreaseFrustumSize();
					}
					else {
						IncreaseFrustumSize();
					}
				}
				if (DebugInput.GetKey("g")) {
					ChangeAngle(DebugInput.GetKey(KeyCode.LeftShift) ? -ROTATION_SPEED : ROTATION_SPEED);
				}
				if (DebugInput.GetKey("h")) {
					RotateAroundCircumference(DebugInput.GetKey(KeyCode.LeftShift) ? -1 : 1);
				}
				if (DebugInput.GetKey("j")) {
					MoveProjectorVertical(DebugInput.GetKey(KeyCode.LeftShift) ? -VERTICAL_MOVESPEED : VERTICAL_MOVESPEED);
				}
			}

			if (upAndDownAnim != null) {
				curUpAndDownAnimTime = Mathf.Lerp(curUpAndDownAnimTime, desiredUpAndDownAnimTime, UP_AND_DOWN_ANIM_LERP_SPEED * Time.deltaTime);
				upAndDownAnim.Play("ProjectorUpDown", 0, curUpAndDownAnimTime);
			}
			if (sideToSideAnim != null) {
				curSideToSideAnimTime = Mathf.Lerp(curSideToSideAnimTime, desiredSideToSideAnimTime, SIDE_TO_SIDE_ANIM_LERP_SPEED * Time.deltaTime);
				sideToSideAnim.Play("ProjectorSideToSide", 1, curSideToSideAnimTime);
			}

			curCircumferenceRotation = Quaternion.Lerp(curCircumferenceRotation, desiredCircumferenceRotation, CIRCUMFERENCE_ROTATION_LERP_SPEED * Time.deltaTime);
		}

		// Returns true if the frustum size changed, false otherwise
		public bool IncreaseFrustumSize() {
			return ChangeFrustumSize(1 + FRUSTUM_SIZE_CHANGE_SPEED * Time.deltaTime / 100f);
		}

		// Returns true if the frustum size changed, false otherwise
		public bool DecreaseFrustumSize() {
			return ChangeFrustumSize(1 - FRUSTUM_SIZE_CHANGE_SPEED * Time.deltaTime / 100f);
		}

		public void RotateAngleLeft() {
			ChangeAngle(-ROTATION_SPEED);
		}

		public void RotateAngleRight() {
			ChangeAngle(ROTATION_SPEED);
		}

		public void RotateAngleDown() {
			MoveProjectorVertical(-VERTICAL_MOVESPEED);
		}

		public void RotateAngleUp() {
			MoveProjectorVertical(VERTICAL_MOVESPEED);
		}

		// Stretches the far plane of the frustum within the bounds minSize <-> maxSize
		// Returns true if the size is within minSize <-> maxSize, false otherwise
		bool ChangeFrustumSize(float multiplier) {
			currentSize *= multiplier;
			if (currentSize < MIN_SIZE || currentSize > MAX_SIZE) {
				currentSize = Mathf.Clamp(currentSize, MIN_SIZE, MAX_SIZE);
				transform.GetChild(0).localScale = new Vector3(currentSize, transform.GetChild(0).localScale.y, currentSize);
				return false;
			}
			transform.GetChild(0).localScale = new Vector3(currentSize, transform.GetChild(0).localScale.y, currentSize);
			return true;
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

		public override void LoadSave(LightProjectorSave save) {
			ChangeFrustumSize(1); // Forces refresh of scale changes
		}

		public override string ID => $"{gameObject.name}";

		[Serializable]
		public class LightProjectorSave : SaveObject<LightProjector> {
			public LightProjectorSave(LightProjector lightProjector) : base(lightProjector) { }
		}
#endregion
	}
}
