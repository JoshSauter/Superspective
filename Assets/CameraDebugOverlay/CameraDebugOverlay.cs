using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class CameraDebugOverlay : MonoBehaviour {
	[SerializeField]
	Material mat;
	[SerializeField]
	Material stencilBufferDebugMat;

	KeyCode modeSwitchKey = KeyCode.N;

	public enum DebugMode {
		depth,
		normals,
		obliqueness,
		stencilBuffer,
		off
	}
	public DebugMode debugMode = DebugMode.off;

	private const int NUM_MODES = 5;
	int mode {
		get {
			return (int)debugMode;
		}
	}

	void Start() {
		GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
	}

	private void Update() {
		if (Input.GetKeyDown(modeSwitchKey)) {
			debugMode = (DebugMode)(((int)debugMode + 1) % NUM_MODES);
		}
	}

	[ImageEffectOpaque]
	void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (mode < NUM_MODES - 1) {
			if (debugMode == DebugMode.stencilBuffer) {
				Graphics.Blit(source, destination, stencilBufferDebugMat);
			}
			else {
				Graphics.Blit(source, destination, mat, mode);
			}
		}
		else {
			Graphics.Blit(source, destination);
		}
	}
}