using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class DoorOpen : MonoBehaviour {
	public bool DEBUG = false;
	public AnimationCurve doorOpenCurve;

	Transform[] doorPieces;
	Vector3[] originalScales;

	bool inDoorOpenCoroutine = false;

	public float timeBetweenEachDoorPiece = 0.4f;
	public float timeForEachDoorPieceToOpen = 2f;

	public float targetLocalXScale = 0;

	// Use this for initialization
	void Start() {
		doorPieces = Utils.GetComponentsInChildrenOnly<Transform>(transform);
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

	void ResetDoorPieceScales() {
		for (int i = 0; i < doorPieces.Length; i++) {
			doorPieces[i].localScale = originalScales[i];
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
		yield return new WaitForSeconds(timeForEachDoorPieceToOpen);
		inDoorOpenCoroutine = false;
	}

	IEnumerator DoorPieceClose(int pieceIndex) {
		Vector3 startScale = doorPieces[pieceIndex].localScale;
		Vector3 endScale = originalScales[pieceIndex];

		float timeElapsed = 0;
		while (timeElapsed < timeForEachDoorPieceToOpen) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / timeForEachDoorPieceToOpen;

			doorPieces[pieceIndex].localScale = Vector3.LerpUnclamped(endScale, startScale, doorOpenCurve.Evaluate(1-t));
			
			yield return null;
		}

		doorPieces[pieceIndex].localScale = endScale;
	}
}
