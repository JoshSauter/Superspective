using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalCameraFollow : MonoBehaviour {
	public bool DEBUG = false;

	public PortalContainer portalBeingRendered;
	public bool rotateCamera = false;

	PortalTeleporter teleporterForPortalBeingRendered;
	PortalTeleporter teleporterForOtherPortal;

	UnityEngine.Transform source;
	UnityEngine.Transform destination;

    Collider sourceCollider;

	Camera portalCamera;

	Camera playerCamera;

	Matrix4x4 originalProjectionMatrix;

	// Use this for initialization
	IEnumerator Start () {
		playerCamera = EpitaphScreen.instance.playerCamera;

		source = portalBeingRendered.settings.transform;
		destination = portalBeingRendered.otherPortal.settings.transform;

        sourceCollider = portalBeingRendered.teleporter.GetComponent<Collider>();

        portalCamera = GetComponent<Camera>();
		originalProjectionMatrix = portalCamera.projectionMatrix;

		teleporterForPortalBeingRendered = portalBeingRendered.teleporter;
		teleporterForOtherPortal = portalBeingRendered.otherPortal.teleporter;

        while (sourceCollider == null) {
            sourceCollider = portalBeingRendered.teleporter.GetComponent<Collider>();
            yield return null;
        }
	}

	void LateUpdate() {
		Vector3 cameraPositionInSourceSpace = source.InverseTransformPoint(playerCamera.transform.position);
		Quaternion cameraRotationInSourceSpace = Quaternion.Inverse(source.rotation) * playerCamera.transform.rotation;
		
		transform.position = destination.TransformPoint(cameraPositionInSourceSpace);
		transform.rotation = destination.rotation * cameraRotationInSourceSpace;

		Vector3 destinationNormal = teleporterForOtherPortal.portalNormal;

		float dot = Vector3.Dot(teleporterForPortalBeingRendered.portalNormal, playerCamera.transform.position - source.position);
        // TODO: This is an approximation of edge-to-edge distance between colliders, maybe improve this to be true edge-to-edge distance?
        float distanceFromPlayerToPortal = GetDistanceFromPlayerToPortal();
		if (dot < 0.1f || distanceFromPlayerToPortal < 3) {
			portalCamera.projectionMatrix = originalProjectionMatrix;
			return;
		}

		// The leeway is to prevent "slices" at the bottom of the portal that fail to render properly (floating point error)
		float leeway = Mathf.Lerp(0f, 2.5f, distanceFromPlayerToPortal / 100f);
		float distance = Vector3.Dot(destination.position + destinationNormal * -leeway, -destinationNormal);
		Vector4 clipPlaneWorldSpace = new Vector4(destinationNormal.x, destinationNormal.y, destinationNormal.z, distance);
		Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamera.worldToCameraMatrix)) * clipPlaneWorldSpace;
		portalCamera.projectionMatrix = playerCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
	}

	private void OnPreRender() {
		Shader.SetGlobalVector("_InvProjParam", InvProjParam(portalCamera.projectionMatrix));
	}

    private float GetDistanceFromPlayerToPortal() {
        float distance = float.MaxValue;
        if (sourceCollider != null) {
            distance = Vector3.Distance(sourceCollider.ClosestPoint(playerCamera.transform.position), playerCamera.transform.position);
        }
        return distance;
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
