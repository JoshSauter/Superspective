using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using EpitaphUtils;

namespace OldPortalMechanics {
	[System.Serializable]
	public struct TransformObjectOnRotate {
		public UnityEngine.Transform objectToTransform;
		public Vector3 displacement;
		public Vector3 scaling;
		public Vector3 rotation;
		public Vector3 rotationPivot;
	}

	public class OldPortalSettings : MonoBehaviour {
		DebugLogger debug;

		public int channel;
		public bool useMeshAsCollider = false;
		public bool useCameraEdgeDetectionColor = true;
		public TransformObjectOnRotate[] objectsToTransformOnTeleport;
		public BladeEdgeDetection.EdgeColorMode edgeColorMode;
		public Color edgeColor = Color.black;
		public Gradient edgeColorGradient;
		public Texture2D edgeColorGradientTexture;

		private void Awake() {
			debug = new DebugLogger(this, () => false);
		}

		private void OnEnable() {
			StartCoroutine(AddReceiverCoroutine());
		}

		private void OnDisable() {
			OldPortalManager.instance.RemoveReceiver(channel, this);
		}

		IEnumerator AddReceiverCoroutine() {
			while (!gameObject.scene.isLoaded) {
				debug.Log("Waiting for scene " + gameObject.scene + " to be loaded before adding receiver...");
				yield return null;
			}

			OldPortalManager.instance.AddReceiver(channel, this);
		}
	}
}
