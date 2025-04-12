using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using GrowShrink;
using UnityEngine;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine.Serialization;

public class Player : SingletonSuperspectiveObject<Player, Player.PlayerSave> {
	public PlayerLook look;
	public PlayerMovement movement;
	public Headbob headbob;
	public Camera PlayerCam => SuperspectiveScreen.instance.playerCamera;
	// Returns the position of the camera adjusted to be aligned with the player's body
	public Vector3 AdjustedCamPos => PlayerCam.transform.position.GetClosestPointOnFiniteLine(movement.BottomOfPlayer, movement.TopOfPlayer);
	public CameraFollow cameraFollow;
	public PickupObject heldObject;

	public float Scale => growShrink != null ? growShrink.CurrentScale : 1f;
	public GrowShrinkObject growShrink;
	public bool IsHoldingSomething => heldObject != null;

	public CapsuleCollider CapsuleCollider => movement.thisCollider;
	public new Collider collider;
	public new Renderer renderer;

	private const int MAX_LAYERS = 32;
	public int interactsWithPlayerLayerMask;

	public Vector3 lastFramePosition;

	PickupObject.PickupObjectAction onPickup => (objPickedUp) => heldObject = objPickedUp;
	PickupObject.PickupObjectAction onDrop => (objDropped) => {
		if (objDropped == heldObject) heldObject = null;
	};

	protected override void OnEnable() {
		base.OnEnable();
		renderer = GetComponentInChildren<Renderer>();
		collider = GetComponentInChildren<Collider>();
		look = GetComponent<PlayerLook>();
		movement = GetComponent<PlayerMovement>();
		headbob = GetComponent<Headbob>();
		cameraFollow = GetComponentInChildren<CameraFollow>();
		growShrink = GetComponent<GrowShrinkObject>();
		
		PickupObject.OnAnyPickup += onPickup;
		PickupObject.OnAnyDrop += onDrop;
		
		int playerLayer = SuperspectivePhysics.PlayerLayer;
		for (int layer = 0; layer < MAX_LAYERS; layer++) {
			if (layer == SuperspectivePhysics.IgnoreRaycastLayer) continue;
			
			// Check if the layer can interact with the "Player" layer
			if (!Physics.GetIgnoreLayerCollision(layer, playerLayer)) {
				// If they can interact, add the layer to the interactableLayers mask
				interactsWithPlayerLayerMask |= (1 << layer);
			}
		}
	}

	private void FixedUpdate() {
		lastFramePosition = transform.position;
	}

	protected override void OnDisable() {
		base.OnDisable();
		PickupObject.OnAnyPickup -= onPickup;
		PickupObject.OnAnyDrop -= onDrop;
	}

#region Saving

	public override void LoadSave(PlayerSave save) {
		transform.position = save.position;
		transform.rotation = save.rotation;
		transform.localScale = save.localScale;
			
		Physics.gravity = save.gravity;
	}
	
	[Serializable]
	public class PlayerSave : SaveObject<Player> {
		public SerializableVector3 position;
		public SerializableQuaternion rotation;
		public SerializableVector3 localScale;
		
		// I'm also saving Physics.gravity here because it's basically the Player's GravityObject
		public SerializableVector3 gravity;

		public PlayerSave(Player player) : base(player) {
			this.position = player.transform.position;
			this.rotation = player.transform.rotation;
			this.localScale = player.transform.localScale;

			gravity = Physics.gravity;
		}
	}
#endregion
}
