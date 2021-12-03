﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;

namespace LevelSpecific.Level3 {
	public class RisingBridge : MonoBehaviour {
		public LaserReceiver redLaserReceiver;
		public LaserReceiver purpleLaserReceiver;
		public bool DEBUG = false;
		public AnimationCurve bridgePieceRiseCurve;

		UnityEngine.Transform[] bridgePieces;
		Vector3[] originalPositions;

		bool inBridgeRiseCoroutine = false;

		public float timeBetweenEachBridgePiece = 0.4f;
		public float timeForEachBridgePieceToRise = 2f;

		float distanceForEachPieceToRise = 10;

		// Use this for initialization
		void Start() {
			bridgePieces = transform.GetComponentsInChildrenOnly<UnityEngine.Transform>();
			if (DEBUG) {
				originalPositions = new Vector3[bridgePieces.Length];
				for (int i = 0; i < bridgePieces.Length; i++) {
					originalPositions[i] = bridgePieces[i].position;
				}
			}

			redLaserReceiver.OnReceiverActivated += StartBridgeRiseIfBothReceiversActivated;
			purpleLaserReceiver.OnReceiverActivated += StartBridgeRiseIfBothReceiversActivated;
		}

		void StartBridgeRiseIfBothReceiversActivated() {
			if (!inBridgeRiseCoroutine && LaserReceiver.numReceiversActivated == 2) {
				StartCoroutine(BridgeRise());
			}
		}

		// Update is called once per frame
		void Update() {
			if (!DEBUG) return;

			if (DebugInput.GetKey(KeyCode.LeftShift) && DebugInput.GetKeyDown(KeyCode.R) && !inBridgeRiseCoroutine) {
				ResetBridgePiecePositions();
			}

			else if (DebugInput.GetKeyDown(KeyCode.R) && !inBridgeRiseCoroutine) {
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

			// Allow time for the last bridge piece to rise before marking the coroutine complete
			yield return new WaitForSeconds(timeForEachBridgePieceToRise);
			inBridgeRiseCoroutine = false;
		}

		IEnumerator BridgePieceRise(UnityEngine.Transform piece) {
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
}