using Saving;
using SerializableClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class ObjectHover : MonoBehaviour, SaveableObject {
	UniqueId _id;
	UniqueId id {
		get {
			if (_id == null) {
				_id = GetComponent<UniqueId>();
			}
			return _id;
		}
	}
	public bool useLocalCoordinates = true;
	public float maxDisplacementUp = 0.125f;
	public float maxDisplacementForward = 0f;
	public float maxDisplacementRight = 0;
	public float period = 1f;
	Vector3 up;
	Vector3 forward;
	Vector3 right;

	Vector3 displacementCounter = Vector3.zero;
	public bool hoveringPaused = false;
	const float hoveringPauseLerp = 0.1f;

	float timeElapsed = 0;

	// Use this for initialization
	void Awake() {
		up = useLocalCoordinates ? transform.up : Vector3.up;
		forward = useLocalCoordinates ? transform.forward : Vector3.forward;
		right = useLocalCoordinates ? transform.right : Vector3.right;
	}

	// Update is called once per frame
	void FixedUpdate() {
		if (!hoveringPaused) {
			timeElapsed += Time.fixedDeltaTime;
			float t = Time.fixedDeltaTime * Mathf.Cos(Mathf.PI * 2 * timeElapsed / period);
			Vector3 displacementUp = maxDisplacementUp * t * up;
			Vector3 displacementForward = maxDisplacementForward * t * forward;
			Vector3 displacementRight = maxDisplacementRight * t * right;
			Vector3 displacementVector = displacementUp + displacementForward + displacementRight;
			displacementCounter += displacementVector;
			transform.position += displacementVector;
		}
		else {
			Vector3 thisFrameMovement = -hoveringPauseLerp * displacementCounter;
			transform.position += thisFrameMovement;
			displacementCounter += thisFrameMovement;
		}
	}

	#region Saving
	public bool SkipSave { get; set; }
	public string ID => $"ObjectHover_{id.uniqueId}";

	[Serializable]
	class ObjectHoverSave {
		bool useLocalCoordinates;
		float maxDisplacementUp;
		float maxDisplacementForward;
		float maxDisplacementRight;
		float period;
		SerializableVector3 up;
		SerializableVector3 forward;
		SerializableVector3 right;

		SerializableVector3 position;
		SerializableVector3 displacementCounter;
		bool hoveringPaused;

		float timeElapsed;

		public ObjectHoverSave(ObjectHover script) {
			this.useLocalCoordinates = script.useLocalCoordinates;
			this.maxDisplacementUp = script.maxDisplacementUp;
			this.maxDisplacementForward = script.maxDisplacementForward;
			this.maxDisplacementRight = script.maxDisplacementRight;
			this.period = script.period;
			this.up = script.up;
			this.forward = script.forward;
			this.right = script.right;
			this.position = script.transform.position;
			this.displacementCounter = script.displacementCounter;
			this.hoveringPaused = script.hoveringPaused;
			this.timeElapsed = script.timeElapsed;
		}

		public void LoadSave(ObjectHover script) {
			script.useLocalCoordinates = this.useLocalCoordinates;
			script.maxDisplacementUp = this.maxDisplacementUp;
			script.maxDisplacementForward = this.maxDisplacementForward;
			script.maxDisplacementRight = this.maxDisplacementRight;
			script.period = this.period;
			script.up = this.up;
			script.forward = this.forward;
			script.right = this.right;
			script.transform.position = this.position;
			script.displacementCounter = this.displacementCounter;
			script.hoveringPaused = this.hoveringPaused;
			script.timeElapsed = this.timeElapsed;
		}
	}

	public object GetSaveObject() {
		return new ObjectHoverSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		ObjectHoverSave save = savedObject as ObjectHoverSave;

		save.LoadSave(this);
	}
	#endregion
}
