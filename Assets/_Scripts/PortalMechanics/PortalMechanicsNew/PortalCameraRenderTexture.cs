using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalCameraRenderTexture : MonoBehaviour {
	public PortalReceiver portal;
	EpitaphRenderer thisPortalRenderer;

	Camera cam;
	RenderTexture rt;
	Material cutoutMaterial;

	// Use this for initialization
	void Start () {
		cam = GetComponent<Camera>();
		cutoutMaterial = new Material(Shader.Find("Custom/ScreenCutout"));
		thisPortalRenderer = portal.GetComponent<EpitaphRenderer>();
		if (thisPortalRenderer == null) thisPortalRenderer = portal.gameObject.AddComponent<EpitaphRenderer>();

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
	}
}
