using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using System.Linq;
using NaughtyAttributes;
using System;
using UnityStandardAssets.ImageEffects;

namespace PortalMechanics {
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
		public class RenderTreeNodeInfo {
			public bool shouldRenderPortal;
			public int depth;
			public int renderStep;
			public Portal portal;
			public Rect[] portalScreenBounds;
			public CameraSettings cameraSettings;
		}
		public class RenderTreeNode {
			public RenderTreeNode parent;
			public List<RenderTreeNode> children;
			public RenderTreeNodeInfo info;
			public bool isRoot => parent == null;
			public bool isLeaf => children == null || children.Count == 0;
		}

		public class RenderTree {
			public int depth;
			public int renderSteps;
			public RenderTreeNode root;
		}

		[Serializable]
		public class RecursiveTextures {
			public RenderTexture mainTexture;
			public RenderTexture depthNormalsTexture;

			public static RecursiveTextures CreateTextures() {
				RecursiveTextures recursiveTextures = new RecursiveTextures {
					mainTexture = new RenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight, 24, RenderTextureFormat.ARGB32),
					depthNormalsTexture = new RenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight, 24, Portal.DepthNormalsTextureFormat)
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
		Camera mainCamera;

		BladeEdgeDetection mainCameraEdgeDetection;
		BladeEdgeDetection portalCameraEdgeDetection;

		public int MaxDepth = 4;
		public int MaxRenderSteps = 12;
		public float MaxRenderDistance = 400;
		public float distanceToStartCheckingPortalBounds = 5f;
		public float clearSpaceBehindPortal = 0.49f;

		public RenderTreeNode rootRenderTreeNode;
		int renderSteps;
		public List<RecursiveTextures> renderStepTextures;
		public RenderTexture recursiveDepthNormalsTexture;

		[ShowIf("DEBUG")]
		public List<Portal> portalOrder = new List<Portal>();
		[ShowIf("DEBUG")]
		public List<RecursiveTextures> finishedTex = new List<RecursiveTextures>();

		private static readonly Rect[] fullScreenRect = new Rect[1] { new Rect(0, 0, 1, 1) };

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

		void Start() {
			debug = new DebugLogger(gameObject, () => DEBUG);
			mainCamera = EpitaphScreen.instance.playerCamera;
			portalCamera = GetComponent<Camera>();
			mainCameraEdgeDetection = mainCamera.GetComponent<BladeEdgeDetection>();
			portalCameraEdgeDetection = GetComponent<BladeEdgeDetection>();

			EpitaphScreen.instance.OnPlayerCamPreRender += RenderPortals;
			//EpitaphScreen.instance.OnPlayerCamPreRender += RenderPortals2;
			EpitaphScreen.instance.OnScreenResolutionChanged += (width, height) => ClearRenderTextures();

			renderStepTextures = new List<RecursiveTextures>();
			recursiveDepthNormalsTexture = new RenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight, 24, Portal.DepthNormalsTextureFormat);
		}

		void ClearRenderTextures() {
			renderStepTextures.ForEach(rt => rt.Release());
			renderStepTextures.Clear();
			recursiveDepthNormalsTexture.Release();
		}

		void RenderPortals2() {
			RenderTree renderTree = BuildRenderTree();

			Debug.Log(renderTree);

			while (renderStepTextures.Count < renderTree.renderSteps) {
				renderStepTextures.Add(RecursiveTextures.CreateTextures());
			}

			RenderPortalDepthNormals(renderTree);
			RenderPortalMaterials(renderTree);
			//RenderPortalDepthNormals(renderTree);
		}

		void RenderPortalMaterials(RenderTree tree) {
			RenderPortalMaterialsRecursively(tree.root);
		}

		RecursiveTextures RenderPortalMaterialsRecursively(RenderTreeNode renderNode) {
			Dictionary<RenderTreeNode, RecursiveTextures> textureState = new Dictionary<RenderTreeNode, RecursiveTextures>();
			foreach (var child in renderNode.children) {
				if (child.info.shouldRenderPortal) {
					// Remember texture result to restore to portal material before this render step
					textureState[child] = RenderPortalMaterialsRecursively(child);
				}
				else {
					child.info.portal.DefaultMaterial();
				}
			}

			if (renderNode.isRoot) return null;
			SetCameraSettings(portalCamera, renderNode.info.cameraSettings);
			// Restore state for each visible portal
			foreach (var child in renderNode.children) {
				if (child.info.shouldRenderPortal) {
					child.info.portal.SetTexture(textureState[child].mainTexture);
				}
				else {
					child.info.portal.DefaultMaterial();
				}
			}

			portalCamera.targetTexture = renderStepTextures[renderNode.info.renderStep].mainTexture;

			EpitaphScreen.instance.portalMaskCamera.RenderWithShader(Shader.Find("Hidden/PortalMask"), "PortalTag");
			Shader.SetGlobalTexture("_PortalMask", MaskBufferRenderTextures.instance.portalMaskTexture);
			portalCamera.Render();

			renderNode.info.portal.SetTexture(renderStepTextures[renderNode.info.renderStep].mainTexture);
			return renderStepTextures[renderNode.info.renderStep];
		}

