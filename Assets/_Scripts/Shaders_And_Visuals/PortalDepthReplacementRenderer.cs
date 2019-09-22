using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalDepthReplacementRenderer : MonoBehaviour {
	[SerializeField]
	private Shader replacementShader;
	private Material replacementShaderMaterial;
	private RenderTexture updatedDepthNormalsMaterial;
	private Camera cam;

	private void Start() {
		CreateRenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);
		EpitaphScreen.instance.OnScreenResolutionChanged += HandleScreenResolutionChanged;
	}

	private void OnEnable() {
		if (cam == null) {
			cam = GetComponent<Camera>();
		}
		if (replacementShaderMaterial == null && replacementShader != null) {
			replacementShaderMaterial = new Material(replacementShader);
		}
		if (replacementShader != null) {
			cam.SetReplacementShader(replacementShader, "RenderType");
		}
	}

	private void OnPostRender() {
		Shader.SetGlobalTexture("_CameraDepthNormalsTexture", updatedDepthNormalsMaterial);
	}

	private void OnDisable() {
		cam.ResetReplacementShader();
	}

	private void HandleScreenResolutionChanged(int newWidth, int newHeight) {
		if (updatedDepthNormalsMaterial != null) {
			updatedDepthNormalsMaterial.Release();
		}
		CreateRenderTexture(newWidth, newHeight);
	}

	private void CreateRenderTexture(int currentWidth, int currentHeight) {
		updatedDepthNormalsMaterial = new RenderTexture(currentWidth, currentHeight, 24);
		cam.targetTexture = updatedDepthNormalsMaterial;
	}
}
