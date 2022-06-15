using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using SuperspectiveUtils;
using System.Runtime.CompilerServices;
using UnityEditor;
using System.Linq;
using LevelManagement;
using MagicTriggerMechanics;
using Saving;
using SerializableClasses;
using UnityEngine.Events;

namespace PortalMechanics {
	[Serializable]
	public class RecursiveTextures {
		public RenderTexture mainTexture;
		public RenderTexture depthNormalsTexture;

		public static RecursiveTextures CreateTextures(string name) {
			int width = SuperspectiveScreen.instance.currentPortalWidth;
			int height = SuperspectiveScreen.instance.currentPortalHeight;
			
			RecursiveTextures recursiveTextures = new RecursiveTextures {
				mainTexture = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR),
				depthNormalsTexture = new RenderTexture(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight, 24, Portal.DepthNormalsTextureFormat, RenderTextureReadWrite.Linear)
			};
			recursiveTextures.mainTexture.name = $"{name}_MainTex";
			recursiveTextures.depthNormalsTexture.name = $"{name}_DepthNormals";
			return recursiveTextures;
		}

		public void Release() {
			mainTexture.Release();
			depthNormalsTexture.Release();
			GameObject.Destroy(mainTexture);
			GameObject.Destroy(depthNormalsTexture);
		}
	}
	
	[RequireComponent(typeof(UniqueId))]
	public class Portal : SaveableObject<Portal, Portal.PortalSave> {
		public static RenderTextureFormat DepthNormalsTextureFormat = RenderTextureFormat.ARGB32;

		[Header("Make sure the Transform's Z-direction arrow points into the portal")]
		public string channel = "<Not set>";

		public bool changeActiveSceneOnTeleport = false;
		public bool changeCameraEdgeDetection;
		[ShowIf("changeCameraEdgeDetection")]
		public BladeEdgeDetection.EdgeColorMode edgeColorMode;
		[ShowIf("changeCameraEdgeDetection")]
		public Color edgeColor = Color.black;
		[ShowIf("changeCameraEdgeDetection")]
		public Gradient edgeColorGradient;
		[ShowIf("changeCameraEdgeDetection")]
		public Texture2D edgeColorGradientTexture;

		public bool renderRecursivePortals = false;
		[Tooltip("Enable composite portals if there are multiple renderers that make up the portal surface. Ensure that these renderers are the only children of the portal gameObject.")]
		public bool compositePortal = false;

		[Tooltip("Double-sided portals will rotate 180 degrees (along with otherPortal) if the player moves around to the backside")]
		public bool doubleSidedPortals = false;

		GameObject volumetricPortalPrefab;
		SuperspectiveRenderer[] volumetricPortals;
		private const float volumetricPortalEnableDistance = 5f;

		private bool volumetricPortalsShouldBeEnabled {
			get {
				if (!hasInitialized || !portalIsEnabled || !playerCamIsCloseEnoughToInFrontOfPortal || dimensionObject || IsInvisible) return false;
				Vector3 playerCamPos = playerCamera.position;
				float playerCamToPortal = Vector3.Distance(playerCamPos, ClosestPoint(playerCamPos, true));
				float playerCamToOtherPortal = Vector3.Distance(playerCamPos, otherPortal.ClosestPoint(playerCamPos, true));
				return playerCamToPortal <= volumetricPortalEnableDistance && playerCamToPortal <= playerCamToOtherPortal;
			}
		}

		private bool IsInvisible {
			get {
				if (dimensionObject != null) {
					return dimensionObject.effectiveVisibilityState == VisibilityState.invisible;
				}

				return false;
			}
		}

		private bool playerCamIsCloseEnoughToInFrontOfPortal {
			get {
				Vector3 playerCamPos = playerCamera.position;
				Vector3 portalPos = transform.position;
				Vector3 outOfPortal = -PortalNormal();
				Vector3 adjustedPortalPos = portalPos - outOfPortal * portalThickness;
				return Vector3.Dot(playerCamPos - adjustedPortalPos, outOfPortal) > 0;
			}
		}
		Material portalMaterial;
		
		public Material fallbackMaterial;
		public SuperspectiveRenderer[] renderers;
		public Collider[] colliders;
		Transform playerCamera;
		CameraFollow playerCameraFollow;
		bool teleportingPlayer = false;

		[HorizontalLine]

		public Portal otherPortal;
		public HashSet<PortalableObject> objectsInPortal = new HashSet<PortalableObject>();

		[SerializeField]
		[ShowIf("DEBUG")]
		RecursiveTextures internalRenderTexturesCopy;

		public bool pauseRenderingOnly = false;
		public bool pauseRenderingAndLogic = false;
		public bool portalIsEnabled => otherPortal != null && !pauseRenderingAndLogic && gameObject.activeSelf;

		[ShowNativeProperty]
		public bool playerRemainsInPortal => volumetricPortals?.Any(vp => vp.enabled) ?? false;

		// May or may not exist on a Portal, affects what the PortalMaterial is
		public DimensionObject dimensionObject;
		public CompositeMagicTrigger trigger;

		private const float portalThickness = 0.55f;

#region Events
		public delegate void PortalTeleportAction(Portal inPortal, Collider objectTeleported);
		public delegate void SimplePortalTeleportAction(Collider objectTeleported);

		public delegate void SimplePortalTeleportPlayerAction();
		public event PortalTeleportAction BeforePortalTeleport;
		public event PortalTeleportAction OnPortalTeleport;
		public event SimplePortalTeleportAction BeforePortalTeleportSimple;
		public event SimplePortalTeleportAction OnPortalTeleportSimple;
		
		public event SimplePortalTeleportPlayerAction BeforePortalTeleportPlayerSimple;
		public event SimplePortalTeleportPlayerAction OnPortalTeleportPlayerSimple;

		public static event PortalTeleportAction BeforeAnyPortalTeleport;
		public static event PortalTeleportAction OnAnyPortalTeleport;
		public static event SimplePortalTeleportAction BeforeAnyPortalTeleportSimple;
		public static event SimplePortalTeleportAction OnAnyPortalTeleportSimple;
		public UnityEvent onPortalTeleport;
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
			Selection.objects = FindObjectsOfType<Portal>().Where(p => p != thisPortal && p.channel == thisPortal.channel).Select(p => p.gameObject).ToArray();
		}
