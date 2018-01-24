using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscardRenderTexture : MonoBehaviour {
	public static DiscardRenderTexture instance;
	public RenderTexture texture;
	private Camera obscureShaderCamera;

	public static int currentWidth;
	public static int currentHeight;

	// Use this for initialization
	void Awake () {
		// Lazy singleton for now
		instance = this;

		currentWidth = Screen.width;
		currentHeight = Screen.height;
		obscureShaderCamera = GetComponent<Camera>();

	}

	private void OnEnable() {
		if (texture == null || !texture.IsCreated()) {
			CreateRenderTexture();
		}
	}

	private void OnDisable() {
		texture.Release();
	}

	// Update is called once per frame
	void Update () {
		// Update the resolution if necessary
		if (Screen.width != currentWidth || Screen.height != currentHeight) {
			texture.Release();

			currentWidth = Screen.width;
			currentHeight = Screen.height;
			CreateRenderTexture();
		}
		Shader.SetGlobalTexture("_DiscardTex", texture);
	}

	void CreateRenderTexture() {
		texture = new RenderTexture(currentWidth, currentHeight, 24);
		texture.enableRandomWrite = true;
		texture.Create();

		Shader.SetGlobalFloat("_ResolutionX", currentWidth);
		Shader.SetGlobalFloat("_ResolutionY", currentHeight);

		obscureShaderCamera.targetTexture = texture;
		Shader.SetGlobalTexture("_DiscardTex", texture);
	}
}
