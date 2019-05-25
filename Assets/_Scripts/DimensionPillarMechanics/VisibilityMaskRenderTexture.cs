using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibilityMaskRenderTexture : Singleton<VisibilityMaskRenderTexture> {
	public RenderTexture maskTexture;

	// Use this for initialization
	void Start () {
		EpitaphScreen.instance.OnScreenResolutionChanged += HandleScreenResolutionChanged;
		CreateRenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);
	}

	private void OnDisable() {
		maskTexture.Release();
	}

	private void HandleScreenResolutionChanged(int newWidth, int newHeight) {
		maskTexture.Release();
		CreateRenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);
	}

	// Update is called once per frame
	void Update () {
		// Will write to global texture named _DimensionMask
		Shader.SetGlobalTexture("_DimensionMask", maskTexture);
	}

	void CreateRenderTexture(int currentWidth, int currentHeight) {
		maskTexture = new RenderTexture(currentWidth, currentHeight, 24);
		maskTexture.enableRandomWrite = true;
		maskTexture.Create();

		Shader.SetGlobalFloat("_ResolutionX", currentWidth);
		Shader.SetGlobalFloat("_ResolutionY", currentHeight);

		EpitaphScreen.instance.dimensionCamera.targetTexture = maskTexture;
	}
}
