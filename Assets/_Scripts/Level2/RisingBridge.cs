using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class RisingBridge : MonoBehaviour {
	public bool DEBUG = false;
	public AnimationCurve bridgePieceRiseCurve;

	Transform[] bridgePieces;
	Vector3[] originalPositions;

	bool inBridgeRiseCoroutine = false;

	public float timeBetweenEachBridgePiece = 0.4f;
	public float timeForEachBridgePieceToRise = 2f;

	float distanceForEachPieceToRise = 10;

	// Use this for initialization
	void Start() {
		bridgePieces = Utils.GetComponentsInChildrenOnly<Transform>(transform);
		if (DEBUG) {
			originalPositions = new Vector3[bridgePieces.Length];
			for (int i = 0; i < bridgePieces.Length; i++) {
				originalPositions[i] = bridgePieces[i].position;
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (!DEBUG) return;

		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R) && !inBridgeRiseCoroutine) {
			ResetBridgePiecePositions();
		}

		else if (Input.GetKeyDown(KeyCode.R) && !inBridgeRiseCoroutine) {
			StartCoroutine(BridgeRise());
		}
	}

	void ResetBridgePiecePositions() {
		for (int i = 0; i < bridgePieces.Length; i++) {
			bridgePieces[i].position = originalPositions[i];
		}
	}

	IEnumerator BridgeRise() {
		inBridgeRiseCoroutine = true;
		
		int i = 0;
		while (i < bridgePieces.Length) {

			StartCoroutine(BridgePieceRise(bridgePieces[i]));
			i++;

			yield return new WaitForSeconds(timeBetweenEachBridgePiece);
		}

		inBridgeRiseCoroutine = false;
	}

	IEnumerator BridgePieceRise(Transform piece) {
		Vector3 startPosition = piece.position;
		Vector3 endPosition = startPosition + Vector3.up * distanceForEachPieceToRise;

		float timeElapsed = 0;
		while (timeElapsed < timeForEachBridgePieceToRise) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / timeForEachBridgePieceToRise;

			piece.position = Vector3.LerpUnclamped(startPosition, endPosition, bridgePieceRiseCurve.Evaluate(t));

			yield return null;
		}
	}
}
