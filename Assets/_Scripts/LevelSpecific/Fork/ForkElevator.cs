using Audio;
using EpitaphUtils;
using NaughtyAttributes;
using Saving;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tayx.Graphy.Utils.NumString;
using UnityEngine;

namespace LevelSpecific.Fork {
	public class ForkElevator : MonoBehaviour, SaveableObject {
		public enum State {
			Idle,
			DoorsClosing,
			ElevatorMoving,
			DoorsOpening
		}

		[SerializeField][ReadOnly]
		private State _state = State.Idle;
		public State state {
			get { return _state; }
			set {
				timeElapsedSinceStateChange = 0f;
				_state = value;
			}
		}
		public bool goingDown = true;
		float timeElapsedSinceStateChange = 0f;

		public AnimationCurve lockBarAnimation;
		public Transform[] lockBars;
		public Transform elevator;
		public Transform lockBeam;
		public GameObject invisibleElevatorWall;
		public Button elevatorButton;
		const float height = 21.5f;
		float raisedHeight;
		float loweredHeight;

		const float lockBarDelayTime = 0.25f;
		const float unlockBarDelayTime = 0.125f;
		const float timeToLockDoors = 2f;
		const float timeToUnlockDoors = .75f;
		const float lockBeamMinSize = 0.125f;
		float curSpeed = 0f;
		const float maxSpeed = 6f;

		bool playerStandingInElevator = false;

		void Start() {
			raisedHeight = transform.parent.position.y;
			loweredHeight = raisedHeight - height;

			elevatorButton.OnButtonPressBegin += (ctx) => RaiseLowerElevator(true);
			elevatorButton.OnButtonDepressBegin += (ctx) => RaiseLowerElevator(false);

			// Saving
			ID = GetComponent<UniqueId>().uniqueId;
		}

		private void FixedUpdate() {
			elevatorButton.interactableObject.interactable = playerStandingInElevator && state == State.Idle;

			switch (state) {
				case State.Idle:
					foreach (var lockBar in lockBars) {
						lockBar.localScale = new Vector3(1, 0, 1);
					}
					lockBeam.localScale = new Vector3(lockBeam.localScale.x, lockBeam.localScale.y, 1f);
					return;
				case State.DoorsClosing:
					UpdateDoorClosingAnimation();
					break;
				case State.ElevatorMoving:
					foreach (var lockBar in lockBars) {
						lockBar.localScale = new Vector3(1, 1, 1);
					}
					lockBeam.localScale = new Vector3(lockBeam.localScale.x, lockBeam.localScale.y, lockBeamMinSize);
					UpdateElevatorMovingAnimation();
					break;
				case State.DoorsOpening:
					UpdateDoorOpeningAnimation();
					break;
			}

			timeElapsedSinceStateChange += Time.fixedDeltaTime;
		}

		void UpdateDoorClosingAnimation() {
			if (timeElapsedSinceStateChange <= Time.fixedDeltaTime) {
				invisibleElevatorWall.SetActive(true);
				AudioManager.instance.PlayOnGameObject(AudioName.ElevatorClose, ID, gameObject, true);
				CameraShake.instance.Shake(timeToLockDoors, 0.25f, 0f);
			}

			float totalAnimationTime = timeToLockDoors + (lockBars.Length / 2) * lockBarDelayTime;
			if (timeElapsedSinceStateChange < totalAnimationTime) {
				float t = timeElapsedSinceStateChange / timeToLockDoors;

				for (int i = 0; i < lockBars.Length; i++) {
					float thisBarTime = Mathf.Clamp01((timeElapsedSinceStateChange - lockBarDelayTime * (i / 2)) / timeToLockDoors);
					Vector3 curScale = lockBars[i].localScale;
					curScale.y = lockBarAnimation.Evaluate(thisBarTime);
					lockBars[i].localScale = curScale;
				}
				lockBeam.localScale = new Vector3(lockBeam.localScale.x, lockBeam.localScale.y, Mathf.Lerp(1f, lockBeamMinSize, t));
			}
			else {
				invisibleElevatorWall.SetActive(false);

				// Transition state to ElevatorMoving after waiting .1 additional seconds
				if (timeElapsedSinceStateChange >= totalAnimationTime + 0.1f) {
					CameraShake.instance.Shake(5f, 0.0625f, 0.0625f);
					AudioManager.instance.PlayOnGameObject(AudioName.ElevatorMove, ID, gameObject, true);

					state = State.ElevatorMoving;
				}
			}
		}

		void UpdateDoorOpeningAnimation() {
			if (timeElapsedSinceStateChange <= Time.fixedDeltaTime) {
				AudioManager.instance.PlayOnGameObject(AudioName.ElevatorOpen, ID, gameObject, true);
				CameraShake.instance.Shake(timeToUnlockDoors, 0.25f, 0f);
			}

			float totalAnimationTime = timeToUnlockDoors + (lockBars.Length / 2) * unlockBarDelayTime;
			if (timeElapsedSinceStateChange < totalAnimationTime) {
				float t = timeElapsedSinceStateChange / timeToUnlockDoors;

				for (int i = 0; i < lockBars.Length; i++) {
					float thisBarTime = 1 - Mathf.Clamp01((timeElapsedSinceStateChange - unlockBarDelayTime * (i / 2)) / timeToUnlockDoors);
					Vector3 curScale = lockBars[i].localScale;
					curScale.y = lockBarAnimation.Evaluate(thisBarTime);
					lockBars[i].localScale = curScale;
				}
				lockBeam.localScale = new Vector3(lockBeam.localScale.x, lockBeam.localScale.y, Mathf.Lerp(1f, lockBeamMinSize, 1 - t));
			}
			else {
				lockBeam.localScale = new Vector3(lockBeam.localScale.x, lockBeam.localScale.y, 1f);
				// Transition state to Idle after waiting .1 additional seconds
				if (timeElapsedSinceStateChange >= totalAnimationTime + 0.1f) {
					state = State.Idle;
				}
			}
		}

		void UpdateElevatorMovingAnimation() {
			if (goingDown ? (elevator.position.y - 0.2f > loweredHeight) : (elevator.position.y + 0.2f < raisedHeight)) {
				curSpeed = Mathf.Lerp(curSpeed, maxSpeed, Time.fixedDeltaTime);

				float nextHeight = elevator.position.y;
				nextHeight += (goingDown ? -1 : 1) * curSpeed * Time.fixedDeltaTime;
				nextHeight = Mathf.Clamp(nextHeight, loweredHeight, raisedHeight);
				Vector3 curPos = elevator.position;
				Vector3 nextPos = curPos;
				nextPos.y = nextHeight;
				elevator.position = nextPos;
				if (playerStandingInElevator) {
					Player.instance.transform.position += nextPos - curPos;
				}
			}
			else {
				CameraShake.instance.CancelShake();
				elevator.position = new Vector3(elevator.position.x, (goingDown ? loweredHeight : raisedHeight), elevator.position.z);

				curSpeed = 0f;
				// Reverse direction for next execution
				goingDown = !goingDown;

				state = State.DoorsOpening;
			}
		}

		private void OnTriggerEnter(Collider other) {
			playerStandingInElevator = true;
		}

		private void OnTriggerExit(Collider other) {
			playerStandingInElevator = false;
		}

		void RaiseLowerElevator(bool goingDown) {
			if (playerStandingInElevator && state == State.Idle) {
				state = State.DoorsClosing;
			}
		}

#region Saving
		[System.Serializable]
		class ForkElevatorSave {
			SerializableClasses.SerializableVector3 position;
			int state;
			bool goingDown;
			float timeElapsedSinceStateChange;
			float raisedHeight;
			float loweredHeight;
			float curSpeed;

			bool playerStandingInElevator;

			public ForkElevatorSave(ForkElevator elevator) {
				this.state = (int)elevator.state;
				this.goingDown = elevator.goingDown;
				this.timeElapsedSinceStateChange = elevator.timeElapsedSinceStateChange;
				this.raisedHeight = elevator.raisedHeight;
				this.loweredHeight = elevator.loweredHeight;
				this.curSpeed = elevator.curSpeed;
				this.playerStandingInElevator = elevator.playerStandingInElevator;
				this.position = elevator.transform.parent.position;
			}

			public void LoadSave(ForkElevator elevator) {
				elevator.state = (State)this.state;
				elevator.goingDown = this.goingDown;
				elevator.timeElapsedSinceStateChange = this.timeElapsedSinceStateChange;
				elevator.raisedHeight = this.raisedHeight;
				elevator.loweredHeight = this.loweredHeight;
				elevator.curSpeed = this.curSpeed;
				elevator.playerStandingInElevator = this.playerStandingInElevator;
				elevator.transform.parent.position = this.position;

				float t = 0f;
				switch (elevator.state) {
					case State.ElevatorMoving:
						t = Mathf.InverseLerp(elevator.loweredHeight, elevator.raisedHeight, elevator.transform.parent.position.y);
						if (elevator.goingDown) t = 1 - t;
						AudioManager.instance.PlayOnGameObject(AudioName.ElevatorMove, elevator.ID, elevator.gameObject, false, (audio) => audio.time = t * audio.clip.length);
						break;
					case State.DoorsOpening:
						float totalAnimationTime = timeToUnlockDoors + (elevator.lockBars.Length / 2) * unlockBarDelayTime;
						t = elevator.timeElapsedSinceStateChange / totalAnimationTime;
						AudioManager.instance.PlayOnGameObject(AudioName.ElevatorOpen, elevator.ID, elevator.gameObject, false, (audio) => audio.time = t * audio.clip.length);
						break;
					case State.DoorsClosing:
						totalAnimationTime = timeToLockDoors + (elevator.lockBars.Length / 2) * lockBarDelayTime;
						t = elevator.timeElapsedSinceStateChange / totalAnimationTime;
						AudioManager.instance.PlayOnGameObject(AudioName.ElevatorClose, elevator.ID, elevator.gameObject, false, (audio) => audio.time = t * audio.clip.length);
						break;
				}
			}
		}

		public bool SkipSave { get; set; }
		public string ID { get; private set; }
		public object GetSaveObject() {
			ForkElevatorSave save = new ForkElevatorSave(this);
			return save;
		}

		public void LoadFromSavedObject(object savedObject) {
			ForkElevatorSave save = savedObject as ForkElevatorSave;

			save.LoadSave(this);
		}
#endregion
	}
}