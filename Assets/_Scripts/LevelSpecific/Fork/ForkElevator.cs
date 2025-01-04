using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Interactables;
using NaughtyAttributes;
using PowerTrailMechanics;
using Saving;
using SerializableClasses;
using UnityEngine;
using PickupObjectRef = SerializableClasses.SuperspectiveReference<PickupObject, PickupObject.PickupObjectSave>;

namespace LevelSpecific.Fork {
	[RequireComponent(typeof(UniqueId))]
	public class ForkElevator : SuperspectiveObject<ForkElevator, ForkElevator.ForkElevatorSave>, AudioJobOnGameObject {
		public enum State : byte {
			NotPowered,
			Idle,
			DoorsClosing,
			ElevatorMoving,
			DoorsOpening
		}

		[SerializeField]
		[ReadOnly]
		State _state = State.NotPowered;
		public State state {
			get => _state;
			set {
				timeElapsedSinceStateChange = 0f;
				_state = value;
			}
		}
		float timeElapsedSinceStateChange = 0f;


		public PowerTrail initialPowerTrail;
		public AnimationCurve lockBarAnimation;
		public Transform[] lockBars;
		public Transform elevator;
		public Transform lockBeam;
		public GameObject invisibleElevatorWall;
		public Button elevatorButton;
		float raisedHeight;
		float loweredHeight;

		const float HEIGHT = 21.5f;
		const float LOCK_BAR_DELAY_TIME = 0.25f;
		const float UNLOCK_BAR_DELAY_TIME = 0.125f;
		const float TIME_TO_LOCK_DOORS = 2f;
		const float TIME_TO_UNLOCK_DOORS = .75f;
		const float LOCK_BEAM_MIN_SIZE = 0.125f;
		const float MAX_SPEED = 6f;
		
		float curSpeed = 0f;
		public bool goingDown = true;

		private CameraShake.CameraShakeEvent cameraShake;

		List<PickupObjectRef> otherObjectsInElevator = new List<PickupObjectRef>();
		bool playerStandingInElevator = false;

		public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob _) => transform;

		protected override void Start() {
			base.Start();
			raisedHeight = transform.parent.position.y;
			loweredHeight = raisedHeight - HEIGHT;

			elevatorButton.OnButtonPressBegin += (ctx) => RaiseLowerElevator(true);
			elevatorButton.OnButtonUnpressBegin += (ctx) => RaiseLowerElevator(false);

			initialPowerTrail.pwr.OnPowerFinish += () => {
				if (state == State.NotPowered) state = State.DoorsOpening;
			};
			initialPowerTrail.pwr.OnDepowerBegin += () => {
				if (state == State.Idle) state = State.DoorsClosing;
			};
		}

		private string DisabledText() {
			switch (state) {
				case State.NotPowered:
					return "(Missing power)";
				case State.Idle:
					return "";
				case State.DoorsClosing:
					return "(Doors closing)";
				case State.ElevatorMoving:
					return "(Already moving)";
				case State.DoorsOpening:
					return "(Doors opening)";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		void UpdateButtonInteractibility() {
			if (!playerStandingInElevator) {
				elevatorButton.interactableObject.SetAsHidden();
			}
			else if (!initialPowerTrail.pwr.PowerIsOn || state != State.Idle) {
				elevatorButton.interactableObject.SetAsDisabled(DisabledText());
			}
			else {
				elevatorButton.interactableObject.SetAsInteractable("Operate elevator");
			}
		}

		void FixedUpdate() {
			UpdateButtonInteractibility();

			switch (state) {
				case State.NotPowered:
					foreach (var lockBar in lockBars) {
						lockBar.localScale = Vector3.one;
					}
					lockBeam.localScale = new Vector3(lockBeam.localScale.x, lockBeam.localScale.y, 1f);
					break;
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
					lockBeam.localScale = new Vector3(lockBeam.localScale.x, lockBeam.localScale.y, LOCK_BEAM_MIN_SIZE);
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
				AudioManager.instance.PlayOnGameObject(AudioName.ElevatorClose, ID, this, true);
				cameraShake = CameraShake.instance.Shake(5f, TIME_TO_LOCK_DOORS);
			}

			float totalAnimationTime = TIME_TO_LOCK_DOORS + (lockBars.Length / 2) * LOCK_BAR_DELAY_TIME;
			if (timeElapsedSinceStateChange < totalAnimationTime) {
				float t = timeElapsedSinceStateChange / TIME_TO_LOCK_DOORS;

				for (int i = 0; i < lockBars.Length; i++) {
					float thisBarTime = Mathf.Clamp01((timeElapsedSinceStateChange - LOCK_BAR_DELAY_TIME * (i / 2)) / TIME_TO_LOCK_DOORS);
					Vector3 curScale = lockBars[i].localScale;
					curScale.y = lockBarAnimation.Evaluate(thisBarTime);
					lockBars[i].localScale = curScale;
				}
				lockBeam.localScale = new Vector3(lockBeam.localScale.x, lockBeam.localScale.y, Mathf.Lerp(1f, LOCK_BEAM_MIN_SIZE, t));
			}
			else {
				invisibleElevatorWall.SetActive(false);

				// Transition state to ElevatorMoving after waiting .1 additional seconds
				if (timeElapsedSinceStateChange >= totalAnimationTime + 0.1f) {
					if (initialPowerTrail.pwr.PowerIsOn) {
						CameraShake.CameraShakeEvent shakeEvent = new CameraShake.CameraShakeEvent() {
							duration = 5f,
							intensity = 1.25f,
							intensityCurve = AnimationCurve.Constant(0, 1, 1),
							spatial = 0
						};
						cameraShake = CameraShake.instance.Shake(shakeEvent);
						AudioManager.instance.PlayOnGameObject(AudioName.ElevatorMove, ID, this, true);
					}

					state = initialPowerTrail.pwr.PowerIsOn ? State.ElevatorMoving : State.NotPowered;
				}
			}
		}

		void UpdateDoorOpeningAnimation() {
			if (timeElapsedSinceStateChange <= Time.fixedDeltaTime) {
				AudioManager.instance.PlayOnGameObject(AudioName.ElevatorOpen, ID, this, true);
				cameraShake = CameraShake.instance.Shake(5f, TIME_TO_UNLOCK_DOORS);
			}

			float totalAnimationTime = TIME_TO_UNLOCK_DOORS + (lockBars.Length / 2) * UNLOCK_BAR_DELAY_TIME;
			if (timeElapsedSinceStateChange < totalAnimationTime) {
				float t = timeElapsedSinceStateChange / TIME_TO_UNLOCK_DOORS;

				for (int i = 0; i < lockBars.Length; i++) {
					float thisBarTime = 1 - Mathf.Clamp01((timeElapsedSinceStateChange - UNLOCK_BAR_DELAY_TIME * (i / 2)) / TIME_TO_UNLOCK_DOORS);
					Vector3 curScale = lockBars[i].localScale;
					curScale.y = lockBarAnimation.Evaluate(thisBarTime);
					lockBars[i].localScale = curScale;
				}
				lockBeam.localScale = new Vector3(lockBeam.localScale.x, lockBeam.localScale.y, Mathf.Lerp(1f, LOCK_BEAM_MIN_SIZE, 1 - t));
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
				curSpeed = Mathf.Lerp(curSpeed, MAX_SPEED, Time.fixedDeltaTime);

				float nextHeight = elevator.position.y;
				nextHeight += (goingDown ? -1 : 1) * curSpeed * Time.fixedDeltaTime;
				nextHeight = Mathf.Clamp(nextHeight, loweredHeight, raisedHeight);
				Vector3 curPos = elevator.position;
				Vector3 nextPos = curPos;
				nextPos.y = nextHeight;
				elevator.position = nextPos;

				Vector3 diff = nextPos - curPos;
				if (playerStandingInElevator) {
					Player.instance.transform.position += diff;
				}

				foreach (var pickupObj in otherObjectsInElevator) {
					pickupObj.GetOrNull().transform.position += diff;
				}
				otherObjectsInElevator.Clear();
			}
			else {
				CameraShake.instance.CancelShake(cameraShake);
				elevator.position = new Vector3(elevator.position.x, (goingDown ? loweredHeight : raisedHeight), elevator.position.z);

				curSpeed = 0f;
				// Reverse direction for next execution
				goingDown = !goingDown;

				state = initialPowerTrail.pwr.PowerIsOn ? State.DoorsOpening : State.NotPowered;
			}
		}

		void OnTriggerStay(Collider other) {
			if (other.gameObject.TryGetComponent(out PickupObject pickup)) {
				if (!otherObjectsInElevator.Contains(pickup)) {
					otherObjectsInElevator.Add(pickup);
				}
			}
		}

		void OnTriggerEnter(Collider other) {
			playerStandingInElevator = true;
		}

		void OnTriggerExit(Collider other) {
			playerStandingInElevator = false;
		}

		void RaiseLowerElevator(bool goingDown) {
			if (playerStandingInElevator && state == State.Idle) {
				state = State.DoorsClosing;
			}
		}

#region Saving

		public override void LoadSave(ForkElevatorSave save) {
			state = save.state;
			otherObjectsInElevator = save.otherObjectsInElevator.ToList();
			goingDown = save.goingDown;
			timeElapsedSinceStateChange = save.timeElapsedSinceStateChange;
			raisedHeight = save.raisedHeight;
			loweredHeight = save.loweredHeight;
			curSpeed = save.curSpeed;
			playerStandingInElevator = save.playerStandingInElevator;
			transform.parent.position = save.position;
		}

		[Serializable]
		public class ForkElevatorSave : SaveObject<ForkElevator> {
			public SerializableVector3 position;
			public PickupObjectRef[] otherObjectsInElevator;
			public State state;
			public float timeElapsedSinceStateChange;
			public float raisedHeight;
			public float loweredHeight;
			public float curSpeed;
			public bool goingDown;
			public bool playerStandingInElevator;

			public ForkElevatorSave(ForkElevator elevator) : base(elevator) {
				this.state = elevator.state;
				this.goingDown = elevator.goingDown;
				this.timeElapsedSinceStateChange = elevator.timeElapsedSinceStateChange;
				this.raisedHeight = elevator.raisedHeight;
				this.loweredHeight = elevator.loweredHeight;
				this.curSpeed = elevator.curSpeed;
				this.playerStandingInElevator = elevator.playerStandingInElevator;
				this.otherObjectsInElevator = elevator.otherObjectsInElevator.ToArray();
				this.position = elevator.transform.parent.position;
			}
		}
#endregion
	}
}