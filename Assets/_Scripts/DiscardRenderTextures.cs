using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscardRenderTextures : MonoBehaviour {
	public static DiscardRenderTextures instance;
	public RenderTexture[] textures;
	[SerializeField]
	private Camera[] obscureShaderCameras;

	public static int currentWidth;
	public static int currentHeight;

	// Use this for initialization
	void Awake () {
		// Lazy singleton for now
		instance = this;

		currentWidth = Screen.width;
		currentHeight = Screen.height;

		// There should really be a better way to do this
		Camera[] allCameras = transform.GetComponentsInChildren<Camera>();
		obscureShaderCameras = new Camera[allCameras.Length - 1];
		for (int i = 1; i < allCameras.Length; i++) {
			obscureShaderCameras[i-1] = allCameras[i];
		}
		textures = new RenderTexture[obscureShaderCameras.Length];
	}

	private void OnEnable() {
		for (int i = 0; i < textures.Length; i++) {
			CreateRenderTexture(i);
		}
	}

	private void OnDisable() {
		foreach (var tex in textures) {
			tex.Release();
		}
	}

	// Update is called once per frame
	void Update () {
		// Update the resolution if necessary
		if (Screen.width != currentWidth || Screen.height != currentHeight) {

			currentWidth = Screen.width;
			currentHeight = Screen.height;

			for (int i = 0; i < textures.Length; i++) {
				textures[i].Release();
				CreateRenderTexture(i);
			}
		}

		for (int i = 0; i < textures.Length; i++) {
			// Will write to global textures named _DiscardTex1, _DiscardTex2, etc.
			Shader.SetGlobalTexture("_DiscardTex" + (i+1), textures[i]);
		}
	}

	void CreateRenderTexture(int index) {
		textures[index] = new RenderTexture(currentWidth, currentHeight, 24);
		textures[index].enableRandomWrite = true;
		textures[index].Create();

		Shader.SetGlobalFloat("_ResolutionX", currentWidth);
		Shader.SetGlobalFloat("_ResolutionY", currentHeight);

		obscureShaderCameras[index].targetTexture = textures[index];
	}
}
