using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Interactables;
using LevelManagement;
using LevelSpecific.WhiteRoom.CathedralTutorial;
using MagicTriggerMechanics;
using UnityEngine;
using Saving;
using SerializableClasses;
using StateUtils;
using SuperspectiveUtils;
using PuzzlePanel = LevelSpecific.WhiteRoom.CathedralTutorial.PuzzlePanel;

namespace LevelSpecific.WhiteRoom.CathedralTutorial {
	[RequireComponent(typeof(UniqueId))]
	public class ElevatorButton : SuperspectiveObject<ElevatorButton, ElevatorButton.ElevatorButtonSave> {
		public Transform elevator;
		public SuperspectiveReference<ValueDisplay, ValueDisplay.ValueDisplaySave> topFloorCompareAgainst;
		public CurrentValueDisplay puzzleFloorCompareAgainst;
		public ValueDisplay buttonValue;
		public Button button;
		private InteractableObject interactableObject;
		public GameObject invisibleWalls;
		public MagicTrigger playerOnElevatorTrigger;

		public enum ButtonState : byte {
			Idle,
			Incorrect,
			Correct
		}

		public StateMachine<ButtonState> buttonState;

		public enum FloorState : byte {
			TopFloor,
			PuzzleFloor,
			SecretFloor
		}

		public StateMachine<FloorState> floorState;

		// TODO: Replace w/ Elevator component
		public enum ElevatorState : byte {
			Idle,
			MovingDown
		}

		public StateMachine<ElevatorState> elevatorState;
		public AnimationCurve elevatorCameraShakeIntensity;

		private const float ELEVATOR_SPEED = 4f;

		private float ElevatorTime(FloorState fromFloor) {
			float distance = 0;
			switch (fromFloor) {
				case FloorState.TopFloor:
					distance = (topFloorWaypoint.position - teleportPointEnterWaypoint.position).magnitude +
					           (teleportPointExitWaypoint.position - puzzleFloorWaypoint.position).magnitude;
					break;
				case FloorState.PuzzleFloor:
					distance = (teleportPointExitWaypoint.position - puzzleFloorWaypoint.position).magnitude;
					break;
				case FloorState.SecretFloor:
					distance = (puzzleFloorWaypoint.position - secretFloorWaypoint.position).magnitude;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(fromFloor), fromFloor, null);
			}

			return distance / ELEVATOR_SPEED;
		}

		private const float ELEVATOR_SHAKE_MAX_INTENSITY = .6f;
		private const float ELEVATOR_DELAY = 2.5f;

		[Header("Elevator Waypoints")]
		public Transform topFloorWaypoint;

		public Transform teleportPointEnterWaypoint;
		public Transform teleportPointExitWaypoint;
		public Transform puzzleFloorWaypoint;
		public Transform secretFloorWaypoint;

		private float TeleportTime => (topFloorWaypoint.position - teleportPointEnterWaypoint.position).magnitude / ELEVATOR_SPEED;

		private Color IncorrectColor => PuzzlePanel.incorrectColor;
		private Color CorrectColor => PuzzlePanel.correctColor;
		private int IncorrectFlashTimes => PuzzlePanel.INCORRECT_FLASH_TIMES;
		private float IncorrectFlashDuration => PuzzlePanel.INCORRECT_FLASH_DURATION;
		private float CurrentValueAlphaLerpSpeed => PuzzlePanel.CURRENT_VALUE_ALPHA_LERP_SPEED;

		protected override void Start() {
			base.Start();

			buttonState = this.StateMachine(ButtonState.Idle);
			floorState = this.StateMachine(FloorState.TopFloor);
			elevatorState = this.StateMachine(ElevatorState.Idle);

			interactableObject = GetComponent<InteractableObject>();
			button.OnButtonPressFinish += OnButtonPress;

			InitButtonStateMachine();
			InitElevatorStateMachine();
		}

		void InitButtonStateMachine() {
			buttonState.AddStateTransition(ButtonState.Incorrect, ButtonState.Idle, IncorrectFlashDuration);
			buttonState.AddStateTransition(ButtonState.Correct, ButtonState.Idle, ElevatorTime(floorState));

			buttonState.AddTrigger(ButtonState.Correct, 0, () => AudioManager.instance.Play(AudioName.CorrectAnswer));
			buttonState.AddTrigger(ButtonState.Correct, ELEVATOR_DELAY, () => elevatorState.Set(ElevatorState.MovingDown));
		}

