using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeReceptacle : MonoBehaviour {
	public float receptacleSize = 1f;

	BoxCollider triggerZone;
	PickupObject cubeInReceptacle;

	float rotateTime = 0.25f;
	float translateTime = 0.5f;

	public delegate void CubeReceptacleAction(CubeReceptacle receptacle, PickupObject cube);
	public delegate void CubeReceptacleActionSimple();

	public event CubeReceptacleAction OnCubeHoldStart;
	public event CubeReceptacleAction OnCubeHoldEnd;
	public event CubeReceptacleAction OnCubeReleaseStart;
	public event CubeReceptacleAction OnCubeReleaseEnd;

	public event CubeReceptacleActionSimple OnCubeHoldStartSimple;
	public event CubeReceptacleActionSimple OnCubeHoldEndSimple;
	public event CubeReceptacleActionSimple OnCubeReleaseStartSimple;
	public event CubeReceptacleActionSimple OnCubeReleaseEndSimple;

	private static Quaternion[] acceptableRotations = new Quaternion[] {
		Quaternion.Euler(0,0,0),
		Quaternion.Euler(0,0,90),
		Quaternion.Euler(0,0,180),
		Quaternion.Euler(0,0,270),
		Quaternion.Euler(0,90,0),
		Quaternion.Euler(0,90,90),
		Quaternion.Euler(0,90,180),
		Quaternion.Euler(0,90,270),
		Quaternion.Euler(0,180,0),
		Quaternion.Euler(0,180,90),
		Quaternion.Euler(0,180,180),
		Quaternion.Euler(0,180,270),
		Quaternion.Euler(0,270,0),
		Quaternion.Euler(0,270,90),
		Quaternion.Euler(0,270,180),
		Quaternion.Euler(0,270,270),
		Quaternion.Euler(90,0,0),
		Quaternion.Euler(90,0,90),
		Quaternion.Euler(90,0,180),
		Quaternion.Euler(90,0,270),
		Quaternion.Euler(90,90,0),
		Quaternion.Euler(90,90,90),
		Quaternion.Euler(90,90,180),
		Quaternion.Euler(90,90,270),
		Quaternion.Euler(90,180,0),
		Quaternion.Euler(90,180,90),
		Quaternion.Euler(90,180,180),
		Quaternion.Euler(90,180,270),
		Quaternion.Euler(90,270,0),
		Quaternion.Euler(90,270,90),
		Quaternion.Euler(90,270,180),
		Quaternion.Euler(90,270,270),
		Quaternion.Euler(180,0,0),
		Quaternion.Euler(180,0,90),
		Quaternion.Euler(180,0,180),
		Quaternion.Euler(180,0,270),
		Quaternion.Euler(180,90,0),
		Quaternion.Euler(180,90,90),
		Quaternion.Euler(180,90,180),
		Quaternion.Euler(180,90,270),
		Quaternion.Euler(180,180,0),
		Quaternion.Euler(180,180,90),
		Quaternion.Euler(180,180,180),
		Quaternion.Euler(180,180,270),
		Quaternion.Euler(180,270,0),
		Quaternion.Euler(180,270,90),
		Quaternion.Euler(180,270,180),
		Quaternion.Euler(180,270,270),
		Quaternion.Euler(270,0,0),
		Quaternion.Euler(270,0,90),
		Quaternion.Euler(270,0,180),
		Quaternion.Euler(270,0,270),
		Quaternion.Euler(270,90,0),
		Quaternion.Euler(270,90,90),
		Quaternion.Euler(270,90,180),
		Quaternion.Euler(270,90,270),
		Quaternion.Euler(270,180,0),
		Quaternion.Euler(270,180,90),
		Quaternion.Euler(270,180,180),
		Quaternion.Euler(270,180,270),
		Quaternion.Euler(270,270,0),
		Quaternion.Euler(270,270,90),
		Quaternion.Euler(270,270,180),
		Quaternion.Euler(270,270,270),
	};

    void Start() {
		AddTriggerZone();
    }

	void AddTriggerZone() {
		//GameObject triggerZoneGO = new GameObject("TriggerZone");
		//triggerZoneGO.transform.SetParent(transform, false);
		//triggerZoneGO.layer = LayerMask.NameToLayer("Ignore Raycast");
		//triggerZone = triggerZoneGO.AddComponent<BoxCollider>();
		triggerZone = gameObject.AddComponent<BoxCollider>();
		gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

		triggerZone.size = new Vector3(receptacleSize * 0.25f, receptacleSize * 1.5f, receptacleSize * 0.25f);
		triggerZone.isTrigger = true;
	}

	private void OnTriggerEnter(Collider other) {
		PickupObject cube = other.gameObject.GetComponent<PickupObject>();
		if (cube != null && cubeInReceptacle == null) {
			Rigidbody cubeRigidbody = cube.GetComponent<Rigidbody>();
			cube.Drop();
			cubeRigidbody.isKinematic = true;
			cubeInReceptacle = cube;
			cubeInReceptacle.OnPickupSimple += OnPickupFromReceptacle;

			StartCoroutine(SnapCubeToPosition());
		}
	}

	void OnPickupFromReceptacle() {
		cubeInReceptacle.OnPickupSimple -= OnPickupFromReceptacle;
		StartCoroutine(ReleaseCubeFromReceptacle(cubeInReceptacle));
	}

	IEnumerator ReleaseCubeFromReceptacle(PickupObject cube) {
		OnCubeReleaseStart?.Invoke(this, cubeInReceptacle);
		OnCubeReleaseStartSimple?.Invoke();

		const float timeToRelease = .25f;
		float timeElapsed = 0;

		Vector3 startPos = cube.transform.position;
		Vector3 endPos = transform.TransformPoint(0, 1f, 0);
		Quaternion startRot = cube.transform.rotation;

		while (timeElapsed < timeToRelease) {
			timeElapsed += Time.fixedDeltaTime;
			float t = timeElapsed / timeToRelease;

			cube.transform.position = Vector3.Lerp(startPos, endPos, t);
			cube.transform.rotation = startRot;

			yield return new WaitForFixedUpdate();
		}

		PickupObject cubeThatWasInReceptacle = cubeInReceptacle;
		cubeInReceptacle = null;
		OnCubeReleaseEnd?.Invoke(this, cubeThatWasInReceptacle);
		OnCubeReleaseEndSimple?.Invoke();
	}

	IEnumerator SnapCubeToPosition() {
		OnCubeHoldStart?.Invoke(this, cubeInReceptacle);
		OnCubeHoldStartSimple?.Invoke();

		Quaternion startRot = cubeInReceptacle.transform.rotation;
		Quaternion endRot = ClosestRotation(startRot);

		Vector3 startRotPos = cubeInReceptacle.transform.position;
		Vector3 endRotPos = transform.TransformPoint(0, transform.InverseTransformPoint(startRotPos).y, 0);

		float timeElapsed = 0;
		while (cubeInReceptacle != null && timeElapsed < rotateTime) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / rotateTime;

			cubeInReceptacle.transform.position = Vector3.Lerp(startRotPos, endRotPos, t);
			cubeInReceptacle.transform.rotation = Quaternion.Lerp(startRot, endRot, t);

			yield return null;
		}

		Vector3 startPos = cubeInReceptacle.transform.position;
		Vector3 endPos = transform.TransformPoint(0, 0.5f, 0);

		timeElapsed = 0;
		while (cubeInReceptacle != null && timeElapsed < translateTime) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / translateTime;

			cubeInReceptacle.transform.position = Vector3.Lerp(startPos, endPos, t);

			yield return null;
		}

		OnCubeHoldEnd?.Invoke(this, cubeInReceptacle);
		OnCubeHoldEndSimple?.Invoke();
	}

	Quaternion ClosestRotation(Quaternion comparedTo) {
		float minAngle = float.MaxValue;
		Quaternion returnRotation = Quaternion.identity;

		foreach (var q in acceptableRotations) {
			float angleBetween = Quaternion.Angle(q, comparedTo);
			if (angleBetween < minAngle) {
				minAngle = angleBetween;
				returnRotation = q;
			}
		}

		return returnRotation;
	}
}
