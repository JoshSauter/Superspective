using Audio;
using EpitaphUtils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tayx.Graphy.Utils.NumString;
using UnityEngine;

public class ForkElevator : MonoBehaviour {
	public AnimationCurve lockBarAnimation;
	public Transform[] lockBars;
	public Transform elevator;
	public Transform lockBeam;
	public GameObject invisibleElevatorWall;
	public Button elevatorButton;
	float height = 21.5f;
	float raisedHeight;
	float loweredHeight;

	float timeToLockDoors = 2f;
	float timeToUnlockDoors = .75f;
	float lockBeamMinSize = 0.125f;
	float maxSpeed = 6f;

	bool inCoroutine = false;
	bool playerStandingInElevator = false;

	public SoundEffect openSfx, closeSfx, movingSfx;

    void Start() {
		raisedHeight = transform.parent.position.y;
		loweredHeight = raisedHeight - height;

		elevatorButton.OnButtonPressBegin += (ctx) => RaiseLowerElevator(true);
		elevatorButton.OnButtonDepressBegin += (ctx) => RaiseLowerElevator(false);

		foreach (var lockBar in lockBars) {
			lockBar.localScale = new Vector3(1, 0, 1);
		}
    }

	private void Update() {
		elevatorButton.interactableObject.interactable = playerStandingInElevator && !inCoroutine;
	}

	private void OnTriggerEnter(Collider other) {
		playerStandingInElevator = true;
	}

	private void OnTriggerExit(Collider other) {
		playerStandingInElevator = false;
	}

	void RaiseLowerElevator(bool goingDown) {
		if (playerStandingInElevator && !inCoroutine) {
			StartCoroutine(RaiseLowerElevatorCoroutine(goingDown));
		}
	}

	IEnumerator RaiseLowerElevatorCoroutine(bool goingDown) {
		inCoroutine = true;
		elevatorButton.interactableObject.interactable = false;

		float timeElapsed = 0f;
		float lockBarDelayTime = 0.25f;
		invisibleElevatorWall.SetActive(true);
		closeSfx.Play(true);
		CameraShake.instance.Shake(timeToLockDoors, 0.25f, 0f);

		while (timeElapsed < timeToLockDoors + (lockBars.Length/2) * lockBarDelayTime) {
			timeElapsed += Time.fixedDeltaTime;
			float t = timeElapsed / timeToLockDoors;

			for (int i = 0; i < lockBars.Length; i++) {
				float thisBarTime = Mathf.Clamp01((timeElapsed - lockBarDelayTime * (i/2)) / timeToLockDoors);
				Vector3 curScale = lockBars[i].localScale;
				curScale.y = lockBarAnimation.Evaluate(thisBarTime);
				lockBars[i].localScale = curScale;
			}
			lockBeam.localScale = new Vector3(lockBeam.localScale.x, lockBeam.localScale.y, Mathf.Lerp(1f, lockBeamMinSize, t));

			yield return new WaitForFixedUpdate();
		}
		invisibleElevatorWall.SetActive(false);

		yield return new WaitForSeconds(0.1f);

		CameraShake.instance.Shake(5f, 0.0625f, 0.0625f);
		movingSfx.Play(true);
		float curSpeed = 0f;
		while (goingDown ? (elevator.position.y - 0.2f > loweredHeight) : (elevator.position.y + 0.2f < raisedHeight)) {
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

			yield return new WaitForFixedUpdate();
		}
		CameraShake.instance.CancelShake();

		elevator.position = new Vector3(elevator.position.x, (goingDown ? loweredHeight : raisedHeight), elevator.position.z);

		yield return new WaitForSeconds(0.1f);

		openSfx.Play(true);
		CameraShake.instance.Shake(timeToUnlockDoors, 0.25f, 0f);

		float unlockBarDelayTime = 0.125f;
		timeElapsed = 0f;
		while (timeElapsed < timeToUnlockDoors + lockBars.Length * unlockBarDelayTime) {
			timeElapsed += Time.fixedDeltaTime;
			float t = timeElapsed / timeToUnlockDoors;

			for (int i = 0; i < lockBars.Length; i++) {
				float thisBarTime = 1 - Mathf.Clamp01((timeElapsed - unlockBarDelayTime * (i/2)) / timeToUnlockDoors);
				Vector3 curScale = lockBars[i].localScale;
				curScale.y = lockBarAnimation.Evaluate(thisBarTime);
				lockBars[i].localScale = curScale;
			}
			lockBeam.localScale = new Vector3(lockBeam.localScale.x, lockBeam.localScale.y, Mathf.Lerp(1f, lockBeamMinSize, 1-t));

			yield return new WaitForFixedUpdate();
		}

		elevatorButton.interactableObject.interactable = true;
		inCoroutine = false;
	}
}
