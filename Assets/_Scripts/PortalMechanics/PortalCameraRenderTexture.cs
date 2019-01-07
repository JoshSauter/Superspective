using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalCameraRenderTexture : MonoBehaviour {
	public PortalContainer portal;
	EpitaphRenderer thisPortalRenderer;
	EpitaphRenderer thisPortalVolumetricRenderer;

	Camera cam;
	RenderTexture rt;
	Material cutoutMaterial;

	// Use this for initialization
	void Start () {
		cam = GetComponent<Camera>();
		cutoutMaterial = new Material(Shader.Find("Custom/ScreenCutout"));
		thisPortalRenderer = portal.settings.GetComponent<EpitaphRenderer>();
		if (thisPortalRenderer == null) thisPortalRenderer = portal.settings.gameObject.AddComponent<EpitaphRenderer>();

		GameObject volumetricPortal = portal.volumetricPortal;
		thisPortalVolumetricRenderer = volumetricPortal.GetComponent<EpitaphRenderer>();
		if (thisPortalVolumetricRenderer == null) thisPortalVolumetricRenderer = volumetricPortal.AddComponent<EpitaphRenderer>();

		CreateRenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);
		EpitaphScreen.instance.OnScreenResolutionChanged += HandleScreenResolutionChanged;
	}

	private void HandleScreenResolutionChanged(int newWidth, int newHeight) {
		if (rt != null) {
			rt.Release();
		}
		CreateRenderTexture(newWidth, newHeight);
	}

	private void CreateRenderTexture(int currentWidth, int currentHeight) {
		rt = new RenderTexture(currentWidth, currentHeight, 24);
		cam.targetTexture = rt;

		cutoutMaterial.mainTexture = cam.targetTexture;
		thisPortalRenderer.SetMaterial(cutoutMaterial, false);
		thisPortalVolumetricRenderer.SetMaterial(cutoutMaterial, false);
	}
}
