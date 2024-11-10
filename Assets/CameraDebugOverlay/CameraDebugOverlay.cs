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
		Depth,
		Normals,
		Obliqueness,
		PortalMask,
		VisibilityMask,
		Off
	}
	public DebugMode debugMode = DebugMode.Off;

	const int NUM_MODES = (int)DebugMode.Off + 1;
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
		if (DebugInput.GetKeyDown(modeSwitchKey) && !GameManager.instance.IsCurrentlyPaused) {
			debugMode = (DebugMode)(((int)debugMode + 1) % NUM_MODES);
		}
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination) {
		switch ((DebugMode)mode) {
			case DebugMode.PortalMask:
				Graphics.Blit(MaskBufferRenderTextures.instance.portalMaskTexture, destination);
				break;
			case DebugMode.VisibilityMask:
				Graphics.Blit(MaskBufferRenderTextures.instance.visibilityMaskTexture, destination);
				break;
			case DebugMode.Off:
				Graphics.Blit(source, destination);
				return;
			default:
				Graphics.Blit(source, destination, mat, mode);
				return;
		}
	}
}
