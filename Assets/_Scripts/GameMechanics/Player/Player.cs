﻿using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using GrowShrink;
using UnityEngine;
using SerializableClasses;

public class Player : SingletonSaveableObject<Player, Player.PlayerSave> {
	public PlayerLook look;
	public PlayerMovement movement;
	public Headbob headbob;
	public Camera playerCam => SuperspectiveScreen.instance.playerCamera;
	public CameraFollow cameraFollow;
	public PickupObject heldObject;
	public GrowShrinkObject growShrink;
	public bool isHoldingSomething => heldObject != null;

	public new Collider collider;
	public new Renderer renderer;

	protected override void Awake() {
		base.Awake();
		renderer = GetComponentInChildren<Renderer>();
		collider = GetComponentInChildren<Collider>();
		look = GetComponent<PlayerLook>();
		movement = GetComponent<PlayerMovement>();
		headbob = GetComponent<Headbob>();
		cameraFollow = GetComponentInChildren<CameraFollow>();
		growShrink = GetComponent<GrowShrinkObject>();
	}

	protected override void Start() {
		base.Start();
		PickupObject.OnAnyPickup += (PickupObject objPickedUp) => heldObject = objPickedUp;
		PickupObject.OnAnyDrop += (PickupObject objDropped) => { if (objDropped == heldObject) heldObject = null; };
	}

	#region Saving
	// There's only one player so we don't need a UniqueId here
	public override string ID => "Player";

	[Serializable]
	public class PlayerSave : SerializableSaveObject<Player> {
		SerializableVector3 position;
		SerializableQuaternion rotation;
		SerializableVector3 localScale;

		public PlayerSave(Player player) : base(player) {
			this.position = player.transform.position;
			this.rotation = player.transform.rotation;
			this.localScale = player.transform.localScale;
		}

		public override void LoadSave(Player player) {
			player.transform.position = this.position;
			player.transform.rotation = this.rotation;
			player.transform.localScale = this.localScale;
		}
	}
	#endregion
}