using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// NOT FINISHED -- Borked Mesh Generation occasionally, need a way to mask trigger collisions
[RequireComponent(typeof(Rigidbody))]
public class PortalCameraNoRenderZone : MonoBehaviour {
	MeshFilter debugFilter;
	MeshRenderer debug;

	public PortalCameraTexture portalCam;
	MeshFilter portalMeshFilter;

	Mesh zoneMesh;
	MeshCollider zoneCollider;
	Vector3[] vertices = new Vector3[4];
	int[] tris = new int[12];

	int invisLayer;

	// Use this for initialization
	void Start () {
		zoneCollider = gameObject.AddComponent<MeshCollider>();
		zoneCollider.convex = true;
		zoneCollider.isTrigger = true;
		invisLayer = LayerMask.NameToLayer("HideFromPortal");
		gameObject.layer = invisLayer;
		zoneMesh = new Mesh();
		zoneCollider.sharedMesh = zoneMesh;
		portalMeshFilter = portalCam.portal.portals[portalCam.otherIndex].GetComponent<MeshFilter>();
		debug = gameObject.AddComponent<MeshRenderer>(); debugFilter = gameObject.AddComponent<MeshFilter>();
	}
	
	// Update is called once per frame
	void Update () {
		transform.position = portalCam.transform.position;
		vertices[0] = Vector3.zero;
		
		Vector3[] portalVertices = portalMeshFilter.mesh.vertices;

		for (int i = 0; i < 3; i++) {
			Vector3 vertex = portalVertices[i];
			Vector3 portalCamPosition = portalCam.transform.position;
			Vector3 portalPosition = portalMeshFilter.transform.position;
			vertices[i+1] = (portalPosition + vertex) - portalCamPosition;
		}

		tris[0] = 0;
		tris[1] = 2;
		tris[2] = 1;
		tris[3] = 0;
		tris[4] = 3;
		tris[5] = 2;
		tris[6] = 0;
		tris[7] = 1;
		tris[8] = 3;
		tris[9] = 1;
		tris[10] = 2;
		tris[11] = 3;

		zoneMesh.vertices = vertices;
		zoneMesh.triangles = tris;
		zoneCollider.sharedMesh = zoneMesh;
		debugFilter.mesh = zoneMesh;
	}

	private Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
	private void OnTriggerEnter(Collider other) {
		if (!originalLayers.ContainsKey(other.gameObject) && other.gameObject.tag == "Pillar") {
			originalLayers.Add(other.gameObject, other.gameObject.layer);
			other.gameObject.layer = invisLayer;
		}
	}
	private void OnTriggerExit(Collider other) {
		if (originalLayers.ContainsKey(other.gameObject)) {
			other.gameObject.layer = originalLayers[other.gameObject];
			originalLayers.Remove(other.gameObject);
		}
	}
}
