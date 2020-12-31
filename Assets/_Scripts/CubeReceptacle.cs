using Audio;
using EpitaphUtils;
using Saving;
using SerializableClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class CubeReceptacle : MonoBehaviour, SaveableObject {
	UniqueId _id;
	UniqueId id {
		get {
			if (_id == null) {
				_id = GetComponent<UniqueId>();
			}
			return _id;
		}
	}
	public enum State {
		Empty,
		CubeEnterRotate,
		CubeEnterTranslate,
		CubeInReceptacle,
		CubeExiting
	}
	private State _state = State.Empty;
	public State state {
		get { return _state; }
		set {
			if (_state == value) {
				return;
			}
			timeSinceStateChange = 0f;
			switch (value) {
				case State.CubeEnterRotate:
					OnCubeHoldStart?.Invoke(this, cubeInReceptacle);
					OnCubeHoldStartSimple?.Invoke();
					break;
				case State.CubeInReceptacle:
					OnCubeHoldEnd?.Invoke(this, cubeInReceptacle);
					OnCubeHoldEndSimple?.Invoke();
					break;
				case State.CubeExiting:
					OnCubeReleaseStart?.Invoke(this, cubeInReceptacle);
					OnCubeReleaseStartSimple?.Invoke();
					break;
				case State.Empty:
					OnCubeReleaseEnd?.Invoke(this, cubeInReceptacle);
					OnCubeReleaseEndSimple?.Invoke();
					break;
				default:
					break;
			}
			_state = value;
		}
	}
	public float timeSinceStateChange;

	Quaternion startRot;
	Quaternion endRot;

	Vector3 startPos;
	Vector3 endPos;

	public bool makesCubeIrreplaceable = true;
	public float receptacleSize = 1f;
	public float receptableDepth = 0.5f;

	BoxCollider triggerZone;
	PickupObject cubeInReceptacle;

	const float rotateTime = 0.25f;
	const float translateTime = 0.5f;
	const float afterReleaseCooldown = 1f;
	const float timeToRelease = .25f;

	ColorCoded colorCoded;

	public SoundEffect cubeEnterSfx, cubeReleaseSfx;

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

    void Start() {
		AddTriggerZone();
		colorCoded = GetComponent<ColorCoded>();
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

	private void FixedUpdate() {
		UpdateCubeReceptacle();
	}

	void UpdateCubeReceptacle() {
		timeSinceStateChange += Time.fixedDeltaTime;
		switch (state) {
			case State.Empty:
				if (timeSinceStateChange > afterReleaseCooldown) {
					RestoreTriggerZoneAfterCooldown();
				}
				break;
			case State.CubeInReceptacle:
				if (cubeInReceptacle == null) {
					state = State.Empty;
				}
				break;
			case State.CubeEnterRotate:
				if (cubeInReceptacle == null) {
					state = State.Empty;
					break;
				}
				if (timeSinceStateChange < rotateTime) {
					float t = timeSinceStateChange / rotateTime;

					cubeInReceptacle.transform.position = Vector3.Lerp(startPos, endPos, t);
					cubeInReceptacle.transform.rotation = Quaternion.Lerp(startRot, endRot, t);
				}
				else {
					cubeInReceptacle.transform.position = endPos;
					cubeInReceptacle.transform.rotation = endRot;

					startPos = cubeInReceptacle.transform.position;
					endPos = transform.TransformPoint(0, 1-receptableDepth, 0);

					state = State.CubeEnterTranslate;
				}
				break;
			case State.CubeEnterTranslate:
				if (cubeInReceptacle == null) {
					state = State.Empty;
					break;
				}
				if (timeSinceStateChange < translateTime) {
					float t = timeSinceStateChange / translateTime;

					cubeInReceptacle.transform.position = Vector3.Lerp(startPos, endPos, t);
				}
				else {
					cubeInReceptacle.transform.position = endPos;

					cubeInReceptacle.interactable = true;
					state = State.CubeInReceptacle;
				}
				break;
			case State.CubeExiting:
				if (timeSinceStateChange < timeToRelease) {
					float t = timeSinceStateChange / timeToRelease;

					cubeInReceptacle.transform.position = Vector3.Lerp(startPos, endPos, t);
					cubeInReceptacle.transform.rotation = startRot;
				}
				else {
					PickupObject cubeThatWasInReceptacle = cubeInReceptacle;
					cubeThatWasInReceptacle.shouldFollow = true;
					cubeThatWasInReceptacle.interactable = true;
					cubeThatWasInReceptacle.isReplaceable = true;
					triggerZone.enabled = false;

					state = State.Empty;
					cubeInReceptacle = null;
				}
				break;
		}
	}

	private void OnTriggerStay(Collider other) {
		PickupObject cube = other.gameObject.GetComponent<PickupObject>();
		if (colorCoded != null && !colorCoded.AcceptedColor(other.gameObject.GetComponent<ColorCoded>())) {
			return;
		}
		if (cube != null && cubeInReceptacle == null) {
			StartCubeEnter(cube);
		}
	}

	void StartCubeEnter(PickupObject cube) {
		Rigidbody cubeRigidbody = cube.GetComponent<Rigidbody>();
		cube.Drop();
		cube.interactable = false;
		cubeRigidbody.isKinematic = true;
		cubeInReceptacle = cube;
		if (makesCubeIrreplaceable) {
			cubeInReceptacle.isReplaceable = false;
		}
		cubeInReceptacle.OnPickupSimple += ReleaseFromReceptacle;

		startRot = cubeInReceptacle.transform.rotation;
		endRot = RightAngleRotations.GetNearestRelativeToTransform(startRot, transform);

		startPos = cubeInReceptacle.transform.position;
		endPos = transform.TransformPoint(0, transform.InverseTransformPoint(startPos).y, 0);

		if (cubeEnterSfx != null) {
			cubeEnterSfx.audioSource.volume = 0.75f;
			cubeEnterSfx.PlayOneShot();
		}

		state = State.CubeEnterRotate;
	}

	void ReleaseFromReceptacle() {
		cubeInReceptacle.OnPickupSimple -= ReleaseFromReceptacle;
		ReleaseCubeFromReceptacle();
	}

	void ReleaseCubeFromReceptacle() {
		state = State.CubeExiting;

		cubeReleaseSfx?.PlayOneShot();

		cubeInReceptacle.shouldFollow = false;
		cubeInReceptacle.interactable = false;

		startPos = cubeInReceptacle.transform.position;
		endPos = transform.TransformPoint(0, 1.5f, 0);
		startRot = cubeInReceptacle.transform.rotation;
	}

	void RestoreTriggerZoneAfterCooldown() {
		triggerZone.enabled = true;
	}

	#region Saving
	public bool SkipSave { get; set; }
	// All components on PickupCubes share the same uniqueId so we need to qualify with component name
	public string ID => $"CubeReceptacle_{id.uniqueId}";

	[Serializable]
	class CubeReceptacleSave {
		State state;
		float timeSinceStateChange;

		SerializableQuaternion startRot;
		SerializableQuaternion endRot;

		SerializableVector3 startPos;
		SerializableVector3 endPos;

		SerializableReference<PickupObject> cubeInReceptacle;

		public CubeReceptacleSave(CubeReceptacle receptacle) {
			this.state = receptacle.state;
			this.timeSinceStateChange = receptacle.timeSinceStateChange;

			this.startRot = receptacle.startRot;
			this.endRot = receptacle.endRot;

			this.startPos = receptacle.startPos;
			this.endPos = receptacle.endPos;

			this.cubeInReceptacle = receptacle.cubeInReceptacle;
		}

		public void LoadSave(CubeReceptacle receptacle) {
			receptacle.state = this.state;
			receptacle.timeSinceStateChange = this.timeSinceStateChange;

			receptacle.startRot = this.startRot;
			receptacle.endRot = this.endRot;

			receptacle.startPos = this.startPos;
			receptacle.endPos = this.endPos;

			receptacle.cubeInReceptacle = this.cubeInReceptacle;
		}
	}

	public object GetSaveObject() {
		return new CubeReceptacleSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		CubeReceptacleSave save = savedObject as CubeReceptacleSave;

		save.LoadSave(this);
	}
	#endregion
}
