using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using System.Runtime.CompilerServices;
using UnityEditor;
using System.Linq;
using DimensionObjectMechanics;
using GrowShrink;
using LevelManagement;
using MagicTriggerMechanics;
using MagicTriggerMechanics.TriggerConditions;
using Saving;
using SerializableClasses;
using Sirenix.OdinInspector;
using UnityEngine.Events;

namespace PortalMechanics {
	[RequireComponent(typeof(UniqueId))]
	public partial class Portal : SuperspectiveObject<Portal, Portal.PortalSave> {
		private const string PORTAL_NORMAL_PROPERTY = "_PortalNormal"; // Different use case for _PortalNormal than in PortalCopy
		
		private bool ThisPortalIsInActiveScene => LevelManager.instance.activeSceneName == gameObject.scene.name;
		private bool IsInActiveScene => ThisPortalIsInActiveScene || otherPortal.ThisPortalIsInActiveScene;
		protected virtual int PortalsRequiredToActivate => 2;
		
		[Header("Make sure the Transform's Z-direction arrow points into the portal")]
		[TabGroup("General"), GUIColor(.65f, 1f, .65f)]
		public string channel = "<Not set>";

		[TabGroup("General"), GUIColor(.65f, 1f, .65f)]
		public bool changeActiveSceneOnTeleport = false;

		[Tooltip("Enable composite portals if there are multiple renderers that make up the portal surface. Ensure that these renderers are the only children of the portal gameObject.")]
		[TabGroup("General"), GUIColor(.65f, 1f, .65f)]
		public bool compositePortal = false;
		[Tooltip("Double-sided portals will rotate 180 degrees (along with otherPortal) if the player moves around to the backside")]
		[TabGroup("General"), GUIColor(.65f, 1f, .65f)]
		public bool doubleSidedPortals = false;
		private bool isFlipped = false;
		private Quaternion startRotation, flippedRotation;
		public void FlipPortal() {
			debug.Log($"Flipping from {(isFlipped ? "flipped" : "not flipped")} to {(!isFlipped ? "flipped" : "not flipped")}\nFrameCount: {Time.frameCount}, LastTeleportedFrame: {globalLastTeleportedFrame}, ShouldTeleportPlayer: {WouldTeleportPlayer}");
			isFlipped = !isFlipped;
			transform.rotation = isFlipped ? flippedRotation : startRotation;
		}

		private float TimeSinceLastTeleport => Time.time - lastTeleportedTime;

		public Vector3 IntoPortalVector {
			get {
				if (renderers == null || renderers.Length == 0) {
					return transform.forward;
				}
				else {
					return renderers[0].transform.forward;
				}
			}
		}

		Transform playerCamera;
		CameraFollow playerCameraFollow;

		[TabGroup("General"), GUIColor(.65f, 1f, .65f)]
		public Portal otherPortal;

		private Plane PortalPlane => new Plane(IntoPortalVector, transform.position);
		
		// This is only used for a flip portals check, I can't think of a better name though
		private bool WouldTeleportPlayer => trigger.AllConditionsSatisfied;

		// May or may not exist on a Portal, affects what the PortalMaterial is
		[TabGroup("General"), GUIColor(.65f, 1f, .65f)]
		public DimensionObject dimensionObject;
		// May or may not exist on a Portal, affects PortalableObjects that are also PillarDimensionObjects by setting their dimension to the outPortal's dimension
		[TabGroup("General"), GUIColor(.65f, 1f, .65f)]
		public PillarDimensionObject pillarDimensionObject;

#region Events
		// Type declarations
		public delegate void PortalTeleportAction(Portal inPortal, Collider objectTeleported);
		public delegate void SimplePortalTeleportAction(Collider objectTeleported);
		public delegate void PortalPlayerTeleportAction(Portal inPortal);
		public delegate void SimplePortalPlayerTeleportAction();

		// Any object teleported in this Portal
		public event PortalTeleportAction BeforePortalTeleport;
		public event PortalTeleportAction OnPortalTeleport;
		
		// Simple version of any object teleported in this Portal
		public event SimplePortalTeleportAction BeforePortalTeleportSimple;
		public event SimplePortalTeleportAction OnPortalTeleportSimple;
		
		// Player teleported in this Portal
		public event PortalPlayerTeleportAction BeforePortalPlayerTeleport;
		public event PortalPlayerTeleportAction OnPortalPlayerTeleport;
		
		// Simple version of Player teleported in this Portal
		public event SimplePortalPlayerTeleportAction BeforePortalTeleportPlayerSimple;
		public event SimplePortalPlayerTeleportAction OnPortalTeleportPlayerSimple;

		// Any object teleported in any Portal
		public static event PortalTeleportAction BeforeAnyPortalTeleport;
		public static event PortalTeleportAction OnAnyPortalTeleport;
		
