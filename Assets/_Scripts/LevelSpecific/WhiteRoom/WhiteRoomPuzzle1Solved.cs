using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiteRoomPuzzle1Solved : MonoBehaviour {
	public CubeReceptacle receptacle;
	public GameObject fakePortal;

	Vector3 startPos;
	Vector3 endPos;
	Vector3 targetPos;
	float moveSpeed;

	float moveSpeedUp = 4;
	float moveSpeedDown = 10;

    void Start() {
		moveSpeed = moveSpeedUp;

		receptacle = GetComponent<CubeReceptacle>();
		receptacle.OnCubeHoldEndSimple += OnCubePlaced;
		receptacle.OnCubeReleaseStartSimple += OnCubeRemoved;

		startPos = fakePortal.transform.position;
		endPos = fakePortal.transform.TransformPoint(Vector3.up * 10);
		targetPos = startPos;
    }

    void Update() {
        if (fakePortal.activeSelf) {
			fakePortal.transform.position = Vector3.Lerp(fakePortal.transform.position, targetPos, Time.deltaTime * moveSpeed);

			if (Vector3.Distance(fakePortal.transform.position, startPos) < 0.1f) {
				ResetFakePortal();
			}
		}

    }

	void OnCubePlaced() {
		fakePortal.SetActive(true);
		targetPos = endPos;
		moveSpeed = moveSpeedUp;
	}

	void OnCubeRemoved() {
		targetPos = startPos;
		moveSpeed = moveSpeedDown;
	}

	void ResetFakePortal() {
		fakePortal.transform.position = startPos;
		fakePortal.SetActive(false);
	}
}
