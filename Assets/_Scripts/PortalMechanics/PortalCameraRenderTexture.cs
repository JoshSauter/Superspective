using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class PortalCameraRenderTexture : MonoBehaviour {
	public PortalContainer portal;
	public bool pauseRendering = false;
	EpitaphRenderer thisPortalRenderer;
	EpitaphRenderer thisPortalVolumetricRenderer;

	RenderTexture rt;
	Material cutoutMaterial;

	float minRenderDistance = 10;       // Portal will always be rendering when Player is this many units or closer
	float maxRenderDistance = 200;		// Portal will never render when Player is this many units or further

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
	}

	void OnEnable() {
		EpitaphScreen.instance.OnScreenResolutionChanged += HandleScreenResolutionChanged;
	}

	void OnDisable() {
		EpitaphScreen.instance.OnScreenResolutionChanged -= HandleScreenResolutionChanged;
	}

    private void Update() {
		Camera playerCam = EpitaphScreen.instance.playerCamera;
		Vector3 camToPortal = playerCam.transform.position - transform.position;
		bool playerInFrontOfPortal = Vector3.Dot(camToPortal, portal.teleporter.portalNormal) > 0;
		bool playerCloseToPortal = camToPortal.sqrMagnitude < minRenderDistance * minRenderDistance;
		bool playerFarFromPortal = camToPortal.sqrMagnitude > maxRenderDistance * maxRenderDistance;
		cam.enabled = !playerFarFromPortal && !pauseRendering && (playerCloseToPortal || (playerInFrontOfPortal && thisPortalRenderer.r.IsVisibleFrom(EpitaphScreen.instance.playerCamera)));
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