		// Simple version of any object teleported in any Portal
		public static event SimplePortalTeleportAction BeforeAnyPortalTeleportSimple;
		public static event SimplePortalTeleportAction OnAnyPortalTeleportSimple;
		
		// Player teleported in any Portal
		public static event PortalPlayerTeleportAction BeforeAnyPortalPlayerTeleport;
		public static event PortalPlayerTeleportAction OnAnyPortalPlayerTeleport;
		
		// Simple version of Player teleported in any Portal
		public static event SimplePortalPlayerTeleportAction BeforeAnyPortalPlayerTeleportSimple;
		public static event SimplePortalPlayerTeleportAction OnAnyPortalPlayerTeleportSimple;
		
		public UnityEvent onPortalTeleport;
		public UnityEvent onOtherPortalTeleport;
		#endregion

#if UNITY_EDITOR
		void OnDrawGizmosSelected() {
			Portal[] otherPortals = FindObjectsOfType<Portal>().Where(p => p != this && p.channel == channel).ToArray();
			Portal otherPortal = (otherPortals.Length > 0) ? otherPortals[0] : null;
			if (otherPortal != null) {
				Color prevGizmosColor = Gizmos.color;
				Gizmos.color = Color.blue;
				Gizmos.DrawLine(transform.position, otherPortal.transform.position);
				Gizmos.color = prevGizmosColor;
			}
		}

		[MenuItem("CONTEXT/Portal/Select other side of portal")]
		public static void SelectOtherSideOfPortal(MenuCommand command) {
			Portal thisPortal = (Portal)command.context;
			Selection.objects = GetOtherPortals(thisPortal).Select(p => p.gameObject).ToArray();
		}

		static Portal GetOtherPortal(Portal thisPortal) {
			return GetOtherPortals(thisPortal).FirstOrDefault();
		}

		static Portal[] GetOtherPortals(Portal thisPortal) {
			return FindObjectsOfType<Portal>().Where(p => p != thisPortal && p.channel == thisPortal.channel).ToArray();
		}

		[Button("Initialize Portal")]
		public void InitializePortal() {
			if (!gameObject.activeInHierarchy) return;
			try {
				CreateCompositeTrigger();
				InitializeRenderers();
				InitializeVolumetricPortals();
			}
			catch (Exception e) {
				Debug.LogError($"{ID} in scene {gameObject.scene.name} failed to initialize, error: {e.StackTrace}");
			}
		}
#endif

#region MonoBehaviour Methods

		protected override void OnDestroy() {
			base.OnDestroy();
			internalRenderTexturesCopy?.Release();
		}

		protected override void Awake() {
			base.Awake();
			
			dimensionObject = gameObject.FindDimensionObjectRecursively<DimensionObject>();
			pillarDimensionObject = dimensionObject as PillarDimensionObject;

			RenderingAwake();
			PhysicsAwake();
			
			if (changeScale) {
				foreach (SuperspectiveRenderer vp in volumetricPortals) {
					vp.SetFloat("_PortalScaleFactor", scaleFactor);
				}
			}
		}
		
		protected override void Start() {
			base.Start();

			if (dimensionObject != null) {
				dimensionObject.ignorePartiallyVisibleLayerChanges = true;
			}
			
			playerCamera = SuperspectiveScreen.instance.playerCamera.transform;
			playerCameraFollow = playerCamera.GetComponent<CameraFollow>();

			CreateRenderTexture(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight);

			ApplyPortalRenderingModeToRenderers();

			startRotation = transform.rotation;
			flippedRotation = Quaternion.AngleAxis(180f, Vector3.up) * startRotation;
		}

		protected override void Init() {
			base.Init();
			
			SuperspectiveScreen.instance.OnScreenResolutionChanged += CreateRenderTexture;
			SetPropertiesOnMaterial();
		}



		protected override void OnEnable() {
			base.OnEnable();
			StartCoroutine(AddPortalCoroutine());
			
			if (dimensionObject != null) {
				foreach (SuperspectiveRenderer r in renderers) {
					// TODO: Uhh does this get called like many times? I should only set the portalMaterial once ideally
					r.OnMaterialChanged += HandleMaterialChanged;
				}
				dimensionObject.OnStateChangeSimple += SetPropertiesOnMaterial;
			}
		}

		protected override void OnDisable() {
			if (!Application.isPlaying) return; // ???
			base.OnDisable();
			
			SuperspectiveScreen.instance.OnScreenResolutionChanged -= CreateRenderTexture;
			PortalManager.instance.RemovePortal(channel, this, PortalsRequiredToActivate);
			
			if (dimensionObject != null) {
				foreach (SuperspectiveRenderer r in renderers) {
					r.OnMaterialChanged -= HandleMaterialChanged;
				}
				dimensionObject.OnStateChangeSimple -= SetPropertiesOnMaterial;
			}
		}


