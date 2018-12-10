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
	public Quaternion debugQ = new Quaternion(0,0,1,1);
	public Vector3 debugV = new Vector3();

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
		// Clean self up if portal references break (they will be re-initialized by Portal.InitializePortal())
		if (thisPortal == null || otherPortal == null) {
			Destroy(gameObject);
			Destroy(transform.parent.GetComponentInChildren<TeleportEnter>().gameObject);
			return;
		}

		// Handle Camera Rotation
		Vector3 thisPortalForward = portal.portalForwards[portalIndex];
		Vector3 otherPortalForward = portal.portalForwards[otherIndex];
		// TODO: Fix this for non-0/180 degree angles between portals
		transform.rotation = thisPortal.transform.rotation * (otherPortal.transform.rotation * portal.playerCamera.transform.rotation);

		// Handle Camera Position
		Vector3 cameraWorldPos = portal.playerCamera.transform.position;
		Vector3 cameraThisPortalLocalPos = thisPortal.InverseTransformPoint(cameraWorldPos);
		transform.localPosition = cameraThisPortalLocalPos;
		transform.position -= otherPortalForward * portal.portalFrameDepth;

		// Handle Camera Oblique Frustum (cull objects behind portal)
		//Plane clipPlane = new Plane(transform.InverseTransformDirection(thisPortalForward).normalized, transform.InverseTransformPoint(thisPortal.position));
		//float distance = (cameraWorldPos - clipPlane.ClosestPointOnPlane(cameraWorldPos)).magnitude;
		//float oldDistance = (cameraWorldPos - thisPortal.position).magnitude;
		////print(distance + "\n" + oldDistance);
		////print(thisPortalForward + "\n" + (Vector3.Project(thisPortalCamera.transform.forward, thisPortalForward)));
		//print(clipPlane.normal);
		//debugV = -clipPlane.normal.normalized;
		//Matrix4x4 test = thisPortalCamera.CalculateObliqueMatrix(new Vector4(debugV.x, debugV.y, debugV.z, Mathf.Max(1, debugQ.w)));
		//print(thisPortalCamera.projectionMatrix + "\n" + test);
		//thisPortalCamera.projectionMatrix = test;
		
	}

	private void CreateRenderTexture(int currentWidth, int currentHeight) {
		// Clean self up if portal references break (they will be re-initialized by Portal.InitializePortal())
		if (thisPortalCamera == null || thisPortalRenderer == null && gameObject != null) {
			thisPortal.GetComponent<Portal>().isInitialized = false;
			Destroy(gameObject);
			Destroy(transform.parent.GetComponentInChildren<TeleportEnter>().gameObject);
			return;
		}

		rt = new RenderTexture(currentWidth, currentHeight, 24);
		thisPortalCamera.targetTexture = rt;

		cutoutMaterial.mainTexture = thisPortalCamera.targetTexture;
		thisPortalRenderer.SetMaterial(cutoutMaterial, false);
	}
}
