using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using System.Linq;
using NaughtyAttributes;
using System;
using UnityStandardAssets.ImageEffects;
using UnityEngine.Assertions;

namespace PortalMechanics {
	// Container for memoizing edge detection color state
	public struct EDColors {
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

	public class VirtualPortalCamera : Singleton<VirtualPortalCamera> {

		[Serializable]
		public class CameraSettings {
			public Vector3 camPosition;
			public Quaternion camRotation;
			public Matrix4x4 camProjectionMatrix;
			[HideInInspector]
			public EDColors edgeColors;
		}

		[Serializable]
		public class RecursiveTextures {
			public RenderTexture mainTexture;
			public RenderTexture depthNormalsTexture;

			public static RecursiveTextures CreateTextures() {
				RecursiveTextures recursiveTextures = new RecursiveTextures {
					mainTexture = new RenderTexture(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight, 24, RenderTextureFormat.DefaultHDR),
					depthNormalsTexture = new RenderTexture(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight, 24, Portal.DepthNormalsTextureFormat)
				};
				return recursiveTextures;
			}

			public void Release() {
				mainTexture.Release();
				depthNormalsTexture.Release();
			}
		}

		public bool DEBUG = false;
		DebugLogger debug;
		public Camera portalCamera;
		public List<MonoBehaviour> postProcessEffects = new List<MonoBehaviour>();
		Camera mainCamera;

		BladeEdgeDetection mainCameraEdgeDetection;
		BladeEdgeDetection portalCameraEdgeDetection;

		public int MaxDepth = 4;
		public int MaxRenderSteps = 12;
		public float MaxRenderDistance = 400;
		public float distanceToStartCheckingPortalBounds = 5f;
		public float clearSpaceBehindPortal = 0.49f;

		int renderSteps;
		public List<RecursiveTextures> renderStepTextures;

		[ShowIf("DEBUG")]
		public List<Portal> portalOrder = new List<Portal>();
		[ShowIf("DEBUG")]
		public List<RecursiveTextures> finishedTex = new List<RecursiveTextures>();
		[ShowIf("DEBUG")]
		public List<RenderTexture> portalMaskTextures = new List<RenderTexture>();

		Shader depthNormalsReplacementShader;
		Shader portalMaskReplacementShader;
		const string depthNormalsReplacementTag = "RenderType";
		const string portalMaskReplacementTag = "PortalTag";
		const string portalMaskTextureName = "_PortalMask";

		static readonly Rect[] fullScreenRect = new Rect[1] { new Rect(0, 0, 1, 1) };

		void Start() {
			debug = new DebugLogger(gameObject, () => DEBUG);
			mainCamera = SuperspectiveScreen.instance.playerCamera;
			portalCamera = GetComponent<Camera>();
			mainCameraEdgeDetection = mainCamera.GetComponent<BladeEdgeDetection>();
			portalCameraEdgeDetection = GetComponent<BladeEdgeDetection>();

			depthNormalsReplacementShader = Shader.Find("Custom/CustomDepthNormalsTexture");
			portalMaskReplacementShader = Shader.Find("Hidden/PortalMask");

			SuperspectiveScreen.instance.OnPlayerCamPreRender += RenderPortals;
			//SuperspectiveScreen.instance.OnPlayerCamPreRender += RenderPortals2;
			SuperspectiveScreen.instance.OnScreenResolutionChanged += (width, height) => ClearRenderTextures();

			renderStepTextures = new List<RecursiveTextures>();
			portalMaskTextures = new List<RenderTexture>();
		}

		void ClearRenderTextures() {
			renderStepTextures.ForEach(rt => rt.Release());
			renderStepTextures.Clear();
			if (DEBUG) {
				portalMaskTextures.ForEach(rt => rt.Release());
				portalMaskTextures.Clear();
			}
		}

