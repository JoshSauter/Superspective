using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using System.Linq;
using NaughtyAttributes;

namespace PortalMechanics {
	public class VirtualPortalCamera : Singleton<VirtualPortalCamera> {
		public bool DEBUG = false;
		DebugLogger debug;
		Camera portalCamera;
		Camera mainCamera;

		BladeEdgeDetection mainCameraEdgeDetection;
		BladeEdgeDetection portalCameraEdgeDetection;

		public int MaxDepth = 6;
		public int MaxRenderSteps = 24;
		public float MaxRenderDistance = 400;
		public float distanceToStartCheckingPortalBounds = 5f;
		public float clearSpaceBehindPortal = 0.49f;

		int renderSteps;
		public List<RenderTexture> renderStepTextures;

		[ShowIf("DEBUG")]
		public List<Portal> portalOrder = new List<Portal>();
		[ShowIf("DEBUG")]
		public List<RenderTexture> finishedTex = new List<RenderTexture>();

		private static Rect[] fullScreenRect = new Rect[1] { new Rect(0, 0, 1, 1) };

		// Container for memoizing edge detection color state
		private struct EDColors {
			public BladeEdgeDetection.EdgeColorMode edgeColorMode;
			public Color edgeColor;
			public Gradient edgeColorGradient;
			public Texture2D edgeColorGradientTexture;

			public EDColors(BladeEdgeDetection edgeDetection) {
				this.edgeColorMode = edgeDetection.edgeColorMode;
				this.edgeColor = edgeDetection.edgeColor;

				this.edgeColorGradient = new Gradient();
				this.edgeColorGradient.alphaKeys = edgeDetection.edgeColorGradient.alphaKeys;
				this.edgeColorGradient.colorKeys = edgeDetection.edgeColorGradient.colorKeys;
				this.edgeColorGradient.mode = edgeDetection.edgeColorGradient.mode;

				this.edgeColorGradientTexture = edgeDetection.edgeColorGradientTexture;
			}

			public EDColors(BladeEdgeDetection.EdgeColorMode edgeColorMode, Color edgeColor, Gradient edgeColorGradient, Texture2D edgeColorGradientTexture) {
				this.edgeColorMode = edgeColorMode;
				this.edgeColor = edgeColor;
				this.edgeColorGradient = new Gradient();
				this.edgeColorGradient.alphaKeys = edgeColorGradient.alphaKeys;
				this.edgeColorGradient.colorKeys = edgeColorGradient.colorKeys;
				this.edgeColorGradient.mode = edgeColorGradient.mode;
				this.edgeColorGradientTexture = edgeColorGradientTexture;
			}
		}

		void Start() {
			debug = new DebugLogger(gameObject, () => DEBUG);
			mainCamera = EpitaphScreen.instance.playerCamera;
			portalCamera = GetComponent<Camera>();
			mainCameraEdgeDetection = mainCamera.GetComponent<BladeEdgeDetection>();
			portalCameraEdgeDetection = GetComponent<BladeEdgeDetection>();

			EpitaphScreen.instance.OnPlayerCamPreRender += RenderPortals;
			EpitaphScreen.instance.OnScreenResolutionChanged += (width, height) => ClearRenderTextures();

			renderStepTextures = new List<RenderTexture>();
		}

		void ClearRenderTextures() {
			renderStepTextures.ForEach(rt => rt.Release());
			renderStepTextures.Clear();
		}

		/// <summary>
		/// Will recursively render each portal surface visible in the scene before the Player's Camera draws the scene
		/// </summary>
		void RenderPortals() {
			List<Portal> allActivePortals = PortalManager.instance.activePortals;
			Dictionary<Portal, RenderTexture> finishedPortalTextures = new Dictionary<Portal, RenderTexture>();

			Vector3 camPosition = mainCamera.transform.position;
			Quaternion camRotation = mainCamera.transform.rotation;
			Matrix4x4 camProjectionMatrix = mainCamera.projectionMatrix;
			EDColors edgeColors = new EDColors(mainCameraEdgeDetection);
			SetCameraSettings(portalCamera, camPosition, camRotation, camProjectionMatrix, edgeColors);

			if (DEBUG) {
				portalOrder.Clear();
				finishedTex.Clear();
			}

			renderSteps = 0;
			foreach (var p in allActivePortals) {
				// Ignore disabled portals
				if (!p.portalIsEnabled) continue;

				float distanceFromPortalToCam = Vector3.Distance(mainCamera.transform.position, p.ClosestPoint(mainCamera.transform.position));
				Rect[] portalScreenBounds = (distanceFromPortalToCam > distanceToStartCheckingPortalBounds) ? p.GetScreenRects(mainCamera) : fullScreenRect;

				// Always render a portal when its volumetric portal is enabled (PortalIsSeenByCamera may be false when the player is in the portal)
				if (PortalIsSeenByCamera(p, mainCamera, fullScreenRect, portalScreenBounds) || p.IsVolumetricPortalEnabled()) {
					SetCameraSettings(portalCamera, camPosition, camRotation, camProjectionMatrix, edgeColors);

					finishedPortalTextures[p] = RenderPortalDepth(0, p, portalScreenBounds, p.name);

					if (DEBUG) {
						portalOrder.Add(p);
						finishedTex.Add(finishedPortalTextures[p]);
					}
				}
			}

			foreach (var finishedPortalTexture in finishedPortalTextures) {
				finishedPortalTexture.Key.SetTexture(finishedPortalTexture.Value);
			}

			debug.LogError("End of frame: renderSteps: " + renderSteps);
		}

