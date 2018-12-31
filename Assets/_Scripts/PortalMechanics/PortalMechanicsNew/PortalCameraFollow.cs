using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalCameraFollow : MonoBehaviour {
	public PortalReceiver portalBeingRendered;
	public PortalReceiver otherPortal;

	PortalTeleporter teleporterForPortalBeingRendered;
	PortalTeleporter teleporterForOtherPortal;

	Transform source;
	Transform destination;

	Camera portalCamera;

	Camera playerCamera;

	// Use this for initialization
	void Start () {
		playerCamera = EpitaphScreen.instance.playerCamera;

		source = portalBeingRendered.transform;
		destination = otherPortal.transform;

		portalCamera = GetComponent<Camera>();

		teleporterForPortalBeingRendered = portalBeingRendered.GetComponentInChildren<PortalTeleporter>();
		teleporterForOtherPortal = otherPortal.GetComponentInChildren<PortalTeleporter>();
	}

	void LateUpdate() {
		Vector3 cameraPositionInSourceSpace = source.InverseTransformPoint(playerCamera.transform.position - teleporterForPortalBeingRendered.portalNormal * portalBeingRendered.portalFrameDepth);
		Quaternion cameraRotationInSourceSpace = Quaternion.Inverse(source.rotation) * playerCamera.transform.rotation;
		
		transform.position = destination.TransformPoint(cameraPositionInSourceSpace);
		transform.rotation = destination.rotation * cameraRotationInSourceSpace;

		Vector3 destinationNormal = teleporterForOtherPortal.portalNormal;

		// The leeway is to prevent "slices" at the bottom of the portal that fail to render properly (floating point error)
		float leeway = Mathf.Lerp(0f, 1, (playerCamera.transform.position - source.position).magnitude / 100f);
		float distance = Vector3.Dot(destination.position + destinationNormal * (portalBeingRendered.portalFrameDepth - leeway), -destinationNormal);
		Vector4 clipPlaneWorldSpace = new Vector4(destinationNormal.x, destinationNormal.y, destinationNormal.z, distance);
		Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamera.worldToCameraMatrix)) * clipPlaneWorldSpace;
		portalCamera.projectionMatrix = playerCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
	}

	private void OnPreRender() {
		Shader.SetGlobalVector("_InvProjParam", InvProjParam(portalCamera.projectionMatrix));
	}

	/// <summary>
	/// Ref: An Efficient Depth Linearization Method for Oblique View Frustums, Eq. 6.
	/// </summary>
	/// <param name="p">Camera projection matrix</param>
	/// <returns>Shit dude idk</returns>
	public Vector4 InvProjParam(Matrix4x4 p) {
		return new Vector4(
			p.m20 / (p.m00 * p.m23),
			p.m21 / (p.m11 * p.m23),
			-1f / p.m23,
			(-p.m22 + p.m20 * p.m02 / p.m00 + p.m21 * p.m12 / p.m11) / p.m23
		);
	}
}
