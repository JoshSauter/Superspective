using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class DoorOpenClose : MonoBehaviour {
	public bool DEBUG = false;
	public AnimationCurve doorOpenCurve;
	public AnimationCurve doorCloseCurve;

	Transform[] doorPieces;
	Vector3[] originalScales;

	bool inDoorOpenCoroutine = false;
	bool doorOpen = false;
	// Has to be re-asserted every physics timestep, else will close the door
	bool playerInTriggerZoneThisFrame = false;

	public float timeBetweenEachDoorPiece = 0.4f;
	public float timeForEachDoorPieceToOpen = 2f;
	public float timeForEachDoorPieceToClose = 0.5f;

	public float targetLocalXScale = 0;

#region events
	public delegate void DoorAction(DoorOpenClose door);
	public event DoorAction OnDoorOpen;
	public event DoorAction OnDoorClose;
#endregion

	// Use this for initialization
	void Start() {
		doorPieces = transform.GetComponentsInChildrenOnly<Transform>();
		originalScales = new Vector3[doorPieces.Length];
		for (int i = 0; i < doorPieces.Length; i++) {
			originalScales[i] = doorPieces[i].localScale;
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (!DEBUG) return;

		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R) && !inDoorOpenCoroutine) {
			ResetDoorPieceScales();
		}

		else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.O) && !inDoorOpenCoroutine) {
			StartCoroutine(DoorCloseCoroutine());
		}

		else if (Input.GetKeyDown(KeyCode.O) && !inDoorOpenCoroutine) {
			StartCoroutine(DoorOpenCoroutine());
		}
	}

	private void FixedUpdate() {
		if (doorOpen && !playerInTriggerZoneThisFrame) {
			CloseDoor();
		}
		// Need to re-assert this every physics timestep, reset state
		playerInTriggerZoneThisFrame = false;
	}

	void ResetDoorPieceScales() {
		for (int i = 0; i < doorPieces.Length; i++) {
			doorPieces[i].localScale = originalScales[i];
		}
	}

	void OpenDoor() {
		if (!inDoorOpenCoroutine && !doorOpen) {
			StartCoroutine(DoorOpenCoroutine());
		}
	}

	void CloseDoor() {
		if (!inDoorOpenCoroutine && doorOpen) {
			StartCoroutine(DoorCloseCoroutine());
		}
	}

	IEnumerator DoorOpenCoroutine() {
		inDoorOpenCoroutine = true;
		
		int i = 0;
		while (i < doorPieces.Length) {

			StartCoroutine(DoorPieceOpen(doorPieces[doorPieces.Length - i - 1]));
			i++;

			yield return new WaitForSeconds(timeBetweenEachDoorPiece);
		}

		// Allow time for the last door piece to open before marking the coroutine complete
		yield return new WaitForSeconds(timeForEachDoorPieceToOpen);
		inDoorOpenCoroutine = false;
		doorOpen = true;

		if (OnDoorOpen != null) {
			OnDoorOpen(this);
		}
	}

	IEnumerator DoorPieceOpen(Transform piece) {
		Vector3 startScale = piece.localScale;
		Vector3 endScale = new Vector3(targetLocalXScale, startScale.y, startScale.z);

		float timeElapsed = 0;
		while (timeElapsed < timeForEachDoorPieceToOpen) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / timeForEachDoorPieceToOpen;

			piece.localScale = Vector3.LerpUnclamped(startScale, endScale, doorOpenCurve.Evaluate(t));

			yield return null;
		}

		piece.localScale = endScale;
	}

	IEnumerator DoorCloseCoroutine() {
		inDoorOpenCoroutine = true;

		int i = 0;
		while (i < doorPieces.Length) {

			StartCoroutine(DoorPieceClose(doorPieces.Length - i - 1));
			i++;

			yield return new WaitForSeconds(timeBetweenEachDoorPiece);
		}

		// Allow time for the last door piece to open before marking the coroutine complete
		yield return new WaitForSeconds(timeForEachDoorPieceToClose);
		inDoorOpenCoroutine = false;
		doorOpen = false;

		if (OnDoorClose != null) {
			OnDoorClose(this);
		}
	}

	IEnumerator DoorPieceClose(int pieceIndex) {
		Vector3 startScale = doorPieces[pieceIndex].localScale;
		Vector3 endScale = originalScales[pieceIndex];

		float timeElapsed = 0;
		while (timeElapsed < timeForEachDoorPieceToClose) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / timeForEachDoorPieceToClose;

			doorPieces[pieceIndex].localScale = Vector3.LerpUnclamped(startScale, endScale, doorCloseCurve.Evaluate(t));
			
			yield return null;
		}

		doorPieces[pieceIndex].localScale = endScale;
	}

	private void OnTriggerStay(Collider other) {
		if (other.TaggedAsPlayer()) {
			if (!doorOpen) {
				OpenDoor();
			}
			playerInTriggerZoneThisFrame = true;
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.TaggedAsPlayer()) {
			if (doorOpen) {
				CloseDoor();
			}
		}
	}
}