		void RenderPortalDepthNormals(RenderTree tree) {
			RenderPortalDepthNormalsRecursively(tree.root);

			SetCameraSettings(portalCamera, tree.root.info.cameraSettings);
			portalCamera.targetTexture = recursiveDepthNormalsTexture;

			// TODO: Don't find the shader every time
			portalCamera.RenderWithShader(Shader.Find("Custom/CustomDepthNormalsTexture"), "RenderType");

			Shader.SetGlobalTexture("_CameraDepthNormalsTexture", recursiveDepthNormalsTexture);
		}

		RecursiveTextures RenderPortalDepthNormalsRecursively(RenderTreeNode renderNode) {
			Dictionary<RenderTreeNode, RecursiveTextures> textureState = new Dictionary<RenderTreeNode, RecursiveTextures>();
			foreach (var child in renderNode.children) {
				if (child.info.shouldRenderPortal) {
					// Remember texture result to restore to portal material before this render step
					textureState[child] = RenderPortalDepthNormalsRecursively(child);
				}
				else {
					child.info.portal.DefaultMaterial();
				}
			}

			if (renderNode.isRoot) return null;
			SetCameraSettings(portalCamera, renderNode.info.cameraSettings);
			// Restore state for each visible portal
			foreach (var child in renderNode.children) {
				if (child.info.shouldRenderPortal) {
					child.info.portal.SetDepthNormalsTexture(textureState[child].depthNormalsTexture);
				}
				else {
					child.info.portal.DefaultMaterial();
				}
			}

			portalCamera.targetTexture = renderStepTextures[renderNode.info.renderStep].depthNormalsTexture;
			// TODO: Don't find the shader every time
			portalCamera.RenderWithShader(Shader.Find("Custom/CustomDepthNormalsTexture"), "RenderType");

			renderNode.info.portal.SetDepthNormalsTexture(renderStepTextures[renderNode.info.renderStep].depthNormalsTexture);
			//Shader.SetGlobalTexture("_CameraDepthNormalsTexture", renderStepTextures[renderNode.info.renderStep].depthNormalsTexture);
			return renderStepTextures[renderNode.info.renderStep];
		}

