using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalCameraTexture : MonoBehaviour {
	public Portal portal;
	public int portalIndex;
	public int otherIndex {
		get { return (portalIndex + 1) % 2; }
	}
	private Transform thisPortal;
	private Transform otherPortal;

	Material cutoutMaterial;
	public EpitaphRenderer thisPortalRenderer;

	Camera thisPortalCamera;
	RenderTexture rt;

	// Use this for initialization
	void Start () {
		thisPortalCamera = portal.portalCameras[portalIndex];
		thisPortal = portal.portals[portalIndex];
		otherPortal = portal.portals[otherIndex];
		thisPortalRenderer = thisPortal.GetComponent<EpitaphRenderer>();
		cutoutMaterial = new Material(Shader.Find("Custom/ScreenCutout"));

		CreateRenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);
		EpitaphScreen.instance.OnScreenResolutionChanged += HandleScreenResolutionChanged;
	}

	private void HandleScreenResolutionChanged(int newWidth, int newHeight) {
		if (rt != null) {
			rt.Release();
		}
		CreateRenderTexture(newWidth, newHeight);
	}

	// Update is called once per frame
	void LateUpdate () {
		// Handle Camera Rotation
		Vector3 thisPortalForward = portal.portalForwards[portalIndex];
		Vector3 otherPortalForward = portal.portalForwards[otherIndex];
		// I don't understand why adding 180 to the angle helps here
		Quaternion portalRotationalDiff = Quaternion.Euler(Quaternion.FromToRotation(thisPortalForward, otherPortalForward).eulerAngles + 180*Vector3.up);
		
		Vector3 newCameraDirection = portalRotationalDiff * portal.playerCamera.transform.forward;
		// I don't understand why this needs to happen
		if (Vector3.Dot(thisPortalForward, otherPortalForward) == -1) {
			newCameraDirection.x *= -1;
			newCameraDirection.y *= -1;
		}
		transform.rotation = Quaternion.LookRotation(newCameraDirection, Vector3.up);

		// Handle Camera Position
		Vector3 cameraWorldPos = portal.playerCamera.transform.position;
		Vector3 cameraThisPortalLocalPos = thisPortal.InverseTransformPoint(cameraWorldPos);
		transform.localPosition = cameraThisPortalLocalPos;
		transform.position -= thisPortalForward * portal.portalOffset;
	}

	private void CreateRenderTexture(int currentWidth, int currentHeight) {
		rt = new RenderTexture(currentWidth, currentHeight, 24);
		thisPortalCamera.targetTexture = rt;

		cutoutMaterial.mainTexture = thisPortalCamera.targetTexture;
		thisPortalRenderer.SetMaterial(cutoutMaterial, false);
	}
}