#endif

#region MonoBehaviour Methods
		private void OnValidate() {
			CreateCompositeTrigger();
		}

		protected override void OnDestroy() {
			base.OnDestroy();
			internalRenderTexturesCopy?.Release();
		}

		protected override void Awake() {
			base.Awake();
			
			dimensionObject = gameObject.FindDimensionObjectRecursively<DimensionObject>();
			string shaderPath = "Shaders/Suberspective/SuberspectivePortal";
			portalMaterial = new Material(Resources.Load<Shader>(shaderPath));
			if (fallbackMaterial == null) {
				fallbackMaterial = Resources.Load<Material>("Materials/Invisible");
			}

			if (renderers == null || renderers.Length == 0) {
				if (compositePortal) {
					renderers = GetComponentsInChildren<Renderer>()
						.Select(r => r.GetOrAddComponent<SuperspectiveRenderer>()).ToArray();
				}
				else {
					renderers = new SuperspectiveRenderer[] { GetComponents<Renderer>().Select(r => r.GetOrAddComponent<SuperspectiveRenderer>()).FirstOrDefault() };
				}
			}
			foreach (var r in renderers) {
				r.gameObject.layer = LayerMask.NameToLayer("Portal");
				r.SetMaterial(pauseRenderingAndLogic ? fallbackMaterial : portalMaterial);
			}
			if (colliders == null || colliders.Length == 0) {
				if (compositePortal) {
					colliders = GetComponentsInChildren<Collider>();
				}
				else {
					colliders = new Collider[] { GetComponent<Collider>() };
				}
			}

			if (colliders.Length == 0) {
				Debug.LogError("No Colliders found in this object or its children", gameObject);
				enabled = false;
				return;
			}
			
			CreateCompositeTrigger();
			InitializeCompositeTrigger();

			foreach (var c in colliders) {
				if (c.gameObject != this.gameObject) {
					// PortalColliders handle non-player objects passing through portals
					c.gameObject.AddComponent<PortalCollider>().portal = this;
					if (c is BoxCollider boxCollider) {
						var size = boxCollider.size;
						size = new Vector3(size.x, size.y, portalThickness);
						boxCollider.size = size;
					}
				}
			}

			volumetricPortalPrefab = Resources.Load<GameObject>("Prefabs/VolumetricPortal");
			volumetricPortals = colliders.Select(r => Instantiate(volumetricPortalPrefab, r.transform, false).GetOrAddComponent<SuperspectiveRenderer>()).ToArray();
			for (int i = 0; i < volumetricPortals.Length; i++) {
				SuperspectiveRenderer vp = volumetricPortals[i];
				Collider collider = colliders[i];
				Vector3 vpScale = Vector3.one;
				if (collider is BoxCollider boxCollider) {
					vpScale = boxCollider.size / 10f;
				}
				else if (collider is MeshCollider meshCollider) {
					vpScale = meshCollider.bounds.size / 10f;
				}
				else {
					Debug.LogError("Collider type: " + collider.GetType().ToString() + " not handled.");
				}
				vpScale.z = 1f;
				vp.transform.localScale = vpScale;

				vp.enabled = false;
			}
		}

		private void CreateCompositeTrigger() {
			if (trigger == null) {
				// A CompositeMagicTrigger handles player passing through portals
				trigger = gameObject.GetOrAddComponent<CompositeMagicTrigger>();
				TriggerCondition positionCondition = new TriggerCondition {
					triggerCondition = TriggerConditionType.PlayerInDirectionFromPoint,
					useLocalCoordinates = true,
					targetDirection = Vector3.forward,
					targetPosition = Vector3.zero,
					triggerThreshold = 0.01f
				};
				TriggerCondition movementCondition = new TriggerCondition {
					triggerCondition = TriggerConditionType.PlayerMovingDirection,
					useLocalCoordinates = true,
					targetDirection = Vector3.forward,
					triggerThreshold = 0.01f
				};
				trigger.triggerConditions = new List<TriggerCondition>() {
					positionCondition,
					movementCondition
				};
			}
		}

		private void InitializeCompositeTrigger() {
			trigger.colliders = colliders;
			trigger.OnMagicTriggerStayOneTime += () => {
				playerCameraFollow.SetLerpSpeed(CameraFollow.desiredLerpSpeed);
				if (!teleportingPlayer) {
					StartCoroutine(TeleportPlayer(Player.instance.transform));
				}
			};
		}

		void CreateRenderTexture(int width, int height) {
			// Not sure why but it seems sometimes the Portals don't get OnDisable called when scene unloaded
			if (this == null) {
				OnDisable();
				return;
			}
			debug.Log($"Creating render textures for new resolution {width}x{height}");
			if (internalRenderTexturesCopy != null && (internalRenderTexturesCopy.mainTexture != null || internalRenderTexturesCopy.depthNormalsTexture != null)) {
				internalRenderTexturesCopy.Release();
			}
			internalRenderTexturesCopy = RecursiveTextures.CreateTextures(ID);
			SetPropertiesOnMaterial();
		}

		public void SetPropertiesOnMaterial() {
			void SetTexturesForRenderers(SuperspectiveRenderer[] portalRenderers) {
				foreach (var r in portalRenderers) {
					r.SetTexture("_MainTex", internalRenderTexturesCopy.mainTexture);
					r.SetTexture("_DepthNormals", internalRenderTexturesCopy.depthNormalsTexture);
				}
			}

			SetTexturesForRenderers(renderers);
			SetTexturesForRenderers(volumetricPortals);
		}

		protected override void Start() {
			base.Start();

			if (dimensionObject != null) {
				dimensionObject.SetStartingLayersFromCurrentLayers();
				dimensionObject.OnStateChangeSimple += SetPropertiesOnMaterial;
				dimensionObject.ignorePartiallyVisibleLayerChanges = true;
			}
			
			playerCamera = SuperspectiveScreen.instance.playerCamera.transform;
			playerCameraFollow = playerCamera.GetComponent<CameraFollow>();

			CreateRenderTexture(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight);
			SuperspectiveScreen.instance.OnScreenResolutionChanged += CreateRenderTexture;

			if (pauseRenderingOnly || pauseRenderingAndLogic) {
				DefaultMaterial();
			}
			else {
				PortalMaterial();
			}
		}

		protected override void Init() {
			base.Init();
			
			SetPropertiesOnMaterial();
		}

		bool test = false;
		void FixedUpdate() {
			bool playerIsCloseToPortal = Vector3.Distance(Player.instance.transform.position, ClosestPoint(Player.instance.transform.position, true)) < 0.99f;
			if (playerIsCloseToPortal && !test) {
				test = true;
				//Debug.Break();
			}
			else if (!playerIsCloseToPortal) {
				test = false;
			}

			// If the player moves to the backside of a double-sided portal, rotate the portals to match
			if (doubleSidedPortals && portalIsEnabled && !playerRemainsInPortal && !otherPortal.playerRemainsInPortal) {
				Vector3 portalToPlayer = playerCamera.position - transform.position;
				Vector3 otherPortalToPlayer = playerCamera.position - otherPortal.transform.position;
				if (portalToPlayer.magnitude < otherPortalToPlayer.magnitude) {
					bool playerIsOnOtherSide = Vector3.Dot(-PortalNormal(), portalToPlayer) <
					                           Vector3.Dot(PortalNormal(), portalToPlayer);
					if (playerIsOnOtherSide) {
						transform.Rotate(transform.up, 180);
						otherPortal.transform.Rotate(otherPortal.transform.up, 180);
					}
				}
			}
		}

		void OnEnable() {
			StartCoroutine(AddPortalCoroutine());
		}

		protected virtual void OnDisable() {
			SuperspectiveScreen.instance.OnScreenResolutionChanged -= CreateRenderTexture;
			PortalManager.instance.RemovePortal(channel, this);
		}

		public void OnTriggerEnter(Collider other) {
			if (other.TaggedAsPlayer()) return;
			
			PortalableObject portalableObj = other.gameObject.GetComponent<PortalableObject>();
			if (portalableObj != null) {
				objectsInPortal.Add(portalableObj);
				portalableObj.sittingInPortal = this;
			}
		}

		public void OnTriggerExit(Collider other) {
			if (other.TaggedAsPlayer()) return;
			
			PortalableObject portalableObj = other.gameObject.GetComponent<PortalableObject>();
			if (portalableObj != null) {
				if (objectsInPortal.Contains(portalableObj)) {
					objectsInPortal.Remove(portalableObj);
					portalableObj.sittingInPortal = null;
				}
			}
		}

		public void OnTriggerStay(Collider other) {
			if (!portalIsEnabled || other.isTrigger) return;

			// Player teleports are handled through a CompositeMagicTrigger to make it easier to ensure they are
			// in the right position and moving the correct direction before triggering teleport
			if (!other.TaggedAsPlayer()) {
				Vector3 closestPoint = ClosestPoint(other.transform.position, true, true);
				bool objectShouldBeTeleported = Mathf.Sign(Vector3.Dot(PortalNormal(), (other.transform.position - closestPoint).normalized)) > 0;
				PortalableObject portalableObj = other.gameObject.GetComponent<PortalableObject>();

				if (!objectShouldBeTeleported) {
					if (portalableObj != null) {
						portalableObj.sittingInPortal = this;
					}

					return;
				}
				
				if (portalableObj != null && objectsInPortal.Contains(portalableObj)) {
					TeleportObject(portalableObj);

					// Swap state to the other portal
					objectsInPortal.Remove(portalableObj);
					otherPortal.objectsInPortal.Add(portalableObj);
					portalableObj.sittingInPortal = otherPortal;
				}
			}
		}

		// Called before render process begins, either enable or disable the volumetric portals for this frame
		void LateUpdate() {
			SetEdgeDetectionColorProperties();
			//debug.Log(volumetricPortalsShouldBeEnabled);
			if (volumetricPortalsShouldBeEnabled) {
				EnableVolumetricPortal();
				
				// Experimental code for moving around a smaller volumetric portal that tracks the player:
				// for (int i = 0; i < volumetricPortals.Length; i++) {
				// 	var volumetricPortal = volumetricPortals[i];
				// 	var portal = colliders[i];
				// 	Transform vpTransform = volumetricPortal.transform;
				// 	Vector3 camPos = playerCamera.position;
				//
				// 	Vector3 vpSize = new Vector3(.5f, .5f, .5f);
				// 	Bounds portalBounds = portal.bounds;
				// 	portalBounds.size -= vpSize;
				//
				// 	Vector3 targetPos = portalBounds.ClosestPoint(camPos);
				// 	vpTransform.position = targetPos;
				// }
			}
			else {
				DisableVolumetricPortal();
			}
		}
		#endregion

		#region Public Interface
		public void EnablePortal(Portal other) {
			otherPortal = other;
			CreatePortalTeleporter();
		}

		public void DisablePortal() {
			otherPortal = null;
		}

		public bool IsVolumetricPortalEnabled() {
			return volumetricPortals.Any(vp => vp.enabled);
		}

		public void SetTexture(RenderTexture tex) {
			if (pauseRenderingOnly || pauseRenderingAndLogic) {
				debug.LogWarning($"Attempting to set MainTexture for disabled portal: {gameObject.name}");
				return;
			}

			PortalMaterial();

			Graphics.CopyTexture(tex, internalRenderTexturesCopy.mainTexture);
			SetPropertiesOnMaterial();
		}

		public void SetDepthNormalsTexture(RenderTexture tex) {
			if (pauseRenderingOnly || pauseRenderingAndLogic) {
				debug.LogWarning($"Attempting to set DepthNormalsTexture for disabled portal: {gameObject.name}");
				return;
			}

			PortalMaterial();

			Graphics.CopyTexture(tex, internalRenderTexturesCopy.depthNormalsTexture);
			SetPropertiesOnMaterial();
		}

		public void PortalMaterial() {
			if (!renderers[0].r.sharedMaterial.name.Contains(portalMaterial.name)) {
				foreach (var r in renderers) {
					r.SetMaterial(portalMaterial);
				}

				foreach (var vp in volumetricPortals) {
					vp.SetMaterial(portalMaterial);
				}
			}
		}

		public void DefaultMaterial() {
			if (!renderers[0].r.sharedMaterial.name.Contains(fallbackMaterial.name)) {
				foreach (var r in renderers) {
					r.SetMaterial(fallbackMaterial);
				}

				foreach (var vp in volumetricPortals) {
					vp.SetMaterial(fallbackMaterial);
				}
			}
		}

		public bool IsVisibleFrom(Camera cam) {
			return renderers.Any(r => r.r.IsVisibleFrom(cam)) || volumetricPortals.Any(vp => vp.r.IsVisibleFrom(cam));
		}

		public Vector3 PortalNormal() {
			if (renderers == null || renderers.Length == 0) {
				return transform.forward;
			}
			else {
				return renderers[0].transform.forward;
			}
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
				if (useInfinitelyThinBounds) {
					// Treat the collider bounds as being infinitely thin in the Z direction (portal normal direction)
					Bounds b = c.bounds;
					Vector3 effectiveNormal = PortalNormal();
					effectiveNormal.x = Mathf.Abs(effectiveNormal.x);
					effectiveNormal.y = Mathf.Abs(effectiveNormal.y);
					effectiveNormal.z = Mathf.Abs(effectiveNormal.z);
					Vector3Int scaleFactor = Vector3Int.RoundToInt(Vector3.one - effectiveNormal);
					b.size = Vector3.Scale(b.size, scaleFactor);
					thisClosestPoint = b.ClosestPoint(point);
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

		#region Portal Teleporter
		void CreatePortalTeleporter() {
			foreach (var c in colliders) {
				c.isTrigger = true;
			}
		}

		public void TeleportObject(PortalableObject portalableObject, bool transformVelocity = true) {
			portalableObject.BeforeObjectTeleported?.Invoke(this);

			TransformObject(portalableObject.transform, transformVelocity);

			portalableObject.OnObjectTeleported?.Invoke(this);
		}

		/// <summary>
		/// Does the work of TeleportObject without invoking teleport events
		/// </summary>
		/// <param name="objToTransform"></param>
		/// <param name="transformVelocity"></param>
		public void TransformObject(Transform objToTransform, bool transformVelocity = true) {
			Rigidbody objRigidbody = objToTransform.GetComponent<Rigidbody>();
			// Position & Rotation
			if (objRigidbody == null) {
				objRigidbody.MovePosition(TransformPoint(objToTransform.position));
				objRigidbody.MoveRotation(TransformRotation(objToTransform.rotation));
			}
			else {
				objToTransform.position = TransformPoint(objToTransform.position);
				objToTransform.rotation = TransformRotation(objToTransform.rotation);
			}

			// Velocity?
			if (transformVelocity && objRigidbody != null) {
				objRigidbody.velocity = TransformDirection(objRigidbody.velocity);
			}
		}

		public virtual Vector3 TransformPoint(Vector3 point) {
			Vector3 relativeObjPos = transform.InverseTransformPoint(point);
			relativeObjPos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeObjPos;
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

		/// <summary>
		/// Frame 1: Teleport player and disable this portal's volumetric portal while enabling the otherPortal's volumetric portal
		/// Frame 2: Do nothing (but ensure that this is not called twice)
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		IEnumerator TeleportPlayer(Transform player) {
			teleportingPlayer = true;

			//if (DEBUG) Debug.Break();
			TriggerEventsBeforeTeleport(player.GetComponent<Collider>());

			// Position
			player.position = TransformPoint(player.position);

			// Rotation
			player.rotation = TransformRotation(player.rotation);

			// Velocity
			Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
			playerRigidbody.velocity = TransformDirection(playerRigidbody.velocity);

			Physics.gravity = Physics.gravity.magnitude * -player.up;

			if (changeCameraEdgeDetection) {
				SwapEdgeDetectionColors();
			}

			if (changeActiveSceneOnTeleport) {
				LevelManager.instance.SwitchActiveScene(otherPortal.gameObject.scene.name);
			}

			// If the out portal is also a PillarDimensionObject, update the active pillar's curDimension to match the out portal's Dimension
			if (otherPortal.dimensionObject != null && otherPortal.dimensionObject is PillarDimensionObject pillarDimensionObject) {
				DimensionPillar activePillar = pillarDimensionObject.activePillar;
				if (activePillar != null) {
					activePillar.curDimension = pillarDimensionObject.Dimension;
					activePillar.dimensionWall.UpdateStateForCamera(SuperspectiveScreen.instance.playerCamera);
				}
			}

			TriggerEventsAfterTeleport(player.GetComponent<Collider>());
			DisableVolumetricPortal();
			otherPortal.EnableVolumetricPortal();
			otherPortal.pauseRenderingOnly = false;
			yield return null;
			// Sometimes teleporting leaves the hasTriggeredOnStay state as true so we explicitly reset state here
			trigger.ResetHasTriggeredOnStayState();
			
			teleportingPlayer = false;
		}

		void TriggerEventsBeforeTeleport(Collider objBeingTeleported) {
			BeforePortalTeleport?.Invoke(this, objBeingTeleported);
			BeforeAnyPortalTeleport?.Invoke(this, objBeingTeleported);
			BeforePortalTeleportSimple?.Invoke(objBeingTeleported);
			BeforeAnyPortalTeleportSimple?.Invoke(objBeingTeleported);

			if (objBeingTeleported.TaggedAsPlayer()) {
				BeforePortalTeleportPlayerSimple?.Invoke();
			}
		}

		void TriggerEventsAfterTeleport(Collider objBeingTeleported) {
			OnPortalTeleport?.Invoke(this, objBeingTeleported);
			OnAnyPortalTeleport?.Invoke(this, objBeingTeleported);
			OnPortalTeleportSimple?.Invoke(objBeingTeleported);
			OnAnyPortalTeleportSimple?.Invoke(objBeingTeleported);
			onPortalTeleport?.Invoke();
			
			if (objBeingTeleported.TaggedAsPlayer()) {
				OnPortalTeleportPlayerSimple?.Invoke();
			}
		}
		#endregion

		#region Volumetric Portal

		void EnableVolumetricPortal() {
			bool anyVolumetricPortalIsDisabled = volumetricPortals.Any(vp => !vp.enabled);
			if (anyVolumetricPortalIsDisabled) {
				debug.Log("Enabling Volumetric Portal(s) for " + gameObject.name);
				foreach (var vp in volumetricPortals) {
					if (pauseRenderingAndLogic) continue;
					else if (pauseRenderingOnly) vp.enabled = true;
					else {
						vp.SetMaterial(portalMaterial);
						vp.enabled = true;
					}
				}
			}
		}

		void DisableVolumetricPortal() {
			bool anyVolumetricPortalIsEnabled = volumetricPortals.Any(vp => vp.enabled);
			if (anyVolumetricPortalIsEnabled) {
				debug.Log("Disabling Volumetric Portal(s) for " + gameObject.name);

				foreach (var vp in volumetricPortals) {
					vp.enabled = false;
				}
			}
		}
		#endregion

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

			PortalManager.instance.AddPortal(channel, this);
		}

		// Allocate once to save GC every frame
		readonly float[] floatGradientBuffer = new float[BladeEdgeDetection.GradientArraySize];
		readonly Color[] colorGradientBuffer = new Color[BladeEdgeDetection.GradientArraySize];

		private BladeEdgeDetection edgeDetection => MaskBufferRenderTextures.instance.edgeDetection;
		BladeEdgeDetection.EdgeColorMode EdgeColorMode => changeCameraEdgeDetection ? edgeColorMode : edgeDetection.edgeColorMode;
		Color EdgeColor => changeCameraEdgeDetection ? edgeColor : edgeDetection.edgeColor;
		Gradient EdgeColorGradient => changeCameraEdgeDetection ? edgeColorGradient : edgeDetection.edgeColorGradient;
		
		void SetEdgeDetectionColorProperties() {
			portalMaterial.SetInt(BladeEdgeDetection.ColorModeID, (int)EdgeColorMode);
			switch (EdgeColorMode) {
				case BladeEdgeDetection.EdgeColorMode.SimpleColor:
					portalMaterial.SetColor(BladeEdgeDetection.EdgeColorID, EdgeColor);
					break;
				case BladeEdgeDetection.EdgeColorMode.Gradient:
					SetEdgeColorGradient();
					break;
				case BladeEdgeDetection.EdgeColorMode.ColorRampTexture:
					portalMaterial.SetTexture(BladeEdgeDetection.GradientTextureID, edgeColorGradientTexture);
					break;
			}
		}
		
		/// <summary>
		/// Sets the _GradientKeyTimes and _EdgeColorGradient float and Color arrays, respectively, in the BladeEdgeDetectionShader
		/// Populates _GradientKeyTimes with the times of each colorKey in edgeColorGradient (as well as a 0 as the first key and a series of 1s to fill out the array at the end)
		/// Populates _EdgeColorGradient with the colors of each colorKey in edgeColorGradient (as well as values for the times filled in as described above)
		/// </summary>
		void SetEdgeColorGradient() {
			Color startColor = EdgeColorGradient.Evaluate(0);
			Color endColor = EdgeColorGradient.Evaluate(1);
			float startAlpha = startColor.a;
			float endAlpha = endColor.a;
	
			portalMaterial.SetFloatArray(BladeEdgeDetection.GradientKeyTimesID, GetGradientFloatValues(0f, EdgeColorGradient.colorKeys.Select(x => x.time), 1f));
			portalMaterial.SetColorArray(BladeEdgeDetection.EdgeColorGradientID, GetGradientColorValues(startColor, EdgeColorGradient.colorKeys.Select(x => x.color), endColor));
			portalMaterial.SetFloatArray(BladeEdgeDetection.GradientAlphaKeyTimesID, GetGradientFloatValues(0f, EdgeColorGradient.alphaKeys.Select(x => x.time), 1f));
			portalMaterial.SetFloatArray(BladeEdgeDetection.AlphaGradientID, GetGradientFloatValues(startAlpha, EdgeColorGradient.alphaKeys.Select(x => x.alpha), endAlpha));
	
			portalMaterial.SetInt(BladeEdgeDetection.GradientModeID, EdgeColorGradient.mode == GradientMode.Blend ? 0 : 1);
	
			SetFrustumCornersVector();
		}
	
		void SetFrustumCornersVector() {
			portalMaterial.SetVectorArray(BladeEdgeDetection.FrustumCorners, edgeDetection.frustumCornersOrdered);
		}
	
		// Actually just populates the float buffer with the values provided, then returns a reference to the float buffer
		float[] GetGradientFloatValues(float startValue, IEnumerable<float> middleValues, float endValue) {
			float[] middleValuesArray = middleValues.ToArray();
			floatGradientBuffer[0] = startValue;
			for (int i = 1; i < middleValuesArray.Length + 1; i++) {
				floatGradientBuffer[i] = middleValuesArray[i - 1];
			}
			for (int j = middleValuesArray.Length + 1; j < BladeEdgeDetection.GradientArraySize; j++) {
				floatGradientBuffer[j] = endValue;
			}
			return floatGradientBuffer;
		}
	
		// Actually just populates the color buffer with the values provided, then returns a reference to the color buffer
		Color[] GetGradientColorValues(Color startValue, IEnumerable<Color> middleValues, Color endValue) {
			Color[] middleValuesArray = middleValues.ToArray();
			colorGradientBuffer[0] = startValue;
			for (int i = 1; i < middleValuesArray.Length + 1; i++) {
				colorGradientBuffer[i] = middleValuesArray[i - 1];
			}
			for (int j = middleValuesArray.Length + 1; j < BladeEdgeDetection.GradientArraySize; j++) {
				colorGradientBuffer[j] = endValue;
			}
			return colorGradientBuffer;
		}

		void SwapEdgeDetectionColors() {
			BladeEdgeDetection playerED = SuperspectiveScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();

			EDColors tempEDColors = new EDColors {
				edgeColorMode = playerED.edgeColorMode,
				edgeColor = playerED.edgeColor,
				edgeColorGradient = playerED.edgeColorGradient,
				edgeColorGradientTexture = playerED.edgeColorGradientTexture
			};

			CopyEdgeColors(from: this, to: playerED);

			otherPortal.changeCameraEdgeDetection = true;
			CopyEdgeColors(from: tempEDColors, to: otherPortal);
		}

		// Don't talk to me about this shit:
		public static void CopyEdgeColors(BladeEdgeDetection from, BladeEdgeDetection to) {
			to.edgeColorMode = from.edgeColorMode;
			to.edgeColor = from.edgeColor;
			to.edgeColorGradient = from.edgeColorGradient;
			to.edgeColorGradientTexture = from.edgeColorGradientTexture;
		}

		public static void CopyEdgeColors(BladeEdgeDetection from, Portal to) {
			to.edgeColorMode = from.edgeColorMode;
			to.edgeColor = from.edgeColor;
			to.edgeColorGradient = from.edgeColorGradient;
			to.edgeColorGradientTexture = from.edgeColorGradientTexture;
		}

		public static void CopyEdgeColors(BladeEdgeDetection from, ref EDColors to) {
			to.edgeColorMode = from.edgeColorMode;
			to.edgeColor = from.edgeColor;
			to.edgeColorGradient = from.edgeColorGradient;
			to.edgeColorGradientTexture = from.edgeColorGradientTexture;
		}

		public static void CopyEdgeColors(Portal from, BladeEdgeDetection to) {
			to.edgeColorMode = from.edgeColorMode;
			to.edgeColor = from.edgeColor;
			to.edgeColorGradient = from.edgeColorGradient;
			to.edgeColorGradientTexture = from.edgeColorGradientTexture;
		}

		public static void CopyEdgeColors(Portal from, Portal to) {
			to.edgeColorMode = from.edgeColorMode;
			to.edgeColor = from.edgeColor;
			to.edgeColorGradient = from.edgeColorGradient;
			to.edgeColorGradientTexture = from.edgeColorGradientTexture;
		}

		public static void CopyEdgeColors(Portal from, ref EDColors to) {
			to.edgeColorMode = from.edgeColorMode;
			to.edgeColor = from.edgeColor;
			to.edgeColorGradient = from.edgeColorGradient;
			to.edgeColorGradientTexture = from.edgeColorGradientTexture;
		}

		public static void CopyEdgeColors(EDColors from, BladeEdgeDetection to) {
			to.edgeColorMode = from.edgeColorMode;
			to.edgeColor = from.edgeColor;
			to.edgeColorGradient = from.edgeColorGradient;
			to.edgeColorGradientTexture = from.edgeColorGradientTexture;
		}

		public static void CopyEdgeColors(EDColors from, Portal to) {
			to.edgeColorMode = from.edgeColorMode;
			to.edgeColor = from.edgeColor;
			to.edgeColorGradient = from.edgeColorGradient;
			to.edgeColorGradientTexture = from.edgeColorGradientTexture;
		}

		public static void CopyEdgeColors(EDColors from, ref EDColors to) {
			to.edgeColorMode = from.edgeColorMode;
			to.edgeColor = from.edgeColor;
			to.edgeColorGradient = from.edgeColorGradient;
			to.edgeColorGradientTexture = from.edgeColorGradientTexture;
		}
		#endregion

		#region Saving
		
		[Serializable]
		public class PortalSave : SerializableSaveObject<Portal> {
			string channel;
			bool changeActiveSceneOnTeleport;
			bool changeCameraEdgeDetection;
			int edgeColorMode;
			SerializableColor edgeColor;
			SerializableGradient edgeColorGradient;
			bool renderRecursivePortals;
			bool compositePortal;
			bool teleportingPlayer = false;

			bool pauseRenderingOnly = false;
			bool pauseRenderingAndLogic = false;

			public PortalSave(Portal portal) : base(portal) {
				this.channel = portal.channel;
				this.changeActiveSceneOnTeleport = portal.changeActiveSceneOnTeleport;
				this.changeCameraEdgeDetection = portal.changeCameraEdgeDetection;
				this.edgeColorMode = (int)portal.edgeColorMode;
				this.edgeColor = portal.edgeColor;
				this.edgeColorGradient = portal.edgeColorGradient;
				this.renderRecursivePortals = portal.renderRecursivePortals;
				this.compositePortal = portal.compositePortal;
				this.teleportingPlayer = portal.teleportingPlayer;
				this.pauseRenderingOnly = portal.pauseRenderingOnly;
				this.pauseRenderingAndLogic = portal.pauseRenderingAndLogic;
			}

			public override void LoadSave(Portal portal) {
				portal.channel = this.channel;
				portal.changeActiveSceneOnTeleport = this.changeActiveSceneOnTeleport;
				portal.changeCameraEdgeDetection = this.changeCameraEdgeDetection;
				portal.edgeColorMode = (BladeEdgeDetection.EdgeColorMode)this.edgeColorMode;
				portal.edgeColor = this.edgeColor;
				portal.edgeColorGradient = this.edgeColorGradient;
				portal.renderRecursivePortals = this.renderRecursivePortals;
				portal.compositePortal = this.compositePortal;
				portal.teleportingPlayer = this.teleportingPlayer;
				portal.pauseRenderingOnly = this.pauseRenderingOnly;
				portal.pauseRenderingAndLogic = this.pauseRenderingAndLogic;
			}
		}
		#endregion
	}
}