		#endregion
		
		MeshFilter GenerateExtrudedMesh(MeshFilter planarMeshFilter, float extrusionDistance) {
			Mesh planarMesh = planarMeshFilter.sharedMesh;
			Mesh extrudedMesh = new Mesh();
			Matrix4x4[] extrusionMatrix = { Matrix4x4.identity, Matrix4x4.Translate(extrusionDistance * Vector3.forward) };
			Matrix4x4 scalarMatrix = Matrix4x4.Scale(Vector3.one * 0.9f);
			MeshUtils.ExtrudeMesh(planarMesh, extrudedMesh, extrusionMatrix, scalarMatrix, true, true);

			// Assign the mesh to the MeshFilter
			var newMeshObj = new GameObject(VOLUMETRIC_PORTAL_NAME);
			var meshFilter = newMeshObj.AddComponent<MeshFilter>();
			newMeshObj.AddComponent<MeshRenderer>();
			newMeshObj.transform.SetParent(planarMeshFilter.transform, false);
			meshFilter.mesh = extrudedMesh;

			// Optional: Recalculate normals and bounds
			extrudedMesh.RecalculateNormals();
			extrudedMesh.RecalculateBounds();

			return meshFilter;
		}

#region Public Interface
		public void EnablePortal(Portal other) {
			otherPortal = other;
			CreatePortalTeleporter();
		}

		public void DisablePortal() {
			otherPortal = null;
		}

		/// <summary>
		/// Sets the rendering and physics modes for the portal and applies the settings to the renderers and colliders.
		/// </summary>
		/// <param name="renderMode">PortalRenderMode for the Portal to use.</param>
		/// <param name="physicsMode">PortalPhysicsMode for the Portal to use.</param>
		public void SetPortalModes(PortalRenderMode renderMode, PortalPhysicsMode physicsMode) {
			RenderMode = renderMode;
			PhysicsMode = physicsMode;
		}

		public Vector3 ClosestPoint(Vector3 point, bool ignoreDisabledColliders = false, bool useInfinitelyThinBounds = false) {
			float minDistance = float.MaxValue;
			Vector3 closestPoint = point + Vector3.up * minDistance;

			foreach (var c in colliders) {
				bool colliderActive = c.gameObject.activeSelf;
				bool colliderEnabled = c.enabled;
				if (ignoreDisabledColliders && (!colliderActive || !colliderEnabled)) continue;
				
				// Closest point does not work with disabled colliders, so we temporarily turn it on for the calculation
				c.enabled = true;

				Vector3 thisClosestPoint;
				if (useInfinitelyThinBounds) { ;
					thisClosestPoint = PortalPlane.ClosestPointOnPlane(c.ClosestPoint(point));
				}
				else {
					thisClosestPoint = c.ClosestPoint(point);
				}
				
				float thisDistance = Vector3.Distance(thisClosestPoint, point);
				if (thisDistance < minDistance) {
					minDistance = thisDistance;
					closestPoint = thisClosestPoint;
				}

				c.enabled = colliderEnabled;
			}
			return closestPoint;
		}

		public Rect[] GetScreenRects(Camera cam) {
			List<Rect> allRects = new List<Rect>();
			foreach (var r in renderers) {
				Vector3 cen = r.r.bounds.center;
				Vector3 ext = r.r.bounds.extents;

				Vector2 min = cam.WorldToViewportPoint(new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z - ext.z));
				Vector2 max = min;

				//0
				Vector2 point = min;
				SetMinMax(point, ref min, ref max);

				//1
				point = cam.WorldToViewportPoint(new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z - ext.z));
				SetMinMax(point, ref min, ref max);
				
				//2
				point = cam.WorldToViewportPoint(new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z + ext.z));
				SetMinMax(point, ref min, ref max);

				//3
				point = cam.WorldToViewportPoint(new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z + ext.z));
				SetMinMax(point, ref min, ref max);

				//4
				point = cam.WorldToViewportPoint(new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z - ext.z));
				SetMinMax(point, ref min, ref max);

				//5
				point = cam.WorldToViewportPoint(new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z - ext.z));
				SetMinMax(point, ref min, ref max);

				//6
				point = cam.WorldToViewportPoint(new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z + ext.z));
				SetMinMax(point, ref min, ref max);

				//7
				point = cam.WorldToViewportPoint(new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z + ext.z));
				SetMinMax(point, ref min, ref max);

				//min = Vector2.Max(Vector2.zero, min);
				//max = Vector2.Min(Vector2.one, max);

				allRects.Add(new Rect(min.x, min.y, max.x - min.x, max.y - min.y));
			}

			return allRects.ToArray();
		}
		#endregion