		RenderTexture RenderPortalDepth(int depth, Portal portal, Rect[] portalScreenBounds, string tree) {
			if (depth == MaxDepth || renderSteps >= MaxRenderSteps) return null;

			var index = renderSteps;
			renderSteps++;

			SetupPortalCameraForPortal(portal, portal.otherPortal, depth);

			Vector3 modifiedCamPosition = portalCamera.transform.position;
			Quaternion modifiedCamRotation = portalCamera.transform.rotation;
			Matrix4x4 modifiedCamProjectionMatrix = portalCamera.projectionMatrix;
			EDColors edgeColors = new EDColors(portalCameraEdgeDetection);

			// Key == Visible Portal, Value == visible portal screen bounds
			Dictionary<Portal, Rect[]> visiblePortals = new Dictionary<Portal, Rect[]>();
			if (portal.renderRecursivePortals) {
				visiblePortals = GetVisiblePortalsAndTheirScreenBounds(portal, portalScreenBounds);
			}

			debug.Log("Depth (Index): " + depth + " (" + index + ")\nPortal: " + portal.name + "\nNumVisible: " + visiblePortals.Count + "\nPortalCamPos: " + portalCamera.transform.position + "\nTree: " + tree + "\nScreenBounds: " + string.Join(", ", portalScreenBounds));

			Dictionary<Portal, RenderTexture> visiblePortalTextures = new Dictionary<Portal, RenderTexture>();
			foreach (var visiblePortalTuple in visiblePortals) {
				Portal visiblePortal = visiblePortalTuple.Key;

				if (ShouldRenderRecursively(depth, portal, visiblePortal)) {
					string nextTree = tree + ", " + visiblePortal.name;
					Rect[] visiblePortalRects = visiblePortalTuple.Value;
					Rect[] nextPortalBounds = IntersectionOfBounds(portalScreenBounds, visiblePortalRects);

					// Remember state
					visiblePortalTextures[visiblePortal] = RenderPortalDepth(depth + 1, visiblePortal, nextPortalBounds, nextTree);
				}
				else {
					visiblePortal.DefaultMaterial();
				}

				// RESTORE STATE
				SetCameraSettings(portalCamera, modifiedCamPosition, modifiedCamRotation, modifiedCamProjectionMatrix, edgeColors);
			}

			// RESTORE STATE
			foreach (var visiblePortalKeyVal in visiblePortals) {
				Portal visiblePortal = visiblePortalKeyVal.Key;

				if (ShouldRenderRecursively(depth, portal, visiblePortal)) {
					// Restore the RenderTextures that were in use at this stage
					visiblePortal.SetTexture(visiblePortalTextures[visiblePortalKeyVal.Key]);
				}
				else {
					visiblePortal.DefaultMaterial();
				}
			}
			SetCameraSettings(portalCamera, modifiedCamPosition, modifiedCamRotation, modifiedCamProjectionMatrix, edgeColors);

			while (renderStepTextures.Count <= index) {
				renderStepTextures.Add(new RenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight, 24, RenderTextureFormat.ARGB32));
			}

			debug.Log("Rendering: " + index + " to " + portal.name + "'s RenderTexture, depth: " + depth);
			portalCamera.targetTexture = renderStepTextures[index];

			portalCamera.Render();

			portal.SetTexture(renderStepTextures[index]);
			return renderStepTextures[index];
		}

		private bool ShouldRenderRecursively(int depth, Portal portal, Portal visiblePortal) {
			return depth < MaxDepth - 1 && IsWithinRenderDistance(visiblePortal, portalCamera) && portal.renderRecursivePortals;
		}

		private bool IsWithinRenderDistance(Portal portal, Camera camera) {
			return Vector3.Distance(portal.transform.position, camera.transform.position) < MaxRenderDistance;
		}

		/// <summary>
		/// Finds all visible portals from this portal and stores them in a Dictionary with their screen bounds
		/// </summary>
		/// <param name="portal">The "in" portal</param>
		/// <param name="portalScreenBounds">The screen bounds of the "in" portal, [0-1]</param>
		/// <returns>A dictionary where each key is a visible portal and each value is the screen bounds of that portal</returns>
		Dictionary<Portal, Rect[]> GetVisiblePortalsAndTheirScreenBounds(Portal portal, Rect[] portalScreenBounds) {
			Dictionary<Portal, Rect[]> visiblePortals = new Dictionary<Portal, Rect[]>();
			foreach (var p in PortalManager.instance.activePortals) {
				// Ignore the portal we're looking through
				if (p == portal.otherPortal) continue;
				// Ignore disabled portals
				if (!p.portalIsEnabled) continue;

				Rect[] testPortalBounds = p.GetScreenRects(portalCamera);
				if (PortalIsSeenByCamera(p, portalCamera, portalScreenBounds, testPortalBounds)) {
					visiblePortals.Add(p, testPortalBounds);
				}
			}

			return visiblePortals;
		}

