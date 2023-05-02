using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using System.Linq;
using NaughtyAttributes;
using System;
using UnityStandardAssets.ImageEffects;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

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

			this.edgeColorGradient = new Gradient {
				alphaKeys = edgeDetection.edgeColorGradient.alphaKeys,
				colorKeys = edgeDetection.edgeColorGradient.colorKeys,
				mode = edgeDetection.edgeColorGradient.mode
			};

			this.edgeColorGradientTexture = edgeDetection.edgeColorGradientTexture;
		}

		public EDColors(BladeEdgeDetection.EdgeColorMode edgeColorMode, Color edgeColor, Gradient edgeColorGradient, Texture2D edgeColorGradientTexture) {
			this.edgeColorMode = edgeColorMode;
			this.edgeColor = edgeColor;
			this.edgeColorGradient = new Gradient {
				alphaKeys = edgeColorGradient.alphaKeys,
				colorKeys = edgeColorGradient.colorKeys,
				mode = edgeColorGradient.mode
			};
			this.edgeColorGradientTexture = edgeColorGradientTexture;
		}
	}

	public class VirtualPortalCamera : Singleton<VirtualPortalCamera> {
		[Serializable]
		public class CameraSettings {
			public Vector3 camPosition;
			public Quaternion camRotation;
			public Matrix4x4 camProjectionMatrix;
			// [HideInInspector]
			// public EDColors edgeColors;
		}

		/// <summary>
		/// Struct containing the screenBounds defined by an array of Rects, as well as whether or not this Portal should be rendered
		/// </summary>
		[Serializable]
		public struct VisiblePortalInfo {
			public Rect[] screenBounds;
			public bool portalShouldBeRendered;
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
		public float clearSpaceBehindPortal => 0.49f * Player.instance.scale;

		int renderSteps;
		public List<RecursiveTextures> renderStepTextures;

		[ShowIf("DEBUG")]
		public List<Portal> portalOrder = new List<Portal>();
		[ShowIf("DEBUG")]
		public List<RecursiveTextures> finishedTex = new List<RecursiveTextures>();
		[ShowIf("DEBUG")]
		public List<RenderTexture> portalMaskTextures = new List<RenderTexture>();

		Shader depthNormalsReplacementShader;
		private Shader visibilityMaskReplacementShader;
		Shader portalMaskReplacementShader;
		const string depthNormalsReplacementTag = "RenderType";
		const string visibilityMaskReplacementTag = "RenderType";
		const string portalMaskReplacementTag = "PortalTag";

		static readonly Rect[] fullScreenRect = new Rect[1] { new Rect(0, 0, 1, 1) };

		void Start() {
			debug = new DebugLogger(gameObject, () => DEBUG);
			mainCamera = SuperspectiveScreen.instance.playerCamera;
			portalCamera = GetComponent<Camera>();
			mainCameraEdgeDetection = mainCamera.GetComponent<BladeEdgeDetection>();
			portalCameraEdgeDetection = GetComponent<BladeEdgeDetection>();

			depthNormalsReplacementShader = Shader.Find("Custom/CustomDepthNormalsTexture");
			visibilityMaskReplacementShader = Shader.Find("Hidden/VisibilityMask");
			portalMaskReplacementShader = Shader.Find("Hidden/PortalMask");

			SuperspectiveScreen.instance.OnPlayerCamPreRender += RenderPortals;
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

			CameraSettings mainCamSettings = new CameraSettings {
				camPosition = mainCamera.transform.position,
				camRotation = mainCamera.transform.rotation,
				camProjectionMatrix = mainCamera.projectionMatrix
				// edgeColors = new EDColors(mainCameraEdgeDetection)
			};
			SetCameraSettings(portalCamera, mainCamSettings);
			SetCameraSettings(SuperspectiveScreen.instance.portalMaskCamera, mainCamSettings);
			SetCameraSettings(SuperspectiveScreen.instance.dimensionCamera, mainCamSettings);

			if (DEBUG) {
				portalOrder.Clear();
				finishedTex.Clear();
			}

			renderSteps = 0;
			foreach (var p in allActivePortals) {
				p.SetMaterialsToEffectiveMaterial();
				// Ignore disabled portals
				if (!p.portalRenderingIsEnabled) continue;

				float portalSurfaceArea = GetPortalSurfaceArea(p);
				float distanceFromPortalToCam = Vector3.Distance(mainCamera.transform.position, p.ClosestPoint(mainCamera.transform.position));
				// Assumes an 8x8 portal is average size
				Rect[] portalScreenBounds = (distanceFromPortalToCam > distanceToStartCheckingPortalBounds * portalSurfaceArea/64f) ? p.GetScreenRects(mainCamera) : fullScreenRect;

				// Always render a portal when the player is in the portal (PortalIsSeenByCamera may be false when the player is in the portal)
				if (PortalIsSeenByCamera(p, mainCamera, fullScreenRect, portalScreenBounds) || p.playerRemainsInPortal) {
					SetCameraSettings(portalCamera, mainCamSettings);

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

			// Reset mask buffer cams and render the VisibilityMask and PortalMask textures once more from the main camera's perspective
			RenderVisibilityMaskTexture(null, mainCamSettings);
			RenderPortalMaskTexture(mainCamSettings);

			foreach (var p in finishedPortalTextures.Keys) {
				p.ProtectScreenFromClipping(mainCamera);
			}

			debug.LogError($"End of frame: renderSteps: {renderSteps}");
		}

		/// <summary>
		/// For a given Portal, determines the Camera position, rotation, and projection matrix that should be used to render it,
		/// then checks to see if any Portals are visible from this Camera. For each visible Portal, we recurse with depth + 1
		/// until there are no other visible Portals, or we are at the max depth or max render steps allowed.
		///
		/// Finally, will render the Camera with the settings determined earlier.
		/// </summary>
		/// <param name="depth"></param>
		/// <param name="portal"></param>
		/// <param name="portalScreenBounds"></param>
		/// <param name="tree"></param>
		/// <returns></returns>
		RecursiveTextures RenderPortalDepth(int depth, Portal portal, Rect[] portalScreenBounds, string tree) {
			if (depth == MaxDepth || renderSteps >= MaxRenderSteps) {
				Debug.LogError($"At max depth or max render steps:\nDepth: {depth}/{MaxDepth}\nRenderSteps: {renderSteps}/{MaxRenderSteps}");
				return null;
			}

			var index = renderSteps;
			renderSteps++;

			CameraSettings modifiedCamSettings = SetupPortalCameraForPortal(portal, portal.otherPortal, depth);

			// Key == Visible Portal, Value == visible portal screen bounds
			Dictionary<Portal, VisiblePortalInfo> visiblePortals = GetVisiblePortalsInfo(portal, portalScreenBounds, depth);
			// Key == Visible Portal, Value == RecursiveTextures
			Dictionary<Portal, RecursiveTextures> visiblePortalTextures = new Dictionary<Portal, RecursiveTextures>();
			
			debug.Log("-------------------------------------------------------");

			debug.Log($"Index (Depth): {index} ({depth})\nPortal:{portal.name}\nNumVisible:{visiblePortals.Count}\nPortalCamPos:{portalCamera.transform.position}\nTree:{tree}\nScreenBounds:{string.Join(", ", portalScreenBounds)}");

			foreach (var visiblePortalTuple in visiblePortals) {
				Portal visiblePortal = visiblePortalTuple.Key;
				VisiblePortalInfo visiblePortalInfo = visiblePortalTuple.Value;

				if (visiblePortalInfo.portalShouldBeRendered) {
					string nextTree = tree + ", " + visiblePortal.name;
					Rect[] nextPortalBounds = IntersectionOfBounds(portalScreenBounds, visiblePortalInfo.screenBounds);

					// Remember state
					visiblePortalTextures[visiblePortal] = RenderPortalDepth(depth + 1, visiblePortal, nextPortalBounds, nextTree);
				}

				// RESTORE STATE
				SetCameraSettings(portalCamera, modifiedCamSettings);
			}

			// RESTORE STATE
			foreach (var visiblePortalKeyVal in visiblePortals) {
				Portal visiblePortal = visiblePortalKeyVal.Key;
				VisiblePortalInfo visiblePortalInfo = visiblePortalKeyVal.Value;

				if (visiblePortalInfo.portalShouldBeRendered) {
					RecursiveTextures textures = visiblePortalTextures[visiblePortal];
					// Restore the RenderTextures that were in use at this stage
					visiblePortal.SetTexture(textures.mainTexture);
					visiblePortal.SetDepthNormalsTexture(textures.depthNormalsTexture);
				}
				else {
					bool wasPausedState = visiblePortal.pauseRendering;
					visiblePortal.pauseRendering = true;
					visiblePortal.SetMaterialsToEffectiveMaterial();
					visiblePortal.pauseRendering = wasPausedState;
				}
			}
			SetCameraSettings(portalCamera, modifiedCamSettings);

			while (renderStepTextures.Count <= index) {
				renderStepTextures.Add(RecursiveTextures.CreateTextures($"VirtualPortalCamera_{index}"));
			}
			
			UpdateForPillarDimensionObject(portal);
			
			// RENDER
			// Hide the other portal
			foreach (Renderer otherPortalPortalScreen in portal.otherPortal.portalScreens) {
				otherPortalPortalScreen.enabled = false;
			}
			RenderVisibilityMaskTexture(portal, modifiedCamSettings);
			RenderDepthNormalsToPortal(portal, index);
			RenderPortalMaskTexture(modifiedCamSettings);
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
			
			// Restore previous DimensionObject state
			RestoreForPillarDimensionObject(portal);
			
			// Restore the other portal's visibility
			foreach (Renderer otherPortalPortalScreen in portal.otherPortal.portalScreens) {
				otherPortalPortalScreen.enabled = true;
			}
			
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
			// Setup
			portalCamera.targetTexture = renderStepTextures[index].depthNormalsTexture;
			List<bool> postProcessEffectsWereEnabled = DisablePostProcessEffects();
			
			// Render
			portalCamera.RenderWithShader(depthNormalsReplacementShader, depthNormalsReplacementTag);
			portal.SetDepthNormalsTexture(renderStepTextures[index].depthNormalsTexture);
			
			// Restore previous state
			ReEnablePostProcessEffects(postProcessEffectsWereEnabled);
		}
		
		/// <summary>
		/// Renders the visibilityMaskCamera, then sets the result as _DimensionMask global texture
		/// </summary>
		/// <param name="parentPortal"></param>
		/// <param name="camSettings"></param>
		void RenderVisibilityMaskTexture(Portal parentPortal, CameraSettings camSettings) {
			Camera maskCam = SuperspectiveScreen.instance.dimensionCamera;
			SetCameraSettings(maskCam, camSettings);

			debug.Log($"Rendering visibility mask camera");
			maskCam.Render();
			Shader.SetGlobalTexture(MaskBufferRenderTextures.DimensionMask, MaskBufferRenderTextures.instance.visibilityMaskTexture);
		}

		/// <summary>
		/// Renders the portalMaskCamera with the portalMaskReplacementShader, then sets the result as _PortalMask global texture
		/// </summary>
		void RenderPortalMaskTexture(CameraSettings camSettings) {
			Camera maskCam = SuperspectiveScreen.instance.portalMaskCamera;
			SetCameraSettings(maskCam, camSettings);

			maskCam.RenderWithShader(portalMaskReplacementShader, portalMaskReplacementTag);

			Shader.SetGlobalTexture(MaskBufferRenderTextures.PortalMask, MaskBufferRenderTextures.instance.portalMaskTexture);
		}

		bool ShouldRenderRecursively(int parentDepth, Portal parentPortal, Portal visiblePortal) {
			bool IsInvisible() {
				return visiblePortal.TryGetComponent(out DimensionObject dimObj) &&
					dimObj.effectiveVisibilityState == VisibilityState.invisible;
			}
			bool parentRendersRecursively = true;
			if (parentPortal != null) {
				parentRendersRecursively = parentPortal.renderRecursivePortals;
			}
			bool pausedRendering = !visiblePortal.portalRenderingIsEnabled;
			return parentDepth < MaxDepth - 1 && IsWithinRenderDistance(visiblePortal, portalCamera) && parentRendersRecursively && !pausedRendering && !IsInvisible();
		}

		bool IsWithinRenderDistance(Portal portal, Camera camera) {
			return Vector3.Distance(portal.transform.position, camera.transform.position) < MaxRenderDistance;
		}

		/// <summary>
		/// Finds all visible portals from this portal and stores them in a Dictionary with their screen bounds
		/// </summary>
		/// <param name="portal">The "in" portal</param>
		/// <param name="portalScreenBounds">The screen bounds of the "in" portal, [0-1]</param>
		/// <param name="parentDepth">Depth of the "in" portal</param>
		/// <returns>A dictionary where each key is a visible portal and each value is the screen bounds of that portal</returns>
		Dictionary<Portal, VisiblePortalInfo> GetVisiblePortalsInfo(Portal portal, Rect[] portalScreenBounds, int parentDepth) {
			Dictionary<Portal, VisiblePortalInfo> visiblePortals = new Dictionary<Portal, VisiblePortalInfo>();
			foreach (var p in PortalManager.instance.activePortals) {
				// Ignore the portal we're looking through
				if (p == portal.otherPortal) continue;
				// Ignore disabled portals
				if (!p.portalRenderingIsEnabled) continue;

				VisiblePortalInfo info = new VisiblePortalInfo() {
					screenBounds = p.GetScreenRects(portalCamera),
					portalShouldBeRendered = ShouldRenderRecursively(parentDepth, portal, p)
				};
				if (PortalIsSeenByCamera(p, portalCamera, portalScreenBounds, info.screenBounds)) {
					visiblePortals.Add(p, info);
				}
			}

			return visiblePortals;
		}

		bool PortalIsSeenByCamera(Portal testPortal, Camera cam, Rect[] parentPortalScreenBounds, Rect[] testPortalBounds) {
			bool IsInvisibleDimensionObject() {
				return testPortal.TryGetComponent(out DimensionObject dimObj) &&
				       dimObj.effectiveVisibilityState == VisibilityState.invisible;
			}
			bool isInCameraFrustum = testPortal.IsVisibleFrom(cam);
			bool isWithinParentPortalScreenBounds = parentPortalScreenBounds.Any(parentBound => testPortalBounds.Any(testPortalBound => testPortalBound.Overlaps(parentBound)));
			bool isFacingCamera = true;//testPortal.playerRemainsInPortal || Vector3.Dot(testPortal.PortalNormal(), (cam.transform.position - testPortal.ClosestPoint(cam.transform.position)).normalized) < 0.05f;
			return isInCameraFrustum && isWithinParentPortalScreenBounds && isFacingCamera && !IsInvisibleDimensionObject();
		}

		void SetCameraSettings(Camera cam, CameraSettings settings) {
			SetCameraSettings(cam, settings.camPosition, settings.camRotation, settings.camProjectionMatrix);
		}

		void SetCameraSettings(Camera cam, Vector3 position, Quaternion rotation, Matrix4x4 projectionMatrix) {
			cam.transform.position = position;
			cam.transform.rotation = rotation;
			cam.projectionMatrix = projectionMatrix;

			// CopyEdgeColors(portalCameraEdgeDetection, edgeColors);
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

		private Transform _debugSphere;
		const float _clipOffset = 0.15f;
		private float clipOffset => _clipOffset;

		private Transform debugSphere {
			get {
				if (_debugSphere == null) {
					_debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
					Destroy(_debugSphere.GetComponent<Collider>());
					_debugSphere.localScale = Vector3.one * .25f;
				}

				return _debugSphere;
			}
		}

		CameraSettings SetupPortalCameraForPortal(Portal inPortal, Portal outPortal, int depth) {
			// Position the camera behind the other portal.
			portalCamera.transform.position = inPortal.TransformPoint(portalCamera.transform.position);

			// Rotate the camera to look through the other portal.
			portalCamera.transform.rotation = inPortal.TransformRotation(portalCamera.transform.rotation);

			// Set the camera's oblique view frustum.
			// Oblique camera matrices break down when distance from camera to portal ~== clearSpaceBehindPortal so we render the default projection matrix when we are < 2*clearSpaceBehindPortal
			Vector3 camPos = mainCamera.transform.position;
			bool shouldUseDefaultProjectionMatrix = depth == 0 && Vector3.Distance(camPos, inPortal.ClosestPoint(camPos, useInfinitelyThinBounds: true)) < 2*clearSpaceBehindPortal;
			if (DEBUG) {
				debugSphere.position = inPortal.ClosestPoint(camPos, useInfinitelyThinBounds: true);
			}

			portalCamera.projectionMatrix = shouldUseDefaultProjectionMatrix ? mainCamera.projectionMatrix : GetProjectionMatrix(inPortal, mainCamera);
			// TODO: If the above GetProjectionMatrix call gives the desired result you can delete this commented out code:
			// if (!shouldUseDefaultProjectionMatrix) {
			// 	Vector3 closestPointOnOutPortal = outPortal.ClosestPoint(portalCamera.transform.position, useInfinitelyThinBounds: true);
			//
			// 	Plane p = new Plane(-outPortal.PortalNormal(), closestPointOnOutPortal + clearSpaceBehindPortal * outPortal.PortalNormal());
			// 	Vector4 clipPlane = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
			// 	Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamera.worldToCameraMatrix)) * clipPlane;
			//
			// 	var newMatrix = mainCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
			// 	//Debug.Log("Setting custom matrix: " + newMatrix);
			// 	portalCamera.projectionMatrix = newMatrix;
			// }
			// else {
			// 	debug.LogWarning("Too close to use oblique projection matrix, using default instead");
			// 	portalCamera.projectionMatrix = mainCamera.projectionMatrix;
			// }
			
			return new CameraSettings {
				camPosition = portalCamera.transform.position,
				camRotation = portalCamera.transform.rotation,
				camProjectionMatrix = portalCamera.projectionMatrix
				// edgeColors = new EDColors(portalCameraEdgeDetection)
			};
		}
		
		// Use custom projection matrix to align portal camera's near clip plane with the surface of the portal
		Matrix4x4 GetProjectionMatrix(Portal inPortal, Camera cam) {
			// Learning resource:
			// http://www.terathon.com/lengyel/Lengyel-Oblique.pdf
	
			// Get the transform of the clip plane, which is the surface of the portal
			Transform clipPlane = inPortal.transform;

			// Get the position and normal of the clip plane
			Vector3 clipPlanePos = clipPlane.position;
			Vector3 clipPlaneNormal = clipPlane.forward;

			// Determine whether the camera is in front of or behind the clip plane
			int dot = Math.Sign(Vector3.Dot(clipPlaneNormal, clipPlanePos - cam.transform.position));

			// Transform the clip plane position and normal into camera space
			Vector3 camSpacePos = cam.worldToCameraMatrix.MultiplyPoint(clipPlanePos);
			Vector3 camSpaceNormal = cam.worldToCameraMatrix.MultiplyVector(clipPlaneNormal) * dot;

			// Calculate the distance between the clip plane and the camera in camera space
			float camSpaceDst = -Vector3.Dot(camSpacePos, camSpaceNormal) + clipOffset;


			// If the camera is not too close to the portal, use an oblique projection matrix
			debug.Log($"Using oblique projection matrix for portal {inPortal.FullPath()}");

			// Create a 4D vector representing the clip plane in camera space
			Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst * inPortal.scaleFactor);

			// Update the projection matrix based on the new clip plane using the player's camera settings
			return mainCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
		}


		/// <summary>
		/// For Portals with PillarDimensionObject associated with them, change the effective pillar dimension
		/// to the linked Portal's dimension before rendering
		/// </summary>
		/// <param name="portal">The portal being rendered</param>
		/// <returns>The active DimensionPillar, if any</returns>
		private void UpdateForPillarDimensionObject(Portal portal) {
			// Portals with PillarDimensionObjects are treated specially in that the effective pillar
			// dimension is set to the out portal's dimension before rendering
			PillarDimensionObject portalDimensionObj = (portal?.otherPortal?.dimensionObject is PillarDimensionObject pillarDimensionObject)
				? pillarDimensionObject
				: null;
			DimensionPillar activePillar = portalDimensionObj?.activePillar;
			if (portalDimensionObj != null && activePillar != null) {
				activePillar.dimensionWall.UpdateStateForCamera(portalCamera);
				foreach (PillarDimensionObject dimensionObject in allRelevantPillarDimensionObjects) {
					if (dimensionObject == portalDimensionObj) continue;
					dimensionObject.UpdateStateForCamera(portalCamera, portalDimensionObj.Dimension);
				}
			}
		}

		private void RestoreForPillarDimensionObject(Portal portal) {
			PillarDimensionObject portalDimensionObj = (portal?.otherPortal?.dimensionObject is PillarDimensionObject pillarDimensionObject)
				? pillarDimensionObject
				: null;
			DimensionPillar activePillar = portalDimensionObj?.activePillar;
			
			// Restore previous DimensionObject state
			if (activePillar != null) {
				activePillar.dimensionWall.UpdateStateForCamera(mainCamera);
				foreach (PillarDimensionObject dimensionObject in allRelevantPillarDimensionObjects) {
					if (dimensionObject == portalDimensionObj) continue;
					dimensionObject.UpdateStateForCamera(mainCamera, activePillar.curDimension);
				}
			}
		}
		
		List<PillarDimensionObject> allRelevantPillarDimensionObjects => PillarDimensionObject.allPillarDimensionObjects
			.Where(dimensionObj => dimensionObj.IsVisibleFrom(portalCamera))
			.ToList();

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