#region Portal Vector Transformations
		public virtual Vector3 TransformPoint(Vector3 point) {
			Vector3 relativeObjPos = transform.InverseTransformPoint(point);
			relativeObjPos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeObjPos;
			relativeObjPos *= scaleFactor;
			return otherPortal.transform.TransformPoint(relativeObjPos);
		}

		public virtual Vector3 TransformDirection(Vector3 direction) {
			Vector3 relativeDir = Quaternion.Inverse(transform.rotation) * direction;
			relativeDir = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeDir;
			return otherPortal.transform.rotation * relativeDir;
		}

		public virtual Quaternion TransformRotation(Quaternion rotation) {
			Quaternion relativeRot = Quaternion.Inverse(transform.rotation) * rotation;
			relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
			return otherPortal.transform.rotation * relativeRot;
		}
#endregion

		void TriggerEventsBeforeTeleport(Collider objBeingTeleported) {
			BeforePortalTeleport?.Invoke(this, objBeingTeleported);
			BeforeAnyPortalTeleport?.Invoke(this, objBeingTeleported);
			BeforePortalTeleportSimple?.Invoke(objBeingTeleported);
			BeforeAnyPortalTeleportSimple?.Invoke(objBeingTeleported);

			if (objBeingTeleported.TaggedAsPlayer()) {
				BeforeAnyPortalPlayerTeleportSimple?.Invoke();
				BeforeAnyPortalPlayerTeleport?.Invoke(this);
				BeforePortalPlayerTeleport?.Invoke(this);
				BeforePortalTeleportPlayerSimple?.Invoke();
			}
		}

		void TriggerEventsAfterTeleport(Collider objBeingTeleported) {
			OnPortalTeleport?.Invoke(this, objBeingTeleported);
			OnAnyPortalTeleport?.Invoke(this, objBeingTeleported);
			OnPortalTeleportSimple?.Invoke(objBeingTeleported);
			OnAnyPortalTeleportSimple?.Invoke(objBeingTeleported);
			onPortalTeleport?.Invoke();
			otherPortal.onOtherPortalTeleport?.Invoke();
			
			if (objBeingTeleported.TaggedAsPlayer()) {
				OnAnyPortalPlayerTeleportSimple?.Invoke();
				OnAnyPortalPlayerTeleport?.Invoke(this);
				OnPortalPlayerTeleport?.Invoke(this);
				OnPortalTeleportPlayerSimple?.Invoke();
			}
		}

#region Helper Methods
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void SetMinMax(Vector2 point, ref Vector2 min, ref Vector2 max) {
			min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
			max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
		}

		protected virtual IEnumerator AddPortalCoroutine() {
			while (!gameObject.scene.isLoaded) {
				//debug.Log("Waiting for scene " + gameObject.scene + " to be loaded before adding receiver...");
				yield return null;
			}

			PortalManager.instance.AddPortal(channel, this, PortalsRequiredToActivate);
		}
#endregion

#region Saving

		public override void LoadSave(PortalSave save) {
			channel = save.channel;
			changeActiveSceneOnTeleport = save.changeActiveSceneOnTeleport;
			changeCameraEdgeDetection = save.changeCameraEdgeDetection;
			edgeColorMode = save.edgeColorMode;
			edgeColor = save.edgeColor;
			edgeColorGradient = save.edgeColorGradient;
			renderRecursivePortals = save.renderRecursivePortals;
			compositePortal = save.compositePortal;
			teleportingPlayer = save.teleportingPlayer;
			PhysicsMode = save.physicsMode;
			RenderMode = save.renderMode;
		}
		
		[Serializable]
		public class PortalSave : SaveObject<Portal> {
			public SerializableGradient edgeColorGradient;
			public SerializableColor edgeColor;
			public string channel;
			public BladeEdgeDetection.EdgeColorMode edgeColorMode;
			public bool changeActiveSceneOnTeleport;
			public bool changeCameraEdgeDetection;
			public bool renderRecursivePortals;
			public bool compositePortal;
			public bool teleportingPlayer;
			public PortalPhysicsMode physicsMode;
			public PortalRenderMode renderMode;

			public PortalSave(Portal portal) : base(portal) {
				this.channel = portal.channel;
				this.changeActiveSceneOnTeleport = portal.changeActiveSceneOnTeleport;
				this.changeCameraEdgeDetection = portal.changeCameraEdgeDetection;
				this.edgeColorMode = portal.edgeColorMode;
				this.edgeColor = portal.edgeColor;
				this.edgeColorGradient = portal.edgeColorGradient;
				this.renderRecursivePortals = portal.renderRecursivePortals;
				this.compositePortal = portal.compositePortal;
				this.teleportingPlayer = portal.teleportingPlayer;
				this.physicsMode = portal.PhysicsMode;
				this.renderMode = portal.RenderMode;
			}
		}
		#endregion
	}
}