		bool PortalIsSeenByCamera(Portal testPortal, Camera cam, Rect[] parentPortalScreenBounds, Rect[] testPortalBounds) {
			bool isInCameraFrustum = testPortal.IsVisibleFrom(cam);
			bool isWithinParentPortalScreenBounds = parentPortalScreenBounds.Any(parentBound => testPortalBounds.Any(testPortalBound => testPortalBound.Overlaps(parentBound)));
			bool isFacingCamera = Vector3.Dot(testPortal.PortalNormal(), (cam.transform.position - testPortal.ClosestPoint(cam.transform.position)).normalized) < 0.05f;
			return isInCameraFrustum && isWithinParentPortalScreenBounds && isFacingCamera;
		}

		void SetCameraSettings(Camera cam, Vector3 position, Quaternion rotation, Matrix4x4 projectionMatrix, EDColors edgeColors) {
			cam.transform.position = position;
			cam.transform.rotation = rotation;
			cam.projectionMatrix = projectionMatrix;

			CopyEdgeColors(portalCameraEdgeDetection, edgeColors);
		}

		Rect[] IntersectionOfBounds(Rect[] boundsA, Rect[] boundsB) {
			List<Rect> intersection = new List<Rect>();
			foreach (var a in boundsA) {
				foreach (var b in boundsB) {
					if (a.Overlaps(b)) {
						intersection.Add(IntersectionOfBounds(a, b));
					}
				}
			}
			return intersection.ToArray();
		}

		Rect IntersectionOfBounds(Rect a, Rect b) {
			Rect intersection = new Rect();
			intersection.min = Vector2.Max(a.min, b.min);
			intersection.max = Vector2.Min(a.max, b.max);
			return intersection;
		}

		void SetupPortalCameraForPortal(Portal inPortal, Portal outPortal, int depth) {
			Transform inTransform = inPortal.transform;
			Transform outTransform = outPortal.transform;

			// Position the camera behind the other portal.
			Vector3 relativePos = inTransform.InverseTransformPoint(portalCamera.transform.position);
			relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
			portalCamera.transform.position = outTransform.TransformPoint(relativePos);

			// Rotate the camera to look through the other portal.
			Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * portalCamera.transform.rotation;
			relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
			portalCamera.transform.rotation = outTransform.rotation * relativeRot;

			// Set the camera's oblique view frustum.
			// Oblique camera matrices break down when distance from camera to portal ~== clearSpaceBehindPortal so we render the default projection matrix when we are < 2*clearSpaceBehindPortal
			bool shouldUseDefaultProjectionMatrix = depth == 0 && Vector3.Distance(mainCamera.transform.position, inPortal.ClosestPoint(mainCamera.transform.position)) < 2*clearSpaceBehindPortal;
			if (!shouldUseDefaultProjectionMatrix) {
				Vector3 closestPointOnOutPortal = outPortal.ClosestPoint(portalCamera.transform.position);

				Plane p = new Plane(-outPortal.PortalNormal(), closestPointOnOutPortal + clearSpaceBehindPortal * outPortal.PortalNormal());
				Vector4 clipPlane = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
				Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamera.worldToCameraMatrix)) * clipPlane;

				var newMatrix = mainCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
				//Debug.Log("Setting custom matrix: " + newMatrix);
				portalCamera.projectionMatrix = newMatrix;
			}
			else {
				portalCamera.projectionMatrix = mainCamera.projectionMatrix;
			}

			// Modify the camera's edge detection if necessary
			if (inPortal != null && inPortal.changeCameraEdgeDetection) {
				CopyEdgeColors(portalCameraEdgeDetection, inPortal.edgeColorMode, inPortal.edgeColor, inPortal.edgeColorGradient, inPortal.edgeColorGradientTexture);
			}
		}

		public void CopyEdgeColors(BladeEdgeDetection dest, BladeEdgeDetection source) {
			CopyEdgeColors(dest, source.edgeColorMode, source.edgeColor, source.edgeColorGradient, source.edgeColorGradientTexture);
		}

		private void CopyEdgeColors(BladeEdgeDetection dest, EDColors edgeColors) {
			CopyEdgeColors(dest, edgeColors.edgeColorMode, edgeColors.edgeColor, edgeColors.edgeColorGradient, edgeColors.edgeColorGradientTexture);
		}

		public void CopyEdgeColors(BladeEdgeDetection dest, BladeEdgeDetection.EdgeColorMode edgeColorMode, Color edgeColor, Gradient edgeColorGradient, Texture2D edgeColorGradientTexture) {
			dest.edgeColorMode = edgeColorMode;
			dest.edgeColor = edgeColor;
			dest.edgeColorGradient = edgeColorGradient;
			dest.edgeColorGradientTexture = edgeColorGradientTexture;
		}
	}
}
