using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TeleportEnter))]
public class PortalTeleporter : MonoBehaviour {
	public PortalContainer portal;
	public TeleportEnter teleporter;
	PortalTeleporter otherPortalTeleporter;

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

		InitializeCollider();
	}

	// Update is called once per frame
	void Update () {
		teleporter.trigger.targetDirection = -portalNormal;
		
		teleporter.teleportOffset = otherPortalTeleporter.portalNormal * 1f;
	}
	
	void InitializeCollider() {
		if (portal.settings.useMeshAsCollider) {
			MeshCollider newMeshCollider = gameObject.AddComponent<MeshCollider>();
			newMeshCollider.inflateMesh = true;
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
	}
}