		/// <summary>
		/// Will recursively render each portal surface visible in the scene before the Player's Camera draws the scene
		/// </summary>
		void RenderPortals() {
			List<Portal> allActivePortals = PortalManager.instance.activePortals;
			Dictionary<Portal, RecursiveTextures> finishedPortalTextures = new Dictionary<Portal, RecursiveTextures>();

			Vector3 camPosition = mainCamera.transform.position;
			Quaternion camRotation = mainCamera.transform.rotation;
			Matrix4x4 camProjectionMatrix = mainCamera.projectionMatrix;
			EDColors edgeColors = new EDColors(mainCameraEdgeDetection);
			EpitaphScreen.instance.portalMaskCamera.transform.SetParent(transform, false);
			SetCameraSettings(portalCamera, camPosition, camRotation, camProjectionMatrix, edgeColors);

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
					SetCameraSettings(portalCamera, camPosition, camRotation, camProjectionMatrix, edgeColors);

					finishedPortalTextures[p] = RenderPortalDepth(0, p, portalScreenBounds, p.name);

					if (DEBUG) {
						portalOrder.Add(p);
						finishedTex.Add(finishedPortalTextures[p]);
					}
				}
			}

			foreach (var finishedPortalTexture in finishedPortalTextures) {
				finishedPortalTexture.Key.SetTexture(finishedPortalTexture.Value.mainTexture);
			}

			EpitaphScreen.instance.portalMaskCamera.transform.SetParent(EpitaphScreen.instance.playerCamera.transform, false);
			EpitaphScreen.instance.portalMaskCamera.RenderWithShader(Shader.Find("Hidden/PortalMask"), "PortalTag");
			Shader.SetGlobalTexture("_PortalMask", MaskBufferRenderTextures.instance.portalMaskTexture);

			debug.LogError("End of frame: renderSteps: " + renderSteps);
		}

		RecursiveTextures RenderPortalDepth(int depth, Portal portal, Rect[] portalScreenBounds, string tree) {
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
				SetCameraSettings(portalCamera, modifiedCamPosition, modifiedCamRotation, modifiedCamProjectionMatrix, edgeColors);
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
			SetCameraSettings(portalCamera, modifiedCamPosition, modifiedCamRotation, modifiedCamProjectionMatrix, edgeColors);

			while (renderStepTextures.Count <= index) {
				renderStepTextures.Add(RecursiveTextures.CreateTextures());
			}

			portalCamera.targetTexture = renderStepTextures[index].depthNormalsTexture;
			// TODO: Don't find the shader every time
			portalCamera.GetComponent<BloomOptimized>().enabled = false;
			portalCamera.GetComponent<ColorfulFog>().enabled = false;
			portalCamera.GetComponent<BladeEdgeDetection>().enabled = false;
			portalCamera.RenderWithShader(Shader.Find("Custom/CustomDepthNormalsTexture"), "RenderType");
			portal.SetDepthNormalsTexture(renderStepTextures[index].depthNormalsTexture);

			debug.Log("Rendering: " + index + " to " + portal.name + "'s RenderTexture, depth: " + depth);
			portalCamera.GetComponent<BloomOptimized>().enabled = true;
			portalCamera.GetComponent<ColorfulFog>().enabled = true;
			portalCamera.GetComponent<BladeEdgeDetection>().enabled = true;
			portalCamera.targetTexture = renderStepTextures[index].mainTexture;

			EpitaphScreen.instance.portalMaskCamera.RenderWithShader(Shader.Find("Hidden/PortalMask"), "PortalTag");
			Shader.SetGlobalTexture("_PortalMask", MaskBufferRenderTextures.instance.portalMaskTexture);
			portalCamera.Render();

			portal.SetTexture(renderStepTextures[index].mainTexture);
			return renderStepTextures[index];
		}

		int renderStepsThisFrame;
		int depthThisFrame;
		private RenderTree BuildRenderTree() {
			RenderTree tree = new RenderTree();

			renderStepsThisFrame = 0;
			depthThisFrame = -1;
			RenderTreeNode root = new RenderTreeNode();
			RenderTreeNodeInfo info = new RenderTreeNodeInfo();
			CameraSettings camSettings = new CameraSettings();
			camSettings.camPosition = mainCamera.transform.position;
			camSettings.camRotation = mainCamera.transform.rotation;
			camSettings.camProjectionMatrix = mainCamera.projectionMatrix;
			camSettings.edgeColors = new EDColors(mainCameraEdgeDetection);

			info.shouldRenderPortal = false;
			info.depth = -1;
			info.renderStep = -1;
			info.portal = null;
			info.portalScreenBounds = fullScreenRect;
			info.cameraSettings = camSettings;

			root.info = info;

			tree.root = BuildRenderTreeRecursively(root);
			tree.renderSteps = renderStepsThisFrame;
			tree.depth = depthThisFrame;

			SetCameraSettings(portalCamera, camSettings);

			return tree;
		}

		private RenderTreeNode BuildRenderTreeRecursively(RenderTreeNode parent) {
			depthThisFrame = Mathf.Max(parent.info.depth, depthThisFrame);

			// Base case: At max depth or max render steps already
			if (parent.info.depth == MaxDepth || renderStepsThisFrame >= MaxRenderSteps) {
				parent.children = new List<RenderTreeNode>();
				return parent;
			}

			Dictionary<Portal, Rect[]> visiblePortals;
			if (parent.isRoot) {
				RenderTreeNode root = parent;
				visiblePortals = GetVisiblePortalsAndTheirScreenBoundsFromMainCamera();

				parent.children = new List<RenderTreeNode>();
				foreach (var visiblePortal in visiblePortals) {
					SetCameraSettings(portalCamera, parent.info.cameraSettings);

					Portal p = visiblePortal.Key;
					Rect[] screenBounds = visiblePortal.Value;

					// Move the portal camera behind the parent portal and see what portals are visible from here
					SetupPortalCameraForPortal(p, p.otherPortal, parent.info.depth+1);
					CameraSettings cameraBehindPortal = new CameraSettings();
					cameraBehindPortal.camPosition = portalCamera.transform.position;
					cameraBehindPortal.camRotation = portalCamera.transform.rotation;
					cameraBehindPortal.camProjectionMatrix = portalCamera.projectionMatrix;
					cameraBehindPortal.edgeColors = new EDColors(portalCameraEdgeDetection);

					RenderTreeNode child = new RenderTreeNode();
					RenderTreeNodeInfo childInfo = new RenderTreeNodeInfo();
					CameraSettings childCamSettings = cameraBehindPortal;

					// Remember which portals should not be rendered so we can change the material to invisible before rendering them
					childInfo.shouldRenderPortal = ShouldRenderRecursively(parent.info.depth, parent.info.portal, p);
					childInfo.depth = 0;
					childInfo.renderStep = ++renderStepsThisFrame - 1;
					childInfo.portal = p;
					childInfo.portalScreenBounds = screenBounds;
					childInfo.cameraSettings = childCamSettings;

					child.info = childInfo;
					child.parent = root;

					parent.children.Add(BuildRenderTreeRecursively(child));

					// If we've run out of render steps, stop trying to add portals to the tree
					if (renderSteps >= MaxRenderSteps) break;
				}
			}
			else {
				visiblePortals = GetVisiblePortalsAndTheirScreenBounds(parent.info.portal, parent.info.portalScreenBounds);

				parent.children = new List<RenderTreeNode>();
				foreach (var visiblePortal in visiblePortals) {
					SetCameraSettings(portalCamera, parent.info.cameraSettings);

					Portal p = visiblePortal.Key;
					Rect[] screenBounds = visiblePortal.Value;

					// Move the portal camera behind the portal and see what portals are visible from here
					SetupPortalCameraForPortal(p, p.otherPortal, parent.info.depth);
					CameraSettings cameraBehindPortal = new CameraSettings();
					cameraBehindPortal.camPosition = portalCamera.transform.position;
					cameraBehindPortal.camRotation = portalCamera.transform.rotation;
					cameraBehindPortal.camProjectionMatrix = portalCamera.projectionMatrix;
					cameraBehindPortal.edgeColors = new EDColors(portalCameraEdgeDetection);

					RenderTreeNode child = new RenderTreeNode();
					RenderTreeNodeInfo childInfo = new RenderTreeNodeInfo();
					CameraSettings childCamSettings = cameraBehindPortal;

					// Remember which portals should not be rendered so we can change the material to invisible before rendering them
					childInfo.shouldRenderPortal = ShouldRenderRecursively(parent.info.depth, parent.info.portal, p);
					childInfo.depth = parent.info.depth + 1;
					childInfo.renderStep = ++renderStepsThisFrame - 1;
					childInfo.portal = p;
					childInfo.portalScreenBounds = screenBounds;
					childInfo.cameraSettings = childCamSettings;

					child.info = childInfo;
					child.parent = parent;

					parent.children.Add(BuildRenderTreeRecursively(child));

					// If we've run out of render steps, stop trying to add portals to the tree
					if (renderSteps >= MaxRenderSteps) break;
				}
			}
			return parent;
		}

		private Dictionary<Portal, Rect[]> GetVisiblePortalsAndTheirScreenBoundsFromMainCamera() {
			Dictionary<Portal, Rect[]> portalsVisibleFromMainCamera = new Dictionary<Portal, Rect[]>();
			foreach (var p in PortalManager.instance.activePortals) {
				if (!p.portalIsEnabled || p.pauseRenderingOnly) continue;

				float portalSurfaceArea = GetPortalSurfaceArea(p);
				float distanceFromPortalToCam = Vector3.Distance(mainCamera.transform.position, p.ClosestPoint(mainCamera.transform.position));
				// Assumes an 8x8 portal is average size
				Rect[] portalScreenBounds = (distanceFromPortalToCam > distanceToStartCheckingPortalBounds * portalSurfaceArea / 64f) ? p.GetScreenRects(mainCamera) : fullScreenRect;
				if (PortalIsSeenByCamera(p, mainCamera, fullScreenRect, portalScreenBounds)) {
					portalsVisibleFromMainCamera.Add(p, portalScreenBounds);
				}
			}

			return portalsVisibleFromMainCamera;
		}

		private bool ShouldRenderRecursively(int parentDepth, Portal parentPortal, Portal visiblePortal) {
			bool parentRendersRecursively = true;
			if (parentPortal != null) {
				parentRendersRecursively = parentPortal.renderRecursivePortals;
			}
			bool pausedRendering = visiblePortal.pauseRenderingAndLogic || visiblePortal.pauseRenderingOnly;
			return parentDepth < MaxDepth - 1 && IsWithinRenderDistance(visiblePortal, portalCamera) && parentRendersRecursively && !pausedRendering;
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

		private void CopyEdgeColors(BladeEdgeDetection dest, EDColors edgeColors) {
			CopyEdgeColors(dest, edgeColors.edgeColorMode, edgeColors.edgeColor, edgeColors.edgeColorGradient, edgeColors.edgeColorGradientTexture);
		}

		public void CopyEdgeColors(BladeEdgeDetection dest, BladeEdgeDetection.EdgeColorMode edgeColorMode, Color edgeColor, Gradient edgeColorGradient, Texture2D edgeColorGradientTexture) {
			dest.edgeColorMode = edgeColorMode;
			dest.edgeColor = edgeColor;
			dest.edgeColorGradient = edgeColorGradient;
			dest.edgeColorGradientTexture = edgeColorGradientTexture;
		}

		private float GetPortalSurfaceArea(Portal p) {
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
