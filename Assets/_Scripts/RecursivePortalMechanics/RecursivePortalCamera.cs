using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class RecursivePortalCamera : MonoBehaviour {
	public PortalContainer portal;

	private Camera mainCamera;

	// tmp1 and tmp2 are used to iterate through the recursive rendering, holding the result after each iteration
	public static RenderTexture tmp1;
	public static RenderTexture tmp2;

	// How many times to recursively render portals
	private const int MaxDepth = 7;

    void Start() {
		mainCamera = EpitaphScreen.instance.playerCamera;

		if (tmp1 == null) {
			tmp1 = new RenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight, 24, RenderTextureFormat.ARGB32);
		}
		if (tmp2 == null) {
			tmp2 = new RenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight, 24, RenderTextureFormat.ARGB32);
		}
	}

	// Before rendering the main camera (this script should be on the main camera)
	private void OnPreRender() {
		// TODO: Replace with actual portal camera. 
		Camera portalCamera = mainCamera;
		// TODO: Replace with actual collection of all portal surfaces
		Renderer[] portalSurfaces = new Renderer[12];

		//RenderPortalRecurseIteration(0);
	}

	//void RenderPortalRecurseIteration(int depth) {
	//	// Don't render any more than MaxDepth recursions
	//	if (depth > MaxDepth) return;

	//	Shader.SetGlobalInt("_MaskId", depth);

	//	// TODO: Replace with actual portal camera. 
	//	Camera portalCamera = mainCamera;

	//	// Store the starting position/rotation to restore before recursing (mutable state)
	//	Vector3 camPosition = portalCamera.transform.position;
	//	Quaternion camRotation = portalCamera.transform.rotation;

	//	// TODO: Replace with actual collection of all portals
	//	PortalContainer[] allPortals = new PortalContainer[12];

	//	List<PortalContainer> portalsVisible = new List<PortalContainer>();
	//	foreach (var portal in allPortals) {
	//		if (portal.renderer.IsVisibleFrom(portalCamera)) {
	//			portalsVisible.Add(portal);
	//		}
	//	}


	//	foreach (var visiblePortal in portalsVisible) {
	//		// Restore camera transform to its position for this recurse iteration (mutable state)
	//		portalCamera.transform.position = camPosition;
	//		portalCamera.transform.rotation = camRotation;

	//		Transform inTransform = visiblePortal.renderer.transform;
	//		Transform outTransform = visiblePortal.otherPortal.renderer.transform;

	//		// Position the camera behind the other portal.
	//		Vector3 relativePos = inTransform.InverseTransformPoint(portalCamera.transform.position);
	//		relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
	//		portalCamera.transform.position = outTransform.TransformPoint(relativePos);

	//		// Rotate the camera to look through the other portal.
	//		Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * portalCamera.transform.rotation;
	//		relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
	//		portalCamera.transform.rotation = outTransform.rotation * relativeRot;

	//		RenderPortalRecurseIteration(depth + 1);
	//	}
	//}

	//private void RenderCamera(PortalContainer inPortal, PortalContainer outPortal, int iterationID) {
	//	Transform inTransform = inPortal.transform;
	//	Transform outTransform = outPortal.transform;

	//	Transform cameraTransform = portalCamera.transform;
	//	cameraTransform.position = transform.position;
	//	cameraTransform.rotation = transform.rotation;

	//	for (int i = 0; i <= iterationID; ++i) {
	//		// Position the camera behind the other portal.
	//		Vector3 relativePos = inTransform.InverseTransformPoint(cameraTransform.position);
	//		relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
	//		cameraTransform.position = outTransform.TransformPoint(relativePos);

	//		// Rotate the camera to look through the other portal.
	//		Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * cameraTransform.rotation;
	//		relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
	//		cameraTransform.rotation = outTransform.rotation * relativeRot;
	//	}

	//	// Set the camera's oblique view frustum.
	//	Plane p = new Plane(-outTransform.forward, outTransform.position);
	//	Vector4 clipPlane = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
	//	Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamera.worldToCameraMatrix)) * clipPlane;

	//	var newMatrix = mainCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
	//	portalCamera.projectionMatrix = newMatrix;

	//	// Render the camera to its render target.
	//	portalCamera.Render();
	//}
}
