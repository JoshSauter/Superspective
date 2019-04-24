using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class PortalCameraRenderTexture : MonoBehaviour {
	public PortalContainer portal;
	EpitaphRenderer thisPortalRenderer;
	EpitaphRenderer thisPortalVolumetricRenderer;

	RenderTexture rt;
	Material cutoutMaterial;

    Camera cam;

	// Use this for initialization
	void Start () {
        cam = portal.portalCamera;
		cutoutMaterial = new Material(Shader.Find("Custom/ScreenCutout"));
		thisPortalRenderer = GetComponent<EpitaphRenderer>();
		if (thisPortalRenderer == null) thisPortalRenderer = gameObject.AddComponent<EpitaphRenderer>();

		GameObject volumetricPortal = portal.volumetricPortal;
		thisPortalVolumetricRenderer = volumetricPortal.GetComponent<EpitaphRenderer>();
		if (thisPortalVolumetricRenderer == null) thisPortalVolumetricRenderer = volumetricPortal.AddComponent<EpitaphRenderer>();

		CreateRenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);
		EpitaphScreen.instance.OnScreenResolutionChanged += HandleScreenResolutionChanged;
	}

    private void Update() {
        cam.enabled = thisPortalRenderer.r.IsVisibleFrom(EpitaphScreen.instance.playerCamera);
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
