using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObscureWallsRenderOnColumnRender : MonoBehaviour {
	public GameObject columnPartial;
	public GameObject columnSolid;

	private MeshRenderer meshRenderer;

	private void Awake() {
		meshRenderer = GetComponent<MeshRenderer>();
	}
	
	void Update() {
		// Only render if the column this is attached to is either solid or partially on
		meshRenderer.enabled = (columnPartial.activeSelf || columnSolid.activeSelf);
	}

	private void OnDisable() {
		// Reset state when turned off
		meshRenderer.enabled = true;
	}
}
