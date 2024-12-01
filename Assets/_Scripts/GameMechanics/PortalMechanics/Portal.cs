using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using SuperspectiveUtils;
using System.Runtime.CompilerServices;
using UnityEditor;
using System.Linq;
using System.Numerics;
using System.Runtime.Remoting.Messaging;
using DimensionObjectMechanics;
using GrowShrink;
using LevelManagement;
using MagicTriggerMechanics;
using Saving;
using SerializableClasses;
using UnityEngine.Events;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Plane = UnityEngine.Plane;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace PortalMechanics {
	/// <summary>
	/// RecursiveTextures contains the mainTexture (what the camera sees)
	/// as well as the depthNormalsTexture (used for image effects)
	/// </summary>
	[Serializable]
	public class RecursiveTextures {
		public string portalName;
		public RenderTexture mainTexture;
		public RenderTexture depthNormalsTexture;

		public static RecursiveTextures CreateTextures(string name, string associatedPortalName) {
			int width = SuperspectiveScreen.instance.currentPortalWidth;
			int height = SuperspectiveScreen.instance.currentPortalHeight;
			
			RecursiveTextures recursiveTextures = new RecursiveTextures {
				mainTexture = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR),
				depthNormalsTexture = new RenderTexture(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight, 24, Portal.DepthNormalsTextureFormat, RenderTextureReadWrite.Linear)
			};
			recursiveTextures.mainTexture.name = $"{name}_MainTex";
			recursiveTextures.depthNormalsTexture.name = $"{name}_DepthNormals";
			recursiveTextures.portalName = associatedPortalName;
			return recursiveTextures;
		}

		public void Release() {
			if (mainTexture != null) {
				mainTexture.Release();
				GameObject.Destroy(mainTexture);
			}

			if (depthNormalsTexture != null) {
				depthNormalsTexture.Release();
				GameObject.Destroy(depthNormalsTexture);
			}
		}
	}
	
	[RequireComponent(typeof(UniqueId))]
	public class Portal : SaveableObject<Portal, Portal.PortalSave> {
		private const int FRAMES_TO_WAIT_BEFORE_DISABLING_VP = 10;
		private int consecutiveFramesVPShouldBeDisabled = 0;
		private const int GLOBAL_FRAMES_TO_WAIT_AFTER_TELEPORT = 5;
		private static int lastTeleportedFrame = 0;
		
		public static bool allowPortalRendering =
#if UNITY_EDITOR
			// Disable portal rendering in editor to increase performance of play mode
			false;
#else
		true;
#endif
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

		[SerializeField]
		private bool renderRecursivePortals = false;
		public bool RenderRecursivePortals =>
#if UNITY_EDITOR
			// Never render recursive portals in editor, the slowdown is too much
			false;
#else
			renderRecursivePortals;
#endif
		[Tooltip("Enable composite portals if there are multiple renderers that make up the portal surface. Ensure that these renderers are the only children of the portal gameObject.")]
		public bool compositePortal = false;

		[Tooltip("Double-sided portals will rotate 180 degrees (along with otherPortal) if the player moves around to the backside")]
		public bool doubleSidedPortals = false;
		private bool isFlipped = false;
		private Quaternion startRotation, flippedRotation;
		public void FlipPortal() {
			debug.Log($"Flipping from {(isFlipped ? "flipped" : "not flipped")} to {(!isFlipped ? "flipped" : "not flipped")}\nFrameCount: {Time.frameCount}, LastTeleportedFrame: {lastTeleportedFrame}, ShouldTeleportPlayer: {ShouldTeleportPlayer}");
			isFlipped = !isFlipped;
			transform.rotation = isFlipped ? flippedRotation : startRotation;
		}

		public bool skipIsVisibleCheck = false; // Useful for very large portals where the isVisible check doesn't work well
		
		[OnValueChanged(nameof(SetScaleFactor))]
		public bool changeScale = false;
		[ShowIf(nameof(changeScale))]
		[OnValueChanged(nameof(SetScaleFactor))]
		[Tooltip("Multiply the player size by this value when passing through this Portal (and inverse for the other Portal)")]
		[Range(1f/64f, 64f)]
		[SerializeField]
		private float scaleFactor = 1;
		public float ScaleFactor => changeScale ? scaleFactor : 1f;

		[Tooltip("Largest size GrowShrinkObject that is allowed to pass through this portal")]
		public float maxScaleAllowed = 6f;
		private float MaxScaleAllowed => otherPortal != null ? Mathf.Max(maxScaleAllowed, otherPortal.maxScaleAllowed) : maxScaleAllowed;

		private readonly string VOLUMETRIC_PORTAL_NAME = "Volumetric Portal";
		[SerializeField]
		SuperspectiveRenderer[] volumetricPortals;
		private const float volumetricPortalEnableDistance = 5f;

		[SerializeField]
		private float volumetricPortalThickness = 1f;
		public float VolumetricPortalThickness => volumetricPortalThickness * transform.localScale.z;

		public static bool forceVolumetricPortalsOn = false;
		private bool VolumetricPortalsShouldBeEnabled => forceVolumetricPortalsOn || PlayerRemainsInPortal;

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

		private bool IsInvisible {
			get {
				if (dimensionObject != null) {
					return dimensionObject.EffectiveVisibilityState == VisibilityState.Invisible;
				}

				return false;
			}
		}
		Material portalMaterial;
		
		public Material fallbackMaterial;
		public SuperspectiveRenderer[] renderers;
		// Colliders are the infinitely thin planes on the Portal layer that interact with raycasts
		public Collider[] colliders;
		// Trigger colliders are the trigger zones that the CompositeMagicTriggers operate within to teleport the player/objects
		public Collider[] triggerColliders;
		Transform playerCamera;
		CameraFollow playerCameraFollow;
		bool teleportingPlayer = false;

		[HorizontalLine]

		public Portal otherPortal;
		public HashSet<PortalableObject> objectsInPortal = new HashSet<PortalableObject>();

		[SerializeField]
		[ShowIf("DEBUG")]
		RecursiveTextures internalRenderTexturesCopy;

		public bool disableColliderWhilePaused = false;
		public bool pauseRenderingWhenNotInActiveScene = false;
		private bool ThisPortalIsInActiveScene => LevelManager.instance.activeSceneName == gameObject.scene.name;
		private bool IsInActiveScene => ThisPortalIsInActiveScene || otherPortal.ThisPortalIsInActiveScene;
		public bool pauseRendering = false;
		public bool pauseLogic = false;

		// I can't seem to set pauseLogic from a UnityEvent so this is a workaround
		public void SetPauseLogic(bool toValue) {
			pauseRendering = toValue;
			pauseLogic = toValue;
			SetMaterialsToEffectiveMaterial();
		}
		
		private Material EffectiveMaterial {
			get => PortalRenderingIsEnabled ? portalMaterial : fallbackMaterial;
			set {
				if (PortalRenderingIsEnabled) {
					portalMaterial = value;
				}
				else {
					fallbackMaterial = value;
				}
			}
		}

		// ReSharper disable once ConditionIsAlwaysTrueOrFalse
		public bool PortalRenderingIsEnabled => otherPortal != null && !pauseRendering && gameObject.activeSelf && allowPortalRendering && (!pauseRenderingWhenNotInActiveScene || IsInActiveScene);
		public bool PortalLogicIsEnabled => otherPortal != null && !pauseLogic && gameObject.activeSelf;

		public bool PlayerRemainsInPortal => PlayerIsInThisPortal || (otherPortal != null && otherPortal.PlayerIsInThisPortal);
		private bool PlayerIsInThisPortal => trigger.playerIsInTriggerZone;

		private bool ShouldTeleportPlayer => trigger.AllConditionsSatisfied;

		// May or may not exist on a Portal, affects what the PortalMaterial is
		public DimensionObject dimensionObject;
		// May or may not exist on a Portal, affects PortalableObjects that are also PillarDimensionObjects by setting their dimension to the outPortal's dimension
		public PillarDimensionObject pillarDimensionObject;
		public CompositeMagicTrigger trigger;

		private const float PORTAL_THICKNESS = 1.55f;
		private float PortalThickness => PORTAL_THICKNESS / (changeScale ? ScaleFactor : 1f);

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

		void SetScaleFactor() {
			if (changeScale) {
				// In Edit mode, update the other portal's scale factor to be the inverse of this one
				if (!Application.isPlaying) {
					Portal other = otherPortal;
#if UNITY_EDITOR
					other = GetOtherPortal(this);
#endif
					if (other != null) {
						other.changeScale = changeScale;
						other.scaleFactor = 1f / scaleFactor;
					}
					else {
						Debug.LogWarning("No other portal to change scale of");
					}
				}
			}
			else {
				scaleFactor = 1;
			}
		}

#region MonoBehaviour Methods

		protected override void OnDestroy() {
			base.OnDestroy();
			internalRenderTexturesCopy?.Release();
		}

		protected override void Awake() {
			base.Awake();
			
			dimensionObject = gameObject.FindDimensionObjectRecursively<DimensionObject>();
			pillarDimensionObject = dimensionObject as PillarDimensionObject;
			string shaderPath = "Shaders/Suberspective/SuberspectivePortal";
			portalMaterial = new Material(Resources.Load<Shader>(shaderPath));
			if (fallbackMaterial == null) {
				fallbackMaterial = Resources.Load<Material>("Materials/Invisible");
			}

			InitializeRenderers();
			gameObject.layer = SuperspectivePhysics.PortalLayer;
			foreach (var r in renderers) {
				r.gameObject.layer = SuperspectivePhysics.PortalLayer;
				r.SetSharedMaterial(PortalRenderingIsEnabled ? portalMaterial : fallbackMaterial);
				if (changeScale) {
					r.SetFloat("_PortalScaleFactor", scaleFactor);
				}
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

			triggerColliders = colliders.Select(c => {
				string triggerName = $"{c.name} Trigger";
				GameObject triggerColliderGO = new GameObject(triggerName);
				triggerColliderGO.transform.SetParent(c.transform, false);
				Collider triggerCollider = null;
				switch (c) {
					case BoxCollider boxCollider:
						// Copy the collider's properties to the trigger collider
						triggerCollider = triggerColliderGO.PasteComponent(boxCollider);
						triggerCollider.isTrigger = true;

						boxCollider.size = boxCollider.size.WithZ(PortalThickness);
						break;
					case MeshCollider meshCollider:
						// Copy the collider's properties to the trigger collider
						triggerCollider = triggerColliderGO.PasteComponent(meshCollider);
						triggerCollider.isTrigger = true;
						
						meshCollider.convex = false;
						break;
					default:
						Debug.LogError("Unsupported collider type for portal trigger collider, only BoxCollider and MeshCollider are supported!");
						break;
				}

				triggerColliderGO.name = triggerName;

				return triggerCollider;
			}).Where(c => c != null).ToArray();

			colliders.ToList().ForEach(c => {
				string raycastBlockerName = $"{c.name} Raycast Blocker";
				GameObject raycastBlockerGO = new GameObject(raycastBlockerName);
				raycastBlockerGO.transform.SetParent(c.transform, false);
				Collider raycastBlocker = null;
				// Make the raycast blocker collider --infinitely thin-- to work with raycasts starting inside the portal
				switch (c) {
					case BoxCollider boxCollider:
						// Copy the collider's properties to the trigger collider
						raycastBlocker = raycastBlockerGO.PasteComponent(boxCollider);
						raycastBlocker.isTrigger = true;
						break;
					case MeshCollider meshCollider:
						// Copy the collider's properties to the trigger collider
						raycastBlocker = raycastBlockerGO.PasteComponent(meshCollider);
						raycastBlocker.isTrigger = true;

						meshCollider.convex = true;
						break;
					default:
						Debug.LogError("Unsupported collider type for portal trigger collider, only BoxCollider and MeshCollider are supported!");
						break;
				}
				raycastBlockerGO.transform.localScale = raycastBlockerGO.transform.localScale.WithZ(0f);
				// Move it just so slightly back to avoid player being slightly past the portal plane
				raycastBlockerGO.transform.position += IntoPortalVector * 0.1f;

				raycastBlockerGO.name = raycastBlockerName;
				raycastBlockerGO.layer = SuperspectivePhysics.PortalLayer;
			});
			
			CreateCompositeTrigger();
			InitializeCompositeTrigger();

			foreach (var c in triggerColliders) {
				c.gameObject.layer = SuperspectivePhysics.TriggerZoneLayer;
				if (c.gameObject != this.gameObject) {
					// PortalColliders handle non-player objects passing through portals
					c.gameObject.AddComponent<NonPlayerPortalTriggerZone>().portal = this;
				}
				
				// Give the trigger colliders some thickness so the player can't phase through them with lag
				if (c is BoxCollider boxCollider) {
					var size = boxCollider.size;
					boxCollider.size = size.WithZ(PortalThickness);
				}
			}

			InitializeVolumetricPortals();
			
			if (changeScale) {
				foreach (SuperspectiveRenderer vp in volumetricPortals) {
					vp.SetFloat("_PortalScaleFactor", scaleFactor);
				}
			}
		}

		private void InitializeRenderers() {
			if (!(renderers == null || renderers.Length == 0)) return;
			
			if (compositePortal) {
				renderers = GetComponentsInChildren<Renderer>()
					.Select(r => r.GetOrAddComponent<SuperspectiveRenderer>()).ToArray();
			}
			else {
				renderers = new SuperspectiveRenderer[] { GetComponents<Renderer>().Select(r => r.GetOrAddComponent<SuperspectiveRenderer>()).FirstOrDefault() };
			}
		}

		private void InitializeVolumetricPortals() {
			// Clean up extra Volumetric Portals
			foreach (var existingVolumetricPortal in transform.GetChildrenMatchingNameRecursively(VOLUMETRIC_PORTAL_NAME)) {
				if (volumetricPortals != null && volumetricPortals.ToList().Exists(vp => vp.transform == existingVolumetricPortal)) continue;
				
				DestroyImmediate(existingVolumetricPortal.gameObject);
			}

			volumetricPortals = volumetricPortals?.Where(vp => vp != null).ToArray();

			if (volumetricPortals != null && volumetricPortals.Length > 0) {
				foreach (var vp in volumetricPortals){
					vp.gameObject.layer = SuperspectivePhysics.VolumetricPortalLayer;
				}
				return;
			}

			List<SuperspectiveRenderer> volumetricPortalsList = new List<SuperspectiveRenderer>();
			foreach (SuperspectiveRenderer r in renderers) {
				try {
					SuperspectiveRenderer vp = GenerateExtrudedMesh(r.GetComponent<MeshFilter>(), VolumetricPortalThickness)
						.GetOrAddComponent<SuperspectiveRenderer>();

					vp.enabled = false;
					vp.SetSharedMaterial(r.r.sharedMaterial);
					vp.gameObject.layer = SuperspectivePhysics.VolumetricPortalLayer;
					volumetricPortalsList.Add(vp);
				}
				catch (Exception e) {
					Debug.LogError($"{ID} in scene {gameObject.scene.name} failed to build volumetric portal, error: {e.StackTrace}");
				}
			}

			volumetricPortals = volumetricPortalsList.ToArray();
		}

		private void CreateCompositeTrigger() {
			if (trigger == null) {
				debug.Log("No trigger set, getting or adding a CompositeMagicTrigger");
				// A CompositeMagicTrigger handles player passing through portals
				trigger = gameObject.GetOrAddComponent<CompositeMagicTrigger>();
				TriggerCondition positionCondition = new TriggerCondition {
					triggerCondition = TriggerConditionType.PlayerInDirectionFromPoint,
					useLocalCoordinates = true,
					targetDirection = Vector3.forward,
					targetPosition = Vector3.zero,
					triggerThreshold = 0f
				};
				TriggerCondition movementCondition = new TriggerCondition {
					triggerCondition = TriggerConditionType.PlayerMovingDirection,
					useLocalCoordinates = true,
					targetDirection = Vector3.forward,
					triggerThreshold = 0f
				};
				trigger.triggerConditions = new List<TriggerCondition>() {
					positionCondition,
					movementCondition
				};
			}
		}

		private void InitializeCompositeTrigger() {
			trigger.colliders = triggerColliders;
			trigger.OnMagicTriggerStayOneTime += () => {
				playerCameraFollow.SetLerpSpeed(CameraFollow.desiredLerpSpeed);
				if (!teleportingPlayer) {
					StartCoroutine(TeleportPlayer(Player.instance.transform));
				}
			};
		}

		private void CreateRenderTexture(int width, int height) {
			// Not sure why but it seems sometimes the Portals don't get OnDisable called when scene unloaded
			if (this == null) {
				OnDisable();
				return;
			}
			debug.Log($"Creating render textures for new resolution {width}x{height}");
			if (internalRenderTexturesCopy != null && (internalRenderTexturesCopy.mainTexture != null || internalRenderTexturesCopy.depthNormalsTexture != null)) {
				internalRenderTexturesCopy.Release();
			}
			internalRenderTexturesCopy = RecursiveTextures.CreateTextures(ID, $"{channel}: {name}");
			SetPropertiesOnMaterial();
		}

		private void SetPropertiesOnMaterial() {
			if (!PortalRenderingIsEnabled) return;
			
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
				dimensionObject.ignorePartiallyVisibleLayerChanges = true;
			}
			
			playerCamera = SuperspectiveScreen.instance.playerCamera.transform;
			playerCameraFollow = playerCamera.GetComponent<CameraFollow>();

			CreateRenderTexture(SuperspectiveScreen.currentWidth, SuperspectiveScreen.currentHeight);

			SetMaterialsToEffectiveMaterial();

			startRotation = transform.rotation;
			flippedRotation = Quaternion.AngleAxis(180f, Vector3.up) * startRotation;
		}

		protected override void Init() {
			base.Init();
			
			SuperspectiveScreen.instance.OnScreenResolutionChanged += CreateRenderTexture;
			SetPropertiesOnMaterial();
		}

		void FixedUpdate() {
			foreach (Collider c in colliders) {
				c.isTrigger = PortalLogicIsEnabled;
				if (disableColliderWhilePaused) {
					c.enabled = PortalLogicIsEnabled;
				}
			}

			// If the player moves to the backside of a double-sided portal, rotate the portals to match
			transform.rotation = !isFlipped ? flippedRotation : startRotation; // Pretend the Portal were already flipped
			bool wouldTeleportPlayersIfPortalWereFlipped = ShouldTeleportPlayer; // If the Portal were flipped, would we be teleporting the player?
			transform.rotation = isFlipped ? flippedRotation : startRotation; // Restore rotation
			if (doubleSidedPortals && PortalLogicIsEnabled && !PlayerRemainsInPortal && (Time.frameCount - lastTeleportedFrame > GLOBAL_FRAMES_TO_WAIT_AFTER_TELEPORT) && !wouldTeleportPlayersIfPortalWereFlipped) {
				Vector3 closestPoint = ClosestPoint(playerCamera.position, true, true);
				Vector3 closestPointOtherPortal = otherPortal.ClosestPoint(playerCamera.position, true, true);
				
				Vector3 portalToPlayer = playerCamera.position - closestPoint;
				Vector3 otherPortalToPlayer = playerCamera.position - closestPointOtherPortal;
				
				if (portalToPlayer.magnitude <= otherPortalToPlayer.magnitude) {
					Vector3 intoPortal = IntoPortalVector;
					Vector3 outOfPortal = -intoPortal;
					float outDot = Vector3.Dot(outOfPortal, portalToPlayer);
					float inDot = Vector3.Dot(intoPortal, portalToPlayer);
					bool playerIsOnOtherSide = outDot < inDot;
					if (playerIsOnOtherSide && !PlayerRemainsInPortal) {
						debug.LogWarning($"Out: {outDot}, In: {inDot}, portalToPlayer: {portalToPlayer:F3}");
						FlipPortal();
						if (transform != otherPortal.transform) {
							otherPortal.FlipPortal();
						}
					}
				}
			}
		}

		private void HandleMaterialChanged(Material newMaterial) {
			if (newMaterial.name.EndsWith(DimensionObjectManager.DIMENSION_OBJECT_SUFFIX)) {
				EffectiveMaterial = newMaterial;
			}
		}

		protected override void OnEnable() {
			base.OnEnable();
			StartCoroutine(AddPortalCoroutine());
			
			if (dimensionObject != null) {
				foreach (SuperspectiveRenderer r in renderers) {
					r.OnMaterialChanged += HandleMaterialChanged;
				}
				dimensionObject.OnStateChangeSimple += SetPropertiesOnMaterial;
			}
		}

		protected virtual void OnDisable() {
			if (!Application.isPlaying) return; // ???
			
			SuperspectiveScreen.instance.OnScreenResolutionChanged -= CreateRenderTexture;
			PortalManager.instance.RemovePortal(channel, this);
			
			if (dimensionObject != null) {
				foreach (SuperspectiveRenderer r in renderers) {
					r.OnMaterialChanged -= HandleMaterialChanged;
				}
				dimensionObject.OnStateChangeSimple -= SetPropertiesOnMaterial;
			}
		}

		public void OnPortalTriggerEnter(Collider other) {
			if (!PortalLogicIsEnabled || other.isTrigger) return;
			
			// Player teleports are handled through a CompositeMagicTrigger to make it easier to ensure they are
			// in the right position and moving the correct direction before triggering teleport
			if (other.TaggedAsPlayer()) return;
			
			// Don't deal with objects that are over the max scale allowed
			GrowShrinkObject growShrinkObject = other.gameObject.FindInParentsRecursively<GrowShrinkObject>();
			if (growShrinkObject != null && growShrinkObject.CurrentScale > MaxScaleAllowed) {
				return;
			}
			
			PortalableObject portalableObj = other.gameObject.GetComponent<PortalableObject>();
			if (portalableObj != null && (!portalableObj.IsInPortal || portalableObj.Portal != this)) {
				portalableObj.EnterPortal(this);
			}
		}

		public void OnPortalTriggerExit(Collider other) {
			if (!PortalLogicIsEnabled || other.isTrigger) return;
			
			// Player teleports are handled through a CompositeMagicTrigger to make it easier to ensure they are
			// in the right position and moving the correct direction before triggering teleport
			if (other.TaggedAsPlayer()) return;
			
			// Don't deal with objects that are over the max scale allowed
			GrowShrinkObject growShrinkObject = other.gameObject.FindInParentsRecursively<GrowShrinkObject>();
			if (growShrinkObject != null && growShrinkObject.CurrentScale > MaxScaleAllowed) {
				return;
			}
			
			PortalableObject portalableObj = other.gameObject.GetComponent<PortalableObject>();
			if (portalableObj != null && !portalableObj.teleportedThisFixedUpdate) {
				if (objectsInPortal.Contains(portalableObj)) {
					portalableObj.ExitPortal(this);
				}
			}
		}

		public void OnPortalTriggerStay(Collider other) {
			if (!PortalLogicIsEnabled || other.isTrigger) return;

			// Player teleports are handled through a CompositeMagicTrigger to make it easier to ensure they are
			// in the right position and moving the correct direction before triggering teleport
			if (other.TaggedAsPlayer()) return;
			
			// Don't teleport objects that are over the max scale allowed
			GrowShrinkObject growShrinkObject = other.gameObject.FindInParentsRecursively<GrowShrinkObject>();
			if (growShrinkObject != null && growShrinkObject.CurrentScale > MaxScaleAllowed) {
				return;
			}
			
			Vector3 closestPoint = ClosestPoint(other.transform.position, true, true);
			bool objectShouldBeTeleported = Mathf.Sign(Vector3.Dot(IntoPortalVector, (other.transform.position - closestPoint).normalized)) > 0;
			PortalableObject portalableObj = other.gameObject.GetComponent<PortalableObject>();

			if (!objectShouldBeTeleported) {
				if (portalableObj != null && portalableObj.IsInPortal && portalableObj.Portal != this) {
					portalableObj.EnterPortal(this);
				}

				return;
			}
			
			if (portalableObj != null && objectsInPortal.Contains(portalableObj)) {
				debug.Log($"{ID} teleporting object {portalableObj.ID}");
				TeleportObject(portalableObj);

				// Swap state to the other portal
				// The PortalableObject itself takes care of its own state by listening for a teleport event
				objectsInPortal.Remove(portalableObj);
				otherPortal.objectsInPortal.Add(portalableObj);
			}
		}

		// Called before render process begins, either enable or disable the volumetric portals for this frame
		void LateUpdate() {
			SetEdgeDetectionColorProperties();
			//debug.Log(volumetricPortalsShouldBeEnabled);
			if (VolumetricPortalsShouldBeEnabled) {
				EnableVolumetricPortal();
				
				consecutiveFramesVPShouldBeDisabled = 0;
			}
			else {
				// Replacing with delayed disabling of VP
				// DisableVolumetricPortal();
				if (consecutiveFramesVPShouldBeDisabled > FRAMES_TO_WAIT_BEFORE_DISABLING_VP) {
					DisableVolumetricPortal();
				}
				
				consecutiveFramesVPShouldBeDisabled++;
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

		public bool IsVolumetricPortalEnabled() {
			return volumetricPortals.Any(vp => vp.enabled && vp.gameObject.layer != SuperspectivePhysics.InvisibleLayer);
		}

		public void SetVolumetricHiddenForPortalRendering(bool hidden) {
			int targetLayer = hidden ? SuperspectivePhysics.InvisibleLayer : SuperspectivePhysics.VolumetricPortalLayer;
			foreach (SuperspectiveRenderer vp in volumetricPortals) {
				vp.gameObject.layer = targetLayer;
			}
		}

		public void SetTexture(RenderTexture tex) {
			if (!PortalRenderingIsEnabled) {
				debug.LogWarning($"Attempting to set MainTexture for disabled portal: {gameObject.FullPath()}");
				return;
			}
			
			if (internalRenderTexturesCopy.mainTexture == null) { 
				Debug.LogWarning($"Attempting to set MainTexture for portal w/ null mainTexture: {gameObject.FullPath()}");
				return;
			}

			SetMaterialsToEffectiveMaterial();

			Graphics.CopyTexture(tex, internalRenderTexturesCopy.mainTexture);
			SetPropertiesOnMaterial();
		}

		public void SetDepthNormalsTexture(RenderTexture tex) {
			if (!PortalRenderingIsEnabled) {
				debug.LogWarning($"Attempting to set DepthNormalsTexture for disabled portal: {gameObject.FullPath()}");
				return;
			}

			if (internalRenderTexturesCopy.depthNormalsTexture == null) { 
				Debug.LogWarning($"Attempting to set DepthNormalsTexture for portal w/ null depthNormalsTexture: {gameObject.FullPath()}");
				return;
			}

			SetMaterialsToEffectiveMaterial();
			
			Graphics.CopyTexture(tex, internalRenderTexturesCopy.depthNormalsTexture);
			SetPropertiesOnMaterial();
		}

		public void SetMaterialsToEffectiveMaterial() {
			if (!renderers[0].r.sharedMaterial.name.Contains(EffectiveMaterial.name)) {
				debug.LogWarning($"Setting portal material to {EffectiveMaterial.name}");
				foreach (var r in renderers) {
					r.SetSharedMaterial(EffectiveMaterial);
				}

				foreach (var vp in volumetricPortals) {
					vp.SetSharedMaterial(EffectiveMaterial);
				}
			}
		}

		public bool IsVisibleFrom(Camera cam) {
			if (skipIsVisibleCheck) {
				// Still don't render portals that are very far away
				Vector3 closestPoint = ClosestPoint(Player.instance.PlayerCam.transform.position, true, true);
				return Vector3.Distance(closestPoint, Player.instance.PlayerCam.transform.position) < Player.instance.PlayerCam.farClipPlane;
			}
			
			return renderers.Any(r => r.r.IsVisibleFrom(cam)) || volumetricPortals.Any(vp => vp.r.IsVisibleFrom(cam));
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
					Plane portalPlane = new Plane(IntoPortalVector, transform.position);
					thisClosestPoint = portalPlane.ClosestPointOnPlane(c.ClosestPoint(point));
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
			debug.Log($"Teleporting {portalableObject.FullPath()}");
			
			TriggerEventsBeforeTeleport(portalableObject.colliders[0]);

			TransformObject(portalableObject.transform, transformVelocity);
			
			TriggerEventsAfterTeleport(portalableObject.colliders[0]);
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

		/// <summary>
		/// Frame 1: Teleport player and disable this portal's volumetric portal while enabling the otherPortal's volumetric portal
		/// Frame 2: Do nothing (but ensure that this is not called twice)
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		IEnumerator TeleportPlayer(Transform player) {
			if (Time.frameCount - lastTeleportedFrame < GLOBAL_FRAMES_TO_WAIT_AFTER_TELEPORT) {
				debug.LogError("Can't teleport so quickly after last teleport!");
				yield break;
			}

			if (!PortalLogicIsEnabled) yield break;
			
			teleportingPlayer = true;

			//if (DEBUG) Debug.Break();
			TriggerEventsBeforeTeleport(player.GetComponent<Collider>());
			
			debug.Log("Teleporting player!");

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
				// TODO: Investigate crash that happens when switching scenes rapidly without the following check
				LevelManager.instance.SwitchActiveScene(otherPortal.gameObject.scene.name);
				// TODO: Can't reproduce the crash anymore, but if it starts happening, uncomment the following lines
				// if (LevelManager.instance.IsCurrentlySwitchingScenes) {
				// 	Debug.LogError($"Tried to switch scenes due to {ID} teleport but LevelManager is still loading!");
				// }
				// else {
				// 	LevelManager.instance.SwitchActiveScene(otherPortal.gameObject.scene.name);
				// }
			}

			// If the out portal is also a PillarDimensionObject, update the active pillar's curDimension to match the out portal's Dimension
			if (otherPortal.pillarDimensionObject) {
				DimensionPillar activePillar = otherPortal.pillarDimensionObject.activePillar;
				if (activePillar != null) {
					// Need to recalculate the camQuadrant of the PillarDimensionObject after teleporting the player
					var originalQuadrant = otherPortal.pillarDimensionObject.camQuadrant;
					otherPortal.pillarDimensionObject.DetermineQuadrantForPlayerCam();
					activePillar.curBaseDimension = otherPortal.pillarDimensionObject.GetPillarDimensionWhere(v => v == VisibilityState.Visible);
					activePillar.dimensionWall.UpdateStateForCamera(Cam.Player, activePillar.dimensionWall.RadsOffsetForDimensionWall(Cam.Player.CamPos()));
					otherPortal.pillarDimensionObject.camQuadrant = originalQuadrant;
				}
			}

			TriggerEventsAfterTeleport(player.GetComponent<Collider>());
			// Replacing with delayed disabling of VP
			// DisableVolumetricPortal();
			otherPortal.EnableVolumetricPortal();
			otherPortal.pauseRendering = false;
			yield return null;
			// Sometimes teleporting leaves the hasTriggeredOnStay state as true so we explicitly reset state here
			trigger.ResetHasTriggeredOnStayState();

			lastTeleportedFrame = Time.frameCount;
			
			teleportingPlayer = false;
		}

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
		#endregion

		#region Volumetric Portal

		public void EnableVolumetricPortal() {
			bool anyVolumetricPortalIsDisabled = volumetricPortals.Any(vp => !vp.enabled);
			if (anyVolumetricPortalIsDisabled) {
				// Don't spam the console when we have the volumetric portal debug setting on
				if (!forceVolumetricPortalsOn) {
					debug.Log("Enabling Volumetric Portal(s) for " + gameObject.name);
				}
				foreach (var vp in volumetricPortals) {
					if (!PortalRenderingIsEnabled) continue;
					
					vp.SetSharedMaterial(portalMaterial);
					vp.enabled = true;
				}
			}
		}

		public void DisableVolumetricPortal() {
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

			public bool pauseRendering = false;
			public bool pauseLogic = false;

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
				this.pauseRendering = portal.pauseRendering;
				this.pauseLogic = portal.pauseLogic;
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
				portal.pauseRendering = this.pauseRendering;
				portal.pauseLogic = this.pauseLogic;
			}
		}
		#endregion
	}
}
