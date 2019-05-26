using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TeleportEnter))]
public class PortalTeleporter : MonoBehaviour {
	public PortalContainer portal;
	public TeleportEnter teleporter;
	PortalTeleporter otherPortalTeleporter;

	public TransformObjectOnRotate[] transformationsBeforeTeleport;

	private MeshFilter portalMeshFilter;
	public Vector3 portalNormal {
		get { return transform.TransformVector(portalMeshFilter.mesh.normals[0]); }
	}

	// Use this for initialization
	void Awake () {
		portalMeshFilter = GetComponentInParent<MeshFilter>();
		teleporter = GetComponent<TeleportEnter>();
	}

	private void Start() {
		otherPortalTeleporter = portal.otherPortal.teleporter;

		teleporter.trigger.triggerCondition = MagicTrigger.TriggerConditionType.PlayerMovingDirection;
		teleporter.trigger.playerFaceThreshold = 0.01f;
		if (transformationsBeforeTeleport != null && transformationsBeforeTeleport.Length > 0) {
			teleporter.OnTeleport += TransformObjectsOnTeleport;
		}

		InitializeCollider();
	}

	// Update is called once per frame
	void Update () {
		teleporter.trigger.targetDirection = -portalNormal;
		portal.volumetricPortalTrigger.targetDirection = teleporter.trigger.targetDirection;
		
		teleporter.teleportOffset = otherPortalTeleporter.portalNormal * (2*transform.localPosition.magnitude);
	}
	
	void InitializeCollider() {
		if (portal.settings.useMeshAsCollider) {
			MeshCollider newMeshCollider = gameObject.AddComponent<MeshCollider>();
			//newMeshCollider.inflateMesh = true;
			Debug.Assert(portalMeshFilter != null, "Trying to use mesh as collider without a mesh on the portal: " + portal.gameObject.name);
			newMeshCollider.sharedMesh = portalMeshFilter.mesh;
			newMeshCollider.convex = true;
			newMeshCollider.isTrigger = true;
			teleporter.teleportEnter = newMeshCollider;
			otherPortalTeleporter.teleporter.teleportExit = newMeshCollider;
		}
		else {
			BoxCollider newBoxCollider = gameObject.AddComponent<BoxCollider>();
			Mesh portalMesh = portalMeshFilter.mesh;
			newBoxCollider.size = portalMesh.bounds.size;
			newBoxCollider.isTrigger = true;
			newBoxCollider.center = portalMesh.bounds.center;
			teleporter.teleportEnter = newBoxCollider;
			otherPortalTeleporter.teleporter.teleportExit = newBoxCollider;
		}

		transform.parent.GetComponent<Collider>().enabled = false;
		teleporter.trigger.targetDirection = -portalNormal;
		transform.position += teleporter.trigger.targetDirection;
	}
	
	void TransformObjectsOnTeleport(Collider unused1, Collider unused2, Collider player) {
		Transform originalPlayerParent = player.transform.parent;

		foreach (var transformation in transformationsBeforeTeleport) {
			player.transform.parent = transformation.objectToTransform;
			DirectionalLightSingleton.instance.transform.parent = transformation.objectToTransform;

			Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
			Vector3 playerWorldVelocity = playerRigidbody.velocity;
			Vector3 playerRelativeVelocity = player.transform.InverseTransformDirection(playerWorldVelocity);

			transformation.objectToTransform.position += transformation.displacement;
			transformation.objectToTransform.localScale = Vector3.Scale(transformation.objectToTransform.localScale, transformation.scaling);
			transformation.objectToTransform.RotateAround(transformation.rotationPivot, Vector3.right, transformation.rotation.x);
			transformation.objectToTransform.RotateAround(transformation.rotationPivot, Vector3.up, transformation.rotation.y);
			transformation.objectToTransform.RotateAround(transformation.rotationPivot, Vector3.forward, transformation.rotation.z);

			playerRigidbody.velocity = player.transform.TransformDirection(playerRelativeVelocity);
		}

		teleporter.teleportOffset = otherPortalTeleporter.portalNormal * (2 * transform.localPosition.magnitude);

		// Restore the player to the ManagerScene
		player.transform.parent = LevelManager.instance.transform;
		player.transform.parent = originalPlayerParent;
		DirectionalLightSingleton.instance.transform.parent = LevelManager.instance.transform;
		DirectionalLightSingleton.instance.transform.parent = null;
	}
}
