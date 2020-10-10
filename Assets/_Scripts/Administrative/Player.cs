using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SerializableClasses;

public class Player : Singleton<Player>, SaveableObject {
	public PlayerLook look;
	public PlayerMovement movement;
	public Headbob headbob;
	public CameraFollow cameraFollow;
	public PickupObject heldObject;
	public bool isHoldingSomething { get { return heldObject != null; } }

	public Collider collider;
	public Renderer renderer;
	public Vector3 playerSize { get { return renderer.bounds.size; } }

	private void Awake() {
		renderer = GetComponentInChildren<Renderer>();
		collider = GetComponentInChildren<Collider>();
		look = GetComponent<PlayerLook>();
		movement = GetComponent<PlayerMovement>();
		headbob = GetComponent<Headbob>();
		cameraFollow = GetComponentInChildren<CameraFollow>();
	}

	private void Start() {
		PickupObject.OnAnyPickup += (PickupObject objPickedUp) => heldObject = objPickedUp;
		PickupObject.OnAnyDrop += (PickupObject objDropped) => { if (objDropped == heldObject) heldObject = null; };
	}

	#region Saving
	// There's only one player so we don't need a UniqueId here
	public string ID => "Player";

	[Serializable]
	class PlayerSave {
		SerializableVector3 position;
		SerializableQuaternion rotation;
		SerializableVector3 localScale;

		public PlayerSave(Player player) {
			this.position = player.transform.position;
			this.rotation = player.transform.rotation;
			this.localScale = player.transform.localScale;
		}

		public void LoadSave(Player player) {
			player.transform.position = this.position;
			player.transform.rotation = this.rotation;
			player.transform.localScale = this.localScale;
		}
	}

	public object GetSaveObject() {
		return new PlayerSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		PlayerSave save = savedObject as PlayerSave;

		save.LoadSave(this);
	}
	#endregion
}