		/// <summary>
		/// Will recursively render each portal surface visible in the scene before the Player's Camera draws the scene
		/// </summary>
		void RenderPortals() {
			List<Portal> allActivePortals = PortalManager.instance.activePortals;
			Dictionary<Portal, RecursiveTextures> finishedPortalTextures = new Dictionary<Portal, RecursiveTextures>();

			CameraSettings camSettings = new CameraSettings {
				camPosition = mainCamera.transform.position,
				camRotation = mainCamera.transform.rotation,
				camProjectionMatrix = mainCamera.projectionMatrix,
				edgeColors = new EDColors(mainCameraEdgeDetection)
			};
			Camera maskCam = SuperspectiveScreen.instance.portalMaskCamera;
			Matrix4x4 maskCamOriginalProjMatrix = maskCam.projectionMatrix;
			maskCam.transform.SetParent(transform, false);
			SetCameraSettings(portalCamera, camSettings);

			if (DEBUG) {
				portalOrder.Clear();
				finishedTex.Clear();
			}

			renderSteps = 0;
			foreach (var p in allActivePortals) {
				// Ignore disabled portals
				if (!p.portalIsEnabled) continue;

				float portalSurfaceArea = GetPortalSurfaceArea(p);
				float distanceFromPortalToCam = Vector3.Distance(mainCamera.transform.position, p.ClosestPoint(mainCamera.transform.position));
				// Assumes an 8x8 portal is average size
				Rect[] portalScreenBounds = (distanceFromPortalToCam > distanceToStartCheckingPortalBounds * portalSurfaceArea/64f) ? p.GetScreenRects(mainCamera) : fullScreenRect;

				bool portalRenderingIsPaused = p.pauseRenderingAndLogic || p.pauseRenderingOnly;
				// Always render a portal when its volumetric portal is enabled (PortalIsSeenByCamera may be false when the player is in the portal)
				if ((PortalIsSeenByCamera(p, mainCamera, fullScreenRect, portalScreenBounds) || p.IsVolumetricPortalEnabled()) && !portalRenderingIsPaused) {
					SetCameraSettings(portalCamera, camSettings);

					finishedPortalTextures[p] = RenderPortalDepth(0, p, portalScreenBounds, p.name);

					if (DEBUG) {
						portalOrder.Add(p);
						finishedTex.Add(finishedPortalTextures[p]);
					}
				}
			}

			foreach (var finishedPortalTexture in finishedPortalTextures) {
				finishedPortalTexture.Key.SetTexture(finishedPortalTexture.Value.mainTexture);
				finishedPortalTexture.Key.SetDepthNormalsTexture(finishedPortalTexture.Value.depthNormalsTexture);
			}

			maskCam.transform.SetParent(SuperspectiveScreen.instance.playerCamera.transform, false);
			maskCam.projectionMatrix = mainCamera.projectionMatrix;
			RenderPortalMaskTexture(false);

			debug.LogError($"End of frame: renderSteps: {renderSteps}");
		}

		RecursiveTextures RenderPortalDepth(int depth, Portal portal, Rect[] portalScreenBounds, string tree) {
			if (depth == MaxDepth || renderSteps >= MaxRenderSteps) return null;

			var index = renderSteps;
			renderSteps++;

			SetupPortalCameraForPortal(portal, portal.otherPortal, depth);

			CameraSettings modifiedCamSettings = new CameraSettings {
				camPosition = portalCamera.transform.position,
				camRotation = portalCamera.transform.rotation,
				camProjectionMatrix = portalCamera.projectionMatrix,
				edgeColors = new EDColors(portalCameraEdgeDetection)
			};

			// Key == Visible Portal, Value == visible portal screen bounds
			Dictionary<Portal, Rect[]> visiblePortals = GetVisiblePortalsAndTheirScreenBounds(portal, portalScreenBounds);

			debug.Log($"Index (Depth): {index} ({depth})\nPortal:{portal.name}\nNumVisible:{visiblePortals.Count}\nPortalCamPos:{portalCamera.transform.position}\nTree:{tree}\nScreenBounds:{string.Join(", ", portalScreenBounds)}");

			Dictionary<Portal, RecursiveTextures> visiblePortalTextures = new Dictionary<Portal, RecursiveTextures>();
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
				SetCameraSettings(portalCamera, modifiedCamSettings);
			}

			// RESTORE STATE
			foreach (var visiblePortalKeyVal in visiblePortals) {
				Portal visiblePortal = visiblePortalKeyVal.Key;

				if (ShouldRenderRecursively(depth, portal, visiblePortal)) {
					// Restore the RenderTextures that were in use at this stage
					visiblePortal.SetTexture(visiblePortalTextures[visiblePortalKeyVal.Key].mainTexture);
					visiblePortal.SetDepthNormalsTexture(visiblePortalTextures[visiblePortalKeyVal.Key].depthNormalsTexture);
				}
				else {
					visiblePortal.DefaultMaterial();
				}
			}
			SetCameraSettings(portalCamera, modifiedCamSettings);

			while (renderStepTextures.Count <= index) {
				renderStepTextures.Add(RecursiveTextures.CreateTextures());
			}

			RenderDepthNormalsToPortal(portal, index);
			RenderPortalMaskTexture(true);
			if (DEBUG) {
				while (portalMaskTextures.Count <= index) {
					portalMaskTextures.Add(new RenderTexture(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight, 24, SuperspectiveScreen.instance.portalMaskCamera.targetTexture.format));
				}
				
				Graphics.CopyTexture(SuperspectiveScreen.instance.portalMaskCamera.targetTexture, portalMaskTextures[index]); 
			}

			debug.Log($"Rendering: {index} to {portal.name}'s RenderTexture, depth: {depth}");
			portalCamera.targetTexture = renderStepTextures[index].mainTexture;

			portalCamera.Render();

			portal.SetTexture(renderStepTextures[index].mainTexture);
			return renderStepTextures[index];
		}

		/// <summary>
		/// Disables all post process effects, sets the portal camera's target to be the depthNormalsTexture,
		/// renders the camera with the depthNormalsReplacementShader, copies it to the portal's depthNormalsTexture,
		/// then re-enables all post process effects that were enabled.
		/// </summary>
		/// <param name="portal"></param>
		/// <param name="index"></param>
		void RenderDepthNormalsToPortal(Portal portal, int index) {
			portalCamera.targetTexture = renderStepTextures[index].depthNormalsTexture;
			List<bool> postProcessEffectsWereEnabled = DisablePostProcessEffects();
			portalCamera.RenderWithShader(depthNormalsReplacementShader, depthNormalsReplacementTag);
			portal.SetDepthNormalsTexture(renderStepTextures[index].depthNormalsTexture);
			ReEnablePostProcessEffects(postProcessEffectsWereEnabled);
		}

		/// <summary>
		/// Renders the portalMaskCamera with the portalMaskReplacementShader, then sets the result as _PortalMask global texture
		/// </summary>
		void RenderPortalMaskTexture(bool usePortalCamProjMatrix) {
			Camera maskCam = SuperspectiveScreen.instance.portalMaskCamera;
			Matrix4x4 originalProjMatrix = maskCam.projectionMatrix;
			if (usePortalCamProjMatrix) {
				maskCam.projectionMatrix = portalCamera.projectionMatrix;
			}

			maskCam.RenderWithShader(portalMaskReplacementShader, portalMaskReplacementTag);

			if (usePortalCamProjMatrix) {
				maskCam.projectionMatrix = originalProjMatrix;
			}
			Shader.SetGlobalTexture(portalMaskTextureName, MaskBufferRenderTextures.instance.portalMaskTexture);
		}

		bool ShouldRenderRecursively(int parentDepth, Portal parentPortal, Portal visiblePortal) {
			bool parentRendersRecursively = true;
			if (parentPortal != null) {
				parentRendersRecursively = parentPortal.renderRecursivePortals;
			}
			bool pausedRendering = visiblePortal.pauseRenderingAndLogic || visiblePortal.pauseRenderingOnly;
			return parentDepth < MaxDepth - 1 && IsWithinRenderDistance(visiblePortal, portalCamera) && parentRendersRecursively && !pausedRendering;
		}

		bool IsWithinRenderDistance(Portal portal, Camera camera) {
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
				// Don't render through paused rendering portals
				if (p.pauseRenderingOnly) continue;

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

		void SetCameraSettings(Camera cam, CameraSettings settings) {
			SetCameraSettings(cam, settings.camPosition, settings.camRotation, settings.camProjectionMatrix, settings.edgeColors);
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
			// Position the camera behind the other portal.
			portalCamera.transform.position = inPortal.TransformPoint(portalCamera.transform.position);

			// Rotate the camera to look through the other portal.
			portalCamera.transform.rotation = inPortal.TransformRotation(portalCamera.transform.rotation);

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

		void CopyEdgeColors(BladeEdgeDetection dest, EDColors edgeColors) {
			CopyEdgeColors(dest, edgeColors.edgeColorMode, edgeColors.edgeColor, edgeColors.edgeColorGradient, edgeColors.edgeColorGradientTexture);
		}

		public void CopyEdgeColors(BladeEdgeDetection dest, BladeEdgeDetection.EdgeColorMode edgeColorMode, Color edgeColor, Gradient edgeColorGradient, Texture2D edgeColorGradientTexture) {
			dest.edgeColorMode = edgeColorMode;
			dest.edgeColor = edgeColor;
			dest.edgeColorGradient = edgeColorGradient;
			dest.edgeColorGradientTexture = edgeColorGradientTexture;
		}

		/// <summary>
		/// Sets each post process effect to enabled = false;
		/// </summary>
		/// <returns>The enabled state for each post process effect before it was disabled</returns>
		List<bool> DisablePostProcessEffects() {
			return postProcessEffects.Select(pp => {
				bool wasEnabled = pp.enabled;
				pp.enabled = false;
				return wasEnabled;
			}).ToList();
		}

		/// <summary>
		/// Sets each post process effect's enabled state to what it was before it was disabled
		/// </summary>
		/// <param name="wasEnabled"></param>
		void ReEnablePostProcessEffects(List<bool> wasEnabled) {
			Assert.AreEqual(wasEnabled.Count, postProcessEffects.Count);
			for (int i = 0; i < postProcessEffects.Count; i++) {
				postProcessEffects[i].enabled = wasEnabled[i];
			}
		}

		float GetPortalSurfaceArea(Portal p) {
			float area = 0f;
			foreach (var c in p.colliders) {
				float product = 1f;
				BoxCollider box = c as BoxCollider;
				if (box != null) {
					Vector3 size = box.bounds.size;
					if (size.x > 1) {
						product *= size.x;
					}
					if (size.y > 1) {
						product *= size.y;
					}
					if (size.z > 1) {
						product *= size.z;
					}
				}
				area += product;
			}

			return area;
		}
	}
}
