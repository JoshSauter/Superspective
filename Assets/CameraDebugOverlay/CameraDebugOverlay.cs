using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class CameraDebugOverlay : MonoBehaviour {
	[SerializeField]
	Material mat;
	//[SerializeField]
	//Shader stencilBufferDebugShader;
	//Material stencilBufferDebugMat;

	KeyCode modeSwitchKey = KeyCode.N;

	public enum DebugMode {
		depth,
		normals,
		obliqueness,
		//stencilBuffer,
		off
	}
	public DebugMode debugMode = DebugMode.off;

	const int NUM_MODES = (int)DebugMode.off + 1;
	int mode {
		get {
			return (int)debugMode;
		}
	}

	void Awake() {
		//if (stencilBufferDebugMat == null) {
		//	stencilBufferDebugMat = new Material(stencilBufferDebugShader);
		//}
	}

	void Start() {
		GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
	}

	void Update() {
		if (DebugInput.GetKeyDown(modeSwitchKey)) {
			debugMode = (DebugMode)(((int)debugMode + 1) % NUM_MODES);
		}
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (mode < NUM_MODES - 1) {
			//if (debugMode == DebugMode.stencilBuffer) {
			//	Graphics.Blit(source, destination, stencilBufferDebugMat);
			//}
			//else {
				Graphics.Blit(source, destination, mat, mode);
			//}
		}
		else {
			Graphics.Blit(source, destination);
		}
	}
}