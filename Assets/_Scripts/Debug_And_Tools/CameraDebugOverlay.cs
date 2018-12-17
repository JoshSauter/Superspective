using UnityEngine;
using System.Collections;

public class CameraDebugOverlay : MonoBehaviour {
	Material mat;

	KeyCode modeSwitchKey = KeyCode.N;

	private const int NUM_MODES = 4;
	private int _mode = NUM_MODES-1;
	int mode {
		get {
			return _mode;
		}
		set {
			if (value > NUM_MODES - 1) _mode = 0;
			else _mode = value;
		}
	}

	void Start() {
		GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
		mat = Resources.Load<Material>("Materials/DepthNormalOverlay");
	}

	private void Update() {
		if (Input.GetKeyDown(modeSwitchKey)) {
			mode++;
		}
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (mode < NUM_MODES - 1) {
			Graphics.Blit(source, destination, mat, mode);
		}
		else {
			Graphics.Blit(source, destination);
		}
	}
}