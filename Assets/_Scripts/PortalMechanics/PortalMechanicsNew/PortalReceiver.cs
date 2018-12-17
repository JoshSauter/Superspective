using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalReceiver : MonoBehaviour {
	public int channel;
	public bool useMeshAsCollider = false;
	public float portalFrameDepth = 1f;
	public bool useCameraEdgeDetectionColor = true;
	public Color portalEdgeDetectionColor = Color.black;

	private void OnEnable() {
		StartCoroutine(AddReceiverCoroutine());
	}

	private void OnDisable() {
		PortalManager.instance.RemoveReceiver(channel, this);
	}

	IEnumerator AddReceiverCoroutine() {
		while (!gameObject.scene.isLoaded) {
			print("Waiting for scene " + gameObject.scene + " to be loaded before adding receiver...");
			yield return null;
		}

		PortalManager.instance.AddReceiver(channel, this);
	}
}
