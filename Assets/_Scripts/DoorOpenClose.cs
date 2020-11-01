using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using Saving;
using System;
using SerializableClasses;
using System.Linq;

[RequireComponent(typeof(UniqueId))]
public class DoorOpenClose : MonoBehaviour, SaveableObject {
	UniqueId _id;
	UniqueId id {
		get {
			if (_id == null) {
				_id = GetComponent<UniqueId>();
			}
			return _id;
		}
	}
	public bool DEBUG = false;
	public AnimationCurve doorOpenCurve;
	public AnimationCurve doorCloseCurve;

	Transform[] doorPieces;

	public enum DoorState {
		Closed,
		Opening,
		Open,
		Closing
	}
	private DoorState _state = DoorState.Closed;
	public DoorState state {
		get { return _state; }
		set {
			if (_state == value) {
				return;
			}
			switch (value) {
				case DoorState.Closed:
					OnDoorCloseEnd?.Invoke();
					break;
				case DoorState.Opening:
					OnDoorOpenStart?.Invoke();
					break;
				case DoorState.Open:
					OnDoorOpenEnd?.Invoke();
					break;
				case DoorState.Closing:
					OnDoorCloseStart?.Invoke();
					break;
			}
			timeSinceStateChange = 0f;
			_state = value;
		}
	}
	float timeSinceStateChange = 0f;
	bool queueDoorClose = false;

	Vector3 closedScale;
	Vector3 openedScale;

	// Has to be re-asserted every physics timestep, else will close the door
	bool playerWasInTriggerZoneLastFrame = false;
	bool playerInTriggerZoneThisFrame = false;

	public float timeBetweenEachDoorPiece = 0.4f;
	public float timeForEachDoorPieceToOpen = 2f;
	public float timeForEachDoorPieceToClose = 0.5f;

	const float targetLocalXScale = 0;

#region events
	public delegate void DoorAction();
	public event DoorAction OnDoorOpenStart;
	public event DoorAction OnDoorCloseStart;
	public event DoorAction OnDoorOpenEnd;
	public event DoorAction OnDoorCloseEnd;
	#endregion

	private void Awake() {
		doorPieces = transform.GetComponentsInChildrenOnly<Transform>();
		closedScale = doorPieces[0].localScale;
		openedScale = new Vector3(targetLocalXScale, closedScale.y, closedScale.z);
	}
	
