using Saving;
using SerializableClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class ObjectScale : MonoBehaviour, SaveableObject {
	UniqueId _id;
	UniqueId id {
		get {
			if (_id == null) {
				_id = GetComponent<UniqueId>();
			}
			return _id;
		}
	}
	public float minSize = 1f;
	public float maxSize = 1f;
	public float period = 3f;

	private Vector3 startScale;

	private float timeElapsed = 0;
	
	// Update is called once per frame
	void Update () {
		timeElapsed += Time.deltaTime;
		transform.localScale = startScale.normalized * startScale.magnitude * Mathf.Lerp(minSize, maxSize, Mathf.Cos(timeElapsed * 2 * Mathf.PI / period) * 0.5f + 0.5f);
	}

	private void OnEnable() {
		startScale = transform.localScale;
	}

	private void OnDisable() {
		transform.localScale = startScale;
		timeElapsed = 0;
	}

	#region Saving
	public bool SkipSave { get; set; }
	// All components on PickupCubes share the same uniqueId so we need to qualify with component name
	public string ID => $"ObjectScale_{id.uniqueId}";

	[Serializable]
	class ObjectScaleSave {
		float minSize;
		float maxSize;
		float period;

		SerializableVector3 startScale;

		float timeElapsed = 0;
		SerializableVector3 scale;

		public ObjectScaleSave(ObjectScale script) {
			this.minSize = script.minSize;
			this.maxSize = script.maxSize;
			this.period = script.period;
			this.startScale = script.startScale;
			this.timeElapsed = script.timeElapsed;
			this.scale = script.transform.localScale;
		}

		public void LoadSave(ObjectScale script) {
			script.minSize = this.minSize;
			script.maxSize = this.maxSize;
			script.period = this.period;
			script.startScale = this.startScale;
			script.timeElapsed = this.timeElapsed;
			script.transform.localScale = this.scale;
		}
	}

	public object GetSaveObject() {
		return new ObjectScaleSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		ObjectScaleSave save = savedObject as ObjectScaleSave;

		save.LoadSave(this);
	}
	#endregion
}
