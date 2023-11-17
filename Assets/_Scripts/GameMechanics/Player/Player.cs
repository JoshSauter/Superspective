using Saving;
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

	public float scale => growShrink != null ? growShrink.currentScale : 1f;
	public GrowShrinkObject growShrink;
	public bool isHoldingSomething => heldObject != null;

	public new Collider collider;
	public new Renderer renderer;

	private const int MAX_LAYERS = 32;
	public int interactsWithPlayerLayerMask;

	PickupObject.PickupObjectAction onPickup => (objPickedUp) => heldObject = objPickedUp;
	PickupObject.PickupObjectAction onDrop => (objDropped) => {
		if (objDropped == heldObject) heldObject = null;
	};

	protected void OnEnable() {
		renderer = GetComponentInChildren<Renderer>();
		collider = GetComponentInChildren<Collider>();
		look = GetComponent<PlayerLook>();
		movement = GetComponent<PlayerMovement>();
		headbob = GetComponent<Headbob>();
		cameraFollow = GetComponentInChildren<CameraFollow>();
		growShrink = GetComponent<GrowShrinkObject>();
		
		PickupObject.OnAnyPickup += onPickup;
		PickupObject.OnAnyDrop += onDrop;
		
		int playerLayer = LayerMask.NameToLayer("Player");
		for (int layer = 0; layer < MAX_LAYERS; layer++) {
			if (LayerMask.LayerToName(layer) == "Ignore Raycast") continue;
			
			// Check if the layer can interact with the "Player" layer
			if (!Physics.GetIgnoreLayerCollision(layer, playerLayer)) {
				// If they can interact, add the layer to the interactableLayers mask
				interactsWithPlayerLayerMask |= (1 << layer);
			}
		}
	}

	protected void OnDisable() {
		PickupObject.OnAnyPickup -= onPickup;
		PickupObject.OnAnyDrop -= onDrop;
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