		void InitElevatorStateMachine() {
			elevatorState.AddStateTransition(ElevatorState.MovingDown, ElevatorState.Idle, () => elevatorState.Time >= ElevatorTime(floorState));

			elevatorState.AddTrigger(ElevatorState.MovingDown, 0f, () => CameraShake.instance.Shake(new CameraShake.CameraShakeEvent {
				intensity = ELEVATOR_SHAKE_MAX_INTENSITY,
				duration = ElevatorTime(floorState),
				intensityCurve = elevatorCameraShakeIntensity,
				spatial = 0
			}));
			elevatorState.AddTrigger(ElevatorState.MovingDown, TeleportTime, () => {
				if (floorState == FloorState.TopFloor && elevatorState == ElevatorState.MovingDown) {
					// Teleport the player and change the level
					Vector3 diff = teleportPointExitWaypoint.position - teleportPointEnterWaypoint.position;
					elevator.position = teleportPointExitWaypoint.position;
					Player.instance.transform.position += diff;
					Player.instance.cameraFollow.RecalculateWorldPositionLastFrame();
					LevelManager.instance.SwitchActiveScene(Levels.ForkCathedralTutorial);
				}
			});
			elevatorState.AddTrigger(ElevatorState.Idle, 0f, () => {
				if (elevatorState.PrevState != ElevatorState.MovingDown) return;
				switch (floorState.State) {
					case FloorState.TopFloor:
						floorState.Set(FloorState.PuzzleFloor);
						break;
					case FloorState.PuzzleFloor:
						floorState.Set(FloorState.SecretFloor);
						break;
					case FloorState.SecretFloor:
						floorState.Set(FloorState.PuzzleFloor);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			});
		}

		void OnButtonPress(Button b) {
			if (buttonState.State != ButtonState.Idle) return;

			buttonState.Set(CheckAnswer() ? ButtonState.Correct : ButtonState.Incorrect);
		}

		int TopFloorValue() {
			return topFloorCompareAgainst.Reference.Match(
				valueDisplay => valueDisplay.actualValue,
				saveObject => saveObject.actualValue
			);
		}

		private bool CheckAnswer() {
			switch (floorState.State) {
				case FloorState.TopFloor:
					return buttonValue.actualValue == TopFloorValue();
				case FloorState.PuzzleFloor:
					return DEBUG || buttonValue.actualValue == puzzleFloorCompareAgainst.actualValue;
				case FloorState.SecretFloor:
					return true;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		void Update() {
			if (GameManager.instance.IsCurrentlyLoading) return;

			UpdateButton();
			UpdateElevator();
		}

		void UpdateElevator() {
			bool wallsUp = elevatorState == ElevatorState.MovingDown || buttonState == ButtonState.Correct;
			invisibleWalls.SetActive(wallsUp);

			switch (elevatorState.State) {
				case ElevatorState.Idle:
					switch (floorState.State) {
						case FloorState.TopFloor:
							elevator.position = topFloorWaypoint.position;
							break;
						case FloorState.PuzzleFloor:
							elevator.position = puzzleFloorWaypoint.position;
							break;
						case FloorState.SecretFloor:
							elevator.position = secretFloorWaypoint.position;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					break;
				case ElevatorState.MovingDown:
					float t = elevatorState.Time / ElevatorTime(floorState);
					Vector3 nextPos = elevator.position;
					switch (floorState.State) {
						case FloorState.TopFloor:
							t = (elevatorState.Time < TeleportTime) ? elevatorState.Time / TeleportTime : (elevatorState.Time - TeleportTime) / (ElevatorTime(floorState) - TeleportTime);
							nextPos = (elevatorState.Time < TeleportTime)
								? Vector3.Lerp(topFloorWaypoint.position, teleportPointEnterWaypoint.position, t)
								: Vector3.Lerp(teleportPointExitWaypoint.position, puzzleFloorWaypoint.position, t);
							break;
						case FloorState.PuzzleFloor:
							nextPos = Vector3.Lerp(teleportPointExitWaypoint.position, secretFloorWaypoint.position, t);
							break;
						case FloorState.SecretFloor:
							nextPos = Vector3.Lerp(secretFloorWaypoint.position, puzzleFloorWaypoint.position, t);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					Vector3 diff = nextPos - elevator.position;
					elevator.position = nextPos;
					Player.instance.transform.position += diff;
					break;
			}
		}

		void UpdateButton() {
			switch (buttonState.State) {
				case ButtonState.Idle:
					// Disable the button while moving down (or just after)
					if (elevatorState == ElevatorState.MovingDown || (elevatorState.PrevState == ElevatorState.MovingDown && elevatorState.Time < .5f)) {
						interactableObject.SetAsDisabled("(Already moving)");
					}
					// Hide it when we enter the puzzle room
					else if (FloorManager.instance.CurrentValue == 0 && floorState != FloorState.TopFloor) {
						interactableObject.SetAsHidden();
					}
					else if (floorState.Time > 1.25f) {
						interactableObject.SetAsInteractable("Operate elevator");
					}

					buttonValue.desiredColor = buttonValue.defaultColor;
					if (floorState == FloorState.TopFloor) {
						ResetCompareAgainstColor();
					}

					break;
				case ButtonState.Incorrect:
					interactableObject.SetAsDisabled();

					float t = 0.5f + 0.5f * Mathf.Cos(buttonState.Time * IncorrectFlashTimes * 2 * Mathf.PI / IncorrectFlashDuration + Mathf.PI);
					buttonValue.SetColorImmediately(Color.Lerp(buttonValue.defaultColor, IncorrectColor, t));
					break;
				case ButtonState.Correct:
					interactableObject.SetAsHidden();

					buttonValue.desiredColor = CorrectColor;
					SetCompareAgainstColor(CorrectColor);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (!playerOnElevatorTrigger.playerIsInTriggerZone) {
				interactableObject.SetAsHidden();
			}
		}

		void ResetCompareAgainstColor() {
			switch (floorState.State) {
				case FloorState.TopFloor:
					ResetTopFloorValueDisplayDesiredColor();
					break;
				case FloorState.PuzzleFloor:
					puzzleFloorCompareAgainst.desiredColor = puzzleFloorCompareAgainst.defaultColor;
					break;
				case FloorState.SecretFloor:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ResetTopFloorValueDisplayDesiredColor() {
			topFloorCompareAgainst.Reference?.MatchAction(
				valueDisplay => valueDisplay.desiredColor = valueDisplay.defaultColor,
				valueDisplaySave => valueDisplaySave.desiredColor = valueDisplaySave.defaultColor
			);
		}

		void SetCompareAgainstColor(Color color) {
			switch (floorState.State) {
				case FloorState.TopFloor:
					SetTopFloorValueDisplayDesiredColor(color);
					break;
				case FloorState.PuzzleFloor:
					puzzleFloorCompareAgainst.desiredColor = color;
					break;
				case FloorState.SecretFloor:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		void SetTopFloorValueDisplayDesiredColor(Color color) {
			topFloorCompareAgainst.Reference.MatchAction(
				valueDisplay => valueDisplay.desiredColor = color,
				valueDisplaySave => valueDisplaySave.desiredColor = color
			);
		}

		// Reset the elevator to the top floor when we leave this area (since you can only enter it from the top floor)
		// Called from a UnityEvent on a MagicTrigger on the way down to the Cathedral
		public void ResetElevatorToTopFloor() {
			buttonState.Set(ButtonState.Idle);
			elevatorState.Set(ElevatorState.Idle);
			floorState.Set(FloorState.TopFloor);

			SetTopFloorValueDisplayDesiredColor(Color.black);
		}

#region Saving

		public override void LoadSave(ElevatorButtonSave save) {
			buttonState.LoadFromSave(save.buttonStateSave);
			elevatorState.LoadFromSave(save.elevatorStateSave);
			floorState.LoadFromSave(save.floorStateSave);
		}

		[Serializable]
		public class ElevatorButtonSave : SaveObject<ElevatorButton> {
			public StateMachineSave<ButtonState> buttonStateSave;
			public StateMachineSave<ElevatorState> elevatorStateSave;
			public StateMachineSave<FloorState> floorStateSave;

			public ElevatorButtonSave(ElevatorButton script) : base(script) {
				this.buttonStateSave = script.buttonState.ToSave();
				this.elevatorStateSave = script.elevatorState.ToSave();
				this.floorStateSave = script.floorState.ToSave();
			}
		}
#endregion
	}
}
