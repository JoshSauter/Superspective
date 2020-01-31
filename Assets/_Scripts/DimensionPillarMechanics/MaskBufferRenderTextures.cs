using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Creates and handles the visibility masks and any other render texture buffers used for rendering
public class MaskBufferRenderTextures : Singleton<MaskBufferRenderTextures> {
	public RenderTexture visibilityMaskTexture;
	public RenderTexture invertMaskTexture;

	// Use this for initialization
	void Start () {
		EpitaphScreen.instance.OnScreenResolutionChanged += HandleScreenResolutionChanged;
		CreateAllRenderTextures(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);
	}

	private void OnDisable() {
		ReleaseAllTextures();
	}

	private void HandleScreenResolutionChanged(int newWidth, int newHeight) {
		ReleaseAllTextures();
		CreateAllRenderTextures(newWidth, newHeight);
	}

	// Update is called once per frame
	void Update () {
		// Will write to global texture named _DimensionMask
		Shader.SetGlobalTexture("_DimensionMask", visibilityMaskTexture);
		// Will write to global texture named _InvertMask
		Shader.SetGlobalTexture("_InvertMask", invertMaskTexture);
	}

	void ReleaseAllTextures() {
		visibilityMaskTexture.Release();
		invertMaskTexture.Release();
	}

	void CreateAllRenderTextures(int currentWidth, int currentHeight) {
		CreateRenderTexture(currentWidth, currentHeight, out visibilityMaskTexture, EpitaphScreen.instance.dimensionCamera);
		CreateRenderTexture(currentWidth, currentHeight, out invertMaskTexture, EpitaphScreen.instance.invertMaskCamera);
	}

	void CreateRenderTexture(int currentWidth, int currentHeight, out RenderTexture rt, Camera targetCamera) {
		rt = new RenderTexture(currentWidth, currentHeight, 24);
		rt.enableRandomWrite = true;
		rt.Create();

		Shader.SetGlobalFloat("_ResolutionX", currentWidth);
		Shader.SetGlobalFloat("_ResolutionY", currentHeight);

		targetCamera.targetTexture = rt;
	}
}