	// Update is called once per frame
	void Update () {
		if (!DEBUG) return;

		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R) && state != DoorState.Opening && state != DoorState.Closing) {
			ResetDoorPieceScales();
		}

		else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.O) && state != DoorState.Opening && state != DoorState.Closing) {
			state = DoorState.Closing;
		}

		else if (Input.GetKeyDown(KeyCode.O) && state != DoorState.Opening && state != DoorState.Closing) {
			state = DoorState.Opening;
		}
	}

	private void FixedUpdate() {
		if ((state == DoorState.Open || state == DoorState.Opening) && playerWasInTriggerZoneLastFrame && !playerInTriggerZoneThisFrame) {
			queueDoorClose = true;
		}

		if (queueDoorClose && state == DoorState.Open) {
			CloseDoor();
		}

		UpdateDoor();

		// Need to re-assert this every physics timestep, reset state
		playerWasInTriggerZoneLastFrame = playerInTriggerZoneThisFrame;
		playerInTriggerZoneThisFrame = false;
	}

	void UpdateDoor() {
		timeSinceStateChange += Time.fixedDeltaTime;
		switch (state) {
			case DoorState.Closed:
				break;
			case DoorState.Opening: {
					float totalTime = timeBetweenEachDoorPiece * (doorPieces.Length - 1) + timeForEachDoorPieceToOpen;
					if (timeSinceStateChange < totalTime) {
						for (int i = doorPieces.Length - 1; i >= 0; i--) {
							float timeIndex = doorPieces.Length - i - 1;
							float startTime = timeIndex * timeBetweenEachDoorPiece;
							float endTime = startTime + timeForEachDoorPieceToOpen;
							float t = Mathf.InverseLerp(startTime, endTime, timeSinceStateChange);

							doorPieces[i].localScale = Vector3.LerpUnclamped(closedScale, openedScale, doorOpenCurve.Evaluate(t));
						}
					}
					else {
						foreach (var piece in doorPieces) {
							piece.localScale = openedScale;
						}
						state = DoorState.Open;
					}
					break;
				}
			case DoorState.Open:
				break;
			case DoorState.Closing: {
					float totalTime = timeBetweenEachDoorPiece * (doorPieces.Length - 1) + timeForEachDoorPieceToClose;
					if (timeSinceStateChange < totalTime) {
						for (int i = doorPieces.Length - 1; i >= 0; i--) {
							float timeIndex = doorPieces.Length - i - 1;
							float startTime = timeIndex * timeBetweenEachDoorPiece;
							float endTime = startTime + timeForEachDoorPieceToClose;
							float t = Mathf.InverseLerp(startTime, endTime, timeSinceStateChange);

							doorPieces[i].localScale = Vector3.LerpUnclamped(openedScale, closedScale, doorCloseCurve.Evaluate(t));
						}
					}
					else {
						for (int i = 0; i < doorPieces.Length; i++) {
							doorPieces[i].localScale = closedScale;
						}
						state = DoorState.Closed;
					}
					break;
				}
		}
	}

	void ResetDoorPieceScales() {
		for (int i = 0; i < doorPieces.Length; i++) {
			doorPieces[i].localScale = closedScale;
		}
	}

	public void OpenDoor() {
		if (state == DoorState.Closed) {
			state = DoorState.Opening;
		}
	}

	public void CloseDoor() {
		if (state == DoorState.Open) {
			queueDoorClose = false;
			state = DoorState.Closing;
		}
	}

	private void OnTriggerStay(Collider other) {
		if (other.TaggedAsPlayer()) {
			if (state == DoorState.Closed) {
				OpenDoor();
			}
			playerInTriggerZoneThisFrame = true;
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.TaggedAsPlayer()) {
			queueDoorClose = true;
		}
	}

	#region Saving
	public bool SkipSave { get { return !gameObject.activeInHierarchy; } set { } }

	public string ID => $"DoorOpenClose_{id.uniqueId}";

	[Serializable]
	class DoorOpenCloseSave {
		SerializableAnimationCurve doorOpenCurve;
		SerializableAnimationCurve doorCloseCurve;

		SerializableVector3[] doorPieceScales;
		DoorState state;
		float timeSinceStateChange;
		bool queueDoorClose;

		SerializableVector3 closedScale;
		SerializableVector3 openedScale;

		bool playerWasInTriggerZoneLastFrame;
		bool playerInTriggerZoneThisFrame;

		public float timeBetweenEachDoorPiece;
		public float timeForEachDoorPieceToOpen;
		public float timeForEachDoorPieceToClose;

		public DoorOpenCloseSave(DoorOpenClose door) {
			this.doorOpenCurve = door.doorOpenCurve;
			this.doorCloseCurve = door.doorCloseCurve;

			this.doorPieceScales = door.doorPieces.Select<Transform, SerializableVector3>(d => d.localScale).ToArray();
			this.state = door.state;
			this.timeSinceStateChange = door.timeSinceStateChange;
			this.queueDoorClose = door.queueDoorClose;

			this.closedScale = door.closedScale;
			this.openedScale = door.openedScale;

			this.playerWasInTriggerZoneLastFrame = door.playerWasInTriggerZoneLastFrame;
			this.playerInTriggerZoneThisFrame = door.playerInTriggerZoneThisFrame;

			this.timeBetweenEachDoorPiece = door.timeBetweenEachDoorPiece;
			this.timeForEachDoorPieceToOpen = door.timeForEachDoorPieceToOpen;
			this.timeForEachDoorPieceToClose = door.timeForEachDoorPieceToClose;
		}

		public void LoadSave(DoorOpenClose door) {
			door.doorOpenCurve = this.doorOpenCurve;
			door.doorCloseCurve = this.doorCloseCurve;

			for (int i = 0; i < this.doorPieceScales.Length; i++) {
				door.doorPieces[i].localScale = this.doorPieceScales[i];
			}
			door._state = this.state;
			door.timeSinceStateChange = this.timeSinceStateChange;
			door.queueDoorClose = this.queueDoorClose;

			door.closedScale = this.closedScale;
			door.openedScale = this.openedScale;

			door.playerWasInTriggerZoneLastFrame = this.playerWasInTriggerZoneLastFrame;
			door.playerInTriggerZoneThisFrame = this.playerInTriggerZoneThisFrame;

			door.timeBetweenEachDoorPiece = this.timeBetweenEachDoorPiece;
			door.timeForEachDoorPieceToOpen = this.timeForEachDoorPieceToOpen;
			door.timeForEachDoorPieceToClose = this.timeForEachDoorPieceToClose;
		}
	}

	public object GetSaveObject() {
		return new DoorOpenCloseSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		DoorOpenCloseSave save = savedObject as DoorOpenCloseSave;

		save.LoadSave(this);
	}
	#endregion
}
