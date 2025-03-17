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
		}

		/// <summary>
		/// Struct containing the screenBounds defined by an array of Rects, as well as whether or not this Portal should be rendered
		/// </summary>
		[Serializable]
		public struct VisiblePortalInfo {
			public Rect[] screenBounds;
			public bool portalShouldBeRendered;
		}
		
		public delegate void RenderPortalAction(Portal portal);
		public static event RenderPortalAction OnPreRenderPortal;
		public static event RenderPortalAction OnPostRenderPortal;
		
		// For state-collection before being rendered (since state can change during OnPreRenderPortal in non-deterministic ways)
		public static event RenderPortalAction OnEarlyPreRenderPortal;

		public bool DEBUG = false;
		DebugLogger debug;
		public Camera portalCamera;
		public List<MonoBehaviour> postProcessEffects = new List<MonoBehaviour>();
		Camera mainCamera;

		BladeEdgeDetection mainCameraEdgeDetection;
		BladeEdgeDetection portalCameraEdgeDetection;

		public int MaxDepth = 4;
		public int MaxRenderSteps = 24;
		public float MaxRenderDistance = 500;
		public float distanceToStartCheckingPortalBounds = 5f;
		public float clearSpaceBehindPortal = 0.49f;

		int renderSteps;
		public List<RecursiveTextures> renderStepTextures;

		[ShowIf(nameof(DEBUG))]
		public List<Portal> portalOrder = new List<Portal>();
		[ShowIf(nameof(DEBUG))]
		public List<RecursiveTextures> finishedTex = new List<RecursiveTextures>();
		[ShowIf(nameof(DEBUG))]
		public List<RenderTexture> portalMaskTextures = new List<RenderTexture>();
		[ShowIf(nameof(DEBUG))]
		public List<RenderTexture> visibilityMaskTextures = new List<RenderTexture>();

		Shader depthNormalsReplacementShader;
		const string DEPTH_NORMALS_REPLACEMENT_TAG = "RenderType";

		static readonly Rect[] fullScreenRect = new Rect[1] { new Rect(0, 0, 1, 1) };

		void Start() {
			debug = new DebugLogger(gameObject, () => DEBUG);
			mainCamera = SuperspectiveScreen.instance.playerCamera;
			portalCamera = GetComponent<Camera>();
			mainCameraEdgeDetection = mainCamera.GetComponent<BladeEdgeDetection>();
			portalCameraEdgeDetection = GetComponent<BladeEdgeDetection>();

			depthNormalsReplacementShader = Shader.Find("Custom/CustomDepthNormalsTexture");

			SuperspectiveScreen.instance.OnPlayerCamPreRender += RenderPortals;
			SuperspectiveScreen.instance.OnScreenResolutionChanged += (width, height) => ReleaseRenderTextures();

			renderStepTextures = new List<RecursiveTextures>();
			portalMaskTextures = new List<RenderTexture>();
		}

		private void ClearRenderTextureBuffers() {
			renderStepTextures.ForEach(rt => {
				rt.mainTexture.Clear();
				rt.depthNormalsTexture.Clear();
			});
			if (DEBUG) {
				portalMaskTextures.ForEach(rt => rt.Clear());
				visibilityMaskTextures.ForEach(rt => rt.Clear());
			}
		}

		void ReleaseRenderTextures() {
			renderStepTextures.ForEach(rt => rt.Release());
			renderStepTextures.Clear();
			if (DEBUG) {
				portalMaskTextures.ForEach(rt => rt.Release());
				portalMaskTextures.Clear();
				
				visibilityMaskTextures.ForEach(rt => rt.Release());
				visibilityMaskTextures.Clear();
			}
		}

		/// <summary>
		/// Will recursively render each portal surface visible in the scene before the Player's Camera draws the scene
		/// </summary>
		void RenderPortals() {
			if (Portal.forceDebugRenderMode) return;
			
			List<Portal> allActivePortals = PortalManager.instance.activePortals;
			Dictionary<Portal, RecursiveTextures> finishedPortalTextures = new Dictionary<Portal, RecursiveTextures>();
			ClearRenderTextureBuffers();

			CameraSettings mainCamSettings = new CameraSettings {
				camPosition = mainCamera.transform.position,
				camRotation = mainCamera.transform.rotation,
				camProjectionMatrix = mainCamera.projectionMatrix
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
				p.ApplyPortalRenderingModeToRenderers();
				// Ignore disabled portals
				if (!p.PortalRenderingIsEnabled) continue;

				float portalSurfaceArea = GetPortalSurfaceArea(p);
				float distanceFromPortalToCam = Vector3.Distance(mainCamera.transform.position, p.ClosestPoint(mainCamera.transform.position));
				// Assumes an 8x8 portal is average size
				Rect[] portalScreenBounds = (distanceFromPortalToCam > distanceToStartCheckingPortalBounds * portalSurfaceArea/64f) ? p.GetScreenRects(mainCamera) : fullScreenRect;

				// Always render a portal when its volumetric portal is enabled (PortalIsSeenByCamera may be false when the player is in the portal)
				if (PortalIsSeenByCamera(p, mainCamera, fullScreenRect, portalScreenBounds) || p.IsVolumetricPortalEnabled()) {
					SetCameraSettings(portalCamera, mainCamSettings);

					finishedPortalTextures[p] = RenderPortalDepth(0, p, portalScreenBounds, p.ScaleFactor, p.name);

					if (DEBUG) {
						portalOrder.Add(p);
						finishedTex.Add(finishedPortalTextures[p]);
					}
				}
			}

			foreach (var finishedPortal in finishedPortalTextures) {
				finishedPortal.Key.SetTexture(finishedPortal.Value.mainTexture);
				finishedPortal.Key.SetDepthNormalsTexture(finishedPortal.Value.depthNormalsTexture);
			}

			// Reset mask buffer cams and render the VisibilityMask and PortalMask textures once more from the main camera's perspective
			RenderVisibilityMaskTexture(null, mainCamSettings);
			RenderPortalMaskTexture(mainCamSettings);
			// Reset the world scale factor as well
			SetGlobalPortalScale(1f);

			debug.LogError($"End of frame: renderSteps: {renderSteps}");
		}

		/// <summary>
		/// For a given Portal, determines the Camera position, rotation, and projection matrix that should be used to render it,
		/// then checks to see if any Portals are visible from this Camera. For each visible Portal, we recurse with depth + 1
		/// until there are no other visible Portals, or we are at the max depth or max render steps allowed.
		///
		/// Finally, will render the Camera with the settings determined earlier.
		/// </summary>
		/// <param name="depth">Depth of current recursion</param>
		/// <param name="portal">The portal we're trying to render</param>
		/// <param name="portalScreenBounds">The array of Rects defining the screen space taken up by the Portal</param>
		/// <param name="effectivePortalScale">The product of recursive portal's ScaleFactor, used to modify some effects like fog
		/// <param name="tree">Debug string showing the render tree</param>
		/// <returns>The final textures to be used for rendering this Portal</returns>
		RecursiveTextures RenderPortalDepth(int depth, Portal portal, Rect[] portalScreenBounds, float effectivePortalScale, string tree) {
			if (depth == MaxDepth || renderSteps >= MaxRenderSteps) {
				Debug.LogError($"At max depth or max render steps:\nDepth: {depth}/{MaxDepth}\nRenderSteps: {renderSteps}/{MaxRenderSteps}");
				return null;
			}

			var index = renderSteps;
			renderSteps++;

			CameraSettings modifiedCamSettings = SetupPortalCameraForPortal(portal, portal.otherPortal, depth, effectivePortalScale);

			// Key == Visible Portal, Value == visible portal screen bounds
			Dictionary<Portal, VisiblePortalInfo> visiblePortals = GetVisiblePortalsInfo(portal, portalScreenBounds, depth);
			// Key == Visible Portal, Value == RecursiveTextures
			Dictionary<Portal, RecursiveTextures> visiblePortalTextures = new Dictionary<Portal, RecursiveTextures>();

			debug.LogWithContext(
				$"Index (Depth): {index} ({depth})\nPortal:{portal.name}\nNumVisible:{visiblePortals.Count}\nPortalCamPos:{portalCamera.transform.position}\nTree:{tree}\nScreenBounds:{string.Join(", ", portalScreenBounds)}\nEffectiveScale:{effectivePortalScale}",
				portal
			);

			foreach (var visiblePortalTuple in visiblePortals) {
				Portal visiblePortal = visiblePortalTuple.Key;
				VisiblePortalInfo visiblePortalInfo = visiblePortalTuple.Value;

				if (visiblePortalInfo.portalShouldBeRendered) {
					string nextTree = tree + ", " + visiblePortal.name;
					Rect[] nextPortalBounds = IntersectionOfBounds(portalScreenBounds, visiblePortalInfo.screenBounds);

					// Remember state
					visiblePortalTextures[visiblePortal] = RenderPortalDepth(depth + 1, visiblePortal, nextPortalBounds, effectivePortalScale * visiblePortal.ScaleFactor, nextTree);
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
			}
			SetCameraSettings(portalCamera, modifiedCamSettings);

			string portalIdentifier = $"{portal.channel}: {portal.name}";
			while (renderStepTextures.Count <= index) {
				renderStepTextures.Add(RecursiveTextures.CreateTextures($"VirtualPortalCamera_{index}", portalIdentifier));
			}
			
			// Turn off the other portal's volumetric portal if it's enabled
			portal.otherPortal.SetVolumetricHiddenForPortalRendering(true);

			OnEarlyPreRenderPortal?.Invoke(portal);
			OnPreRenderPortal?.Invoke(portal);
			
			// RENDER
			RenderVisibilityMaskTexture(portal, modifiedCamSettings);
			RenderDepthNormalsToPortal(portal, index);
			RenderPortalMaskTexture(modifiedCamSettings);
			if (DEBUG) {
				while (portalMaskTextures.Count <= index) {
					portalMaskTextures.Add(new RenderTexture(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight, 24, SuperspectiveScreen.instance.portalMaskCamera.targetTexture.format));
				}

				while (visibilityMaskTextures.Count <= index) {
					visibilityMaskTextures.Add(new RenderTexture(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight, 24, SuperspectiveScreen.instance.dimensionCamera.targetTexture.format));
				}
				
				Graphics.CopyTexture(SuperspectiveScreen.instance.portalMaskCamera.targetTexture, portalMaskTextures[index]);
				Graphics.CopyTexture(SuperspectiveScreen.instance.dimensionCamera.targetTexture, visibilityMaskTextures[index]);
			}

			debug.LogWithContext($"Rendering: {index} to {portal.name}'s RenderTexture, depth: {depth}", portal);
			portalCamera.targetTexture = renderStepTextures[index].mainTexture;
			renderStepTextures[index].portalName = portalIdentifier;

			portalCamera.Render();
			portal.SetTexture(renderStepTextures[index].mainTexture);
			
			portal.otherPortal.SetVolumetricHiddenForPortalRendering(false);
			
			OnPostRenderPortal?.Invoke(portal);
			
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
			SetGlobalPortalScale(portal.ScaleFactor);
			
			// Render
			portalCamera.RenderWithShader(depthNormalsReplacementShader, DEPTH_NORMALS_REPLACEMENT_TAG);
			portal.SetDepthNormalsTexture(renderStepTextures[index].depthNormalsTexture);
			
			// Restore previous state
			ReEnablePostProcessEffects(postProcessEffectsWereEnabled);
		}

		private void SetGlobalPortalScale(float scaleFactor) {
			Matrix4x4 scalingMatrix = new Matrix4x4(
				new Vector4(1f/scaleFactor, 0, 0, 0),
				new Vector4(0, 1f/scaleFactor, 0, 0),
				new Vector4(0, 0, 1f/scaleFactor, 0),
				new Vector4(0, 0, 0, 1f/scaleFactor));
			Shader.SetGlobalMatrix("_PortalScalingMatrix", scalingMatrix);
			Shader.SetGlobalFloat("_PortalScaleFactor", scaleFactor);
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
		/// Renders the portalMaskCamera with the portalMaskReplacementShader, then sets the result as _PortalMaskWithScale global texture.
		/// Then, renders the portalMaskCamera again but with _PortalScaleFactor set to 1 to render the result to _PortalMask
		/// </summary>
		void RenderPortalMaskTexture(CameraSettings camSettings) {
			Camera maskCam = SuperspectiveScreen.instance.portalMaskCamera;

			Shader portalMaskShader = MaskBufferRenderTextures.instance.portalMaskReplacementShader;
			
			SetCameraSettings(maskCam, camSettings);

			maskCam.RenderWithShader(portalMaskShader, MaskBufferRenderTextures.PORTAL_MASK_REPLACEMENT_TAG);

			Shader.SetGlobalTexture(MaskBufferRenderTextures.PortalMask, MaskBufferRenderTextures.instance.portalMaskTexture);
		}

		bool ShouldRenderRecursively(int parentDepth, Portal parentPortal, Portal visiblePortal) {
			bool IsInvisible() {
				return visiblePortal.TryGetComponent(out DimensionObject dimObj) &&
					dimObj.EffectiveVisibilityState == VisibilityState.Invisible;
			}
			bool parentRendersRecursively = true;
			if (parentPortal != null) {
				parentRendersRecursively = parentPortal.RenderRecursivePortals;
			}
			bool pausedRendering = !visiblePortal.PortalRenderingIsEnabled;
			return parentDepth < MaxDepth - 1 && IsWithinRenderDistance(visiblePortal, portalCamera) && parentRendersRecursively && !pausedRendering && !IsInvisible();
		}

		bool IsWithinRenderDistance(Portal portal, Camera camera) {
			float fovInRadians = camera.fieldOfView * Mathf.PI / 180.0f;
			return Vector3.Distance(portal.ClosestPoint(camera.transform.position), camera.transform.position) < (MaxRenderDistance / Mathf.Cos(fovInRadians / 2.0f));
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
				if (!p.PortalRenderingIsEnabled) continue;

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
				       dimObj.EffectiveVisibilityState == VisibilityState.Invisible;
			}
			bool isInCameraFrustum = testPortal.IsVisibleFrom(cam);
			bool isWithinParentPortalScreenBounds = parentPortalScreenBounds.Any(parentBound => testPortalBounds.Any(testPortalBound => testPortalBound.Overlaps(parentBound)));

			// The isFacingPlayer check can have false negatives when the camera is significantly lagging behind the player, such as in freefall
			// so we use the player's position for this check instead of the camera's
			Vector3 playerPos = Player.instance.transform.position;
			bool isFacingPlayer = Vector3.Dot(testPortal.IntoPortalVector, (playerPos - testPortal.ClosestPoint(playerPos)).normalized) < 0.05f;
			bool result = isInCameraFrustum && isWithinParentPortalScreenBounds && isFacingPlayer && !IsInvisibleDimensionObject();
			return result;
		}

		void SetCameraSettings(Camera cam, CameraSettings settings) {
			cam.transform.position = settings.camPosition;
			cam.transform.rotation = settings.camRotation;
			cam.projectionMatrix = settings.camProjectionMatrix;
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
		
		private CameraSettings SetupPortalCameraForPortal(Portal inPortal, Portal outPortal, int depth, float effectivePortalScale) {
			// Position the camera behind the other portal.
			portalCamera.transform.position = inPortal.TransformPoint(portalCamera.transform.position);

			// Rotate the camera to look through the other portal.
			portalCamera.transform.rotation = inPortal.TransformRotation(portalCamera.transform.rotation);

			// Set the camera's oblique view frustum.
			// Oblique camera matrices break down when distance from camera to portal ~== clearSpaceBehindPortal so we render the default projection matrix when we are < 2*clearSpaceBehindPortal
			Vector3 position = mainCamera.transform.position;
			float distanceToPortal = Vector3.Distance(position, inPortal.ClosestPoint(position, useInfinitelyThinBounds: true)) * inPortal.ScaleFactor;
			bool shouldUseDefaultProjectionMatrix = depth == 0 && (distanceToPortal < 2*clearSpaceBehindPortal || inPortal.PlayerRemainsInPortal || outPortal.PlayerRemainsInPortal);
			if (DEBUG) {
				debugSphere.position = inPortal.ClosestPoint(position, useInfinitelyThinBounds: true);
			}
			// float camDistance = Vector3.Distance(position, inPortal.ClosestPoint(position, useInfinitelyThinBounds: true));
			// float threshold = 2 * clearSpaceBehindPortal * Mathf.Max(Player.instance.scale, 1f);
			// bool shouldUseDefaultProjectionMatrix = depth == 0 && camDistance < threshold;
			// debug.Log($"Cam distance: {camDistance:F3}\nThreshold: {threshold}\nUsing {(shouldUseDefaultProjectionMatrix ? "default" : "oblique")} projection matrix");
			if (!shouldUseDefaultProjectionMatrix) {
				Vector3 closestPointOnOutPortal = outPortal.ClosestPoint(portalCamera.transform.position, useInfinitelyThinBounds: true);

				Plane p = new Plane(-outPortal.IntoPortalVector, closestPointOnOutPortal + clearSpaceBehindPortal * outPortal.IntoPortalVector);
				Vector4 clipPlane = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
				Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamera.worldToCameraMatrix)) * clipPlane;

				var newMatrix = mainCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
				//Debug.Log("Setting custom matrix: " + newMatrix);
				portalCamera.projectionMatrix = newMatrix;
			}
			else {
				debug.LogWarningWithContext("Too close to use oblique projection matrix, using default instead", inPortal.gameObject);
				portalCamera.projectionMatrix = mainCamera.projectionMatrix;
			}
			
			return new CameraSettings {
				camPosition = portalCamera.transform.position,
				camRotation = portalCamera.transform.rotation,
				camProjectionMatrix = portalCamera.projectionMatrix
			};
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
