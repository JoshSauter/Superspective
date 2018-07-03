using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscardRenderTextures : MonoBehaviour {
	public static DiscardRenderTextures instance;
	public RenderTexture[] textures;
	[SerializeField]
	private Camera[] obscureShaderCameras;


	// Use this for initialization
	void Awake () {
		// Lazy singleton for now
		instance = this;

		EpitaphScreen.instance.OnScreenResolutionChanged += HandleScreenResolutionChanged;

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
			CreateRenderTexture(i, EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);
		}
	}

	private void OnDisable() {
		foreach (var tex in textures) {
			tex.Release();
		}
	}

	private void HandleScreenResolutionChanged(int newWidth, int newHeight) {
		for (int i = 0; i < textures.Length; i++) {
			textures[i].Release();
			CreateRenderTexture(i, EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);
		}
	}

	// Update is called once per frame
	void Update () {
		for (int i = 0; i < textures.Length; i++) {
			// Will write to global textures named _DiscardTex1, _DiscardTex2, etc.
			Shader.SetGlobalTexture("_DiscardTex" + (i+1), textures[i]);
		}
	}

	void CreateRenderTexture(int index, int currentWidth, int currentHeight) {
		textures[index] = new RenderTexture(currentWidth, currentHeight, 24);
		textures[index].enableRandomWrite = true;
		textures[index].Create();

		Shader.SetGlobalFloat("_ResolutionX", currentWidth);
		Shader.SetGlobalFloat("_ResolutionY", currentHeight);

		obscureShaderCameras[index].targetTexture = textures[index];
	}
}
