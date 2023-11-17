using System.Collections;
using System.Collections.Generic;
using Interactables;
using UnityEngine;
using SuperspectiveUtils;

namespace LevelSpecific.TutorialHallway {
	public class ThirdRoomTeleporterDoor : MonoBehaviour {
		public Button circleButton;
		public Button hexagonButton;
		int numButtonsLeft = 2;
		UnityEngine.Transform[] doorPieces;

		// Use this for initialization
		void Start() {
			doorPieces = transform.GetComponentsInChildrenOnly<UnityEngine.Transform>();

			circleButton.OnButtonPressFinish += HandleButtonPress;
			hexagonButton.OnButtonPressFinish += HandleButtonPress;
		}

		// Update is called once per frame
		void Update() {
			// DEBUG
			if (DebugInput.GetKey(KeyCode.LeftShift) && DebugInput.GetKeyDown("o")) {
				StartCoroutine(DoorOpen());
			}
		}

		IEnumerator DoorOpen() {
			for (int i = 0; i < doorPieces.Length; i++) {
				StartCoroutine(MoveDoorPiece(doorPieces[i]));
				yield return new WaitForSeconds(Mathf.Lerp(0.5f, 0, (float)i / doorPieces.Length));
			}
		}

		IEnumerator MoveDoorPiece(UnityEngine.Transform doorPiece) {
			float timeElapsed = 0;
			while (timeElapsed < 1) {
				timeElapsed += Time.deltaTime;
				float t = timeElapsed / 1f;
				doorPiece.localScale = Vector3.Lerp(Vector3.one, new Vector3(1, 0, 1), Mathf.Sqrt(t));

				yield return null;
			}

			doorPiece.gameObject.SetActive(false);
		}

		void HandleButtonPress(Button b) {
			numButtonsLeft--;
			if (numButtonsLeft <= 0) {
				StartCoroutine(DoorOpen());
			}
		}
	}
}