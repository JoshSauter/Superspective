using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using EpitaphUtils;
using System.Runtime.CompilerServices;
using UnityEditor;
using System.Linq;
using Saving;
using SerializableClasses;
using UnityEngine.Serialization;

namespace PortalMechanics {
	[RequireComponent(typeof(UniqueId))]
	public class Portal : SaveableObject<Portal, Portal.PortalSave> {
		UniqueId id;

		UniqueId UniqueId {
			get {
				if (id == null) {
					id = GetComponent<UniqueId>();
				}
				return id;
			}
		}

		public static RenderTextureFormat DepthNormalsTextureFormat = RenderTextureFormat.ARGBFloat;

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
		Renderer[] volumetricPortals;
		Material portalMaterial;
		Material fallbackMaterial;
		public Renderer[] renderers;
		public Collider[] colliders;
		Transform playerCamera;
		CameraFollow playerCameraFollow;
		bool teleportingPlayer = false;

		[HorizontalLine]

		public Portal otherPortal;
		public HashSet<PortalableObject> objectsInPortal = new HashSet<PortalableObject>();

		RenderTexture internalRenderTextureCopy;
		RenderTexture internalDepthNormalsTextureCopy;

		public bool pauseRenderingOnly = false;
		public bool pauseRenderingAndLogic = false;
		public bool portalIsEnabled => otherPortal != null && !pauseRenderingAndLogic && gameObject.activeSelf;

		[ShowNativeProperty]
		bool playerRemainsInPortal => volumetricPortals?.Any(vp => vp.enabled) ?? false;

#region Events
		public delegate void PortalTeleportAction(Portal inPortal, Collider objectTeleported);
		public delegate void SimplePortalTeleportAction(Collider objectTeleported);

		public event PortalTeleportAction BeforePortalTeleport;
		public event PortalTeleportAction OnPortalTeleport;
		public event SimplePortalTeleportAction BeforePortalTeleportSimple;
		public event SimplePortalTeleportAction OnPortalTeleportSimple;

		public static event PortalTeleportAction BeforeAnyPortalTeleport;
		public static event PortalTeleportAction OnAnyPortalTeleport;
		public static event SimplePortalTeleportAction BeforeAnyPortalTeleportSimple;
		public static event SimplePortalTeleportAction OnAnyPortalTeleportSimple;
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
		protected override void Awake() {
			base.Awake();
			string shaderPath = "Shaders/RecursivePortals/PortalMaterial";
			portalMaterial = new Material(Resources.Load<Shader>(shaderPath));
			fallbackMaterial = Resources.Load<Material>("Materials/Invisible");
			if (renderers == null || renderers.Length == 0) {
				if (compositePortal) {
					renderers = GetComponentsInChildren<Renderer>();
				}
				else {
					renderers = new Renderer[] { GetComponent<Renderer>() };
				}
			}
			foreach (var r in renderers) {
				r.gameObject.layer = LayerMask.NameToLayer("Portal");
				if (pauseRenderingAndLogic) {
					r.material = fallbackMaterial;
				}
				else {
					r.material = portalMaterial;
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
			}

			foreach (var c in colliders) {
				if (c.gameObject != this.gameObject) {
					c.gameObject.AddComponent<PortalCollider>().portal = this;
				}
			}

			volumetricPortalPrefab = Resources.Load<GameObject>("Prefabs/VolumetricPortal");
			volumetricPortals = colliders.Select(r => Instantiate(volumetricPortalPrefab, r.transform, false).GetComponent<Renderer>()).ToArray();
			for (int i = 0; i < volumetricPortals.Length; i++) {
				Renderer vp = volumetricPortals[i];
				Collider collider = colliders[i];
				Vector3 vpScale = Vector3.one;
				if (collider is BoxCollider) {
					vpScale = (collider as BoxCollider).size / 10f;
				}
				else if (collider is MeshCollider) {
					vpScale = (collider as MeshCollider).bounds.size / 10f;
				}
				else {
					Debug.LogError("Collider type: " + collider.GetType().ToString() + " not handled.");
				}
				vpScale.z = 1f;
				vp.transform.localScale = vpScale;

				vp.enabled = false;
			}
		}

		void CreateRenderTexture(int width, int height) {
			internalRenderTextureCopy = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR);
			internalDepthNormalsTextureCopy = new RenderTexture(width, height, 24, DepthNormalsTextureFormat);
			portalMaterial.mainTexture = internalRenderTextureCopy;
			portalMaterial.SetTexture("_DepthNormals", internalDepthNormalsTextureCopy);
		}

		protected override void Start() {
			base.Start();
			playerCamera = EpitaphScreen.instance.playerCamera.transform;
			playerCameraFollow = playerCamera.GetComponent<CameraFollow>();

			CreateRenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);
			EpitaphScreen.instance.OnScreenResolutionChanged += CreateRenderTexture;

			if (pauseRenderingOnly || pauseRenderingAndLogic) {
				DefaultMaterial();
			}
			else {
				foreach (var r in renderers) {
					r.material = portalMaterial;
				}
				foreach (var vp in volumetricPortals) {
					vp.material = portalMaterial;
				}
			}
			
			// DimensionObject Portals are treated in a special-case way
			// This is so that the Portal script does not change the shader off of the DimensionPortalMaterial
			if (TryGetComponent(out DimensionObject d) && !(d is PillarDimensionObject)) {
				portalMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionPortalMaterial"));
				portalMaterial.SetInt("_Inverse", 1);
			}
		}

		bool test = false;
		void FixedUpdate() {
			bool playerIsCloseToPortal = Vector3.Distance(Player.instance.transform.position, ClosestPoint(Player.instance.transform.position)) < 0.99f;
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
			PortalManager.instance.RemovePortal(channel, this);
		}

		public void OnTriggerEnter(Collider other) {
			if (other.TaggedAsPlayer()) {
				EnableVolumetricPortal();
			}
			else {
				PortalableObject portalableObj = other.gameObject.GetComponent<PortalableObject>();
				if (portalableObj != null) {
					objectsInPortal.Add(portalableObj);
					portalableObj.sittingInPortal = this;
				}
			}
		}

		public void OnTriggerExit(Collider other) {
			if (other.TaggedAsPlayer()) {
				DisableVolumetricPortal();
			}
			else {
				PortalableObject portalableObj = other.gameObject.GetComponent<PortalableObject>();
				if (portalableObj != null && objectsInPortal.Contains(portalableObj)) {
					objectsInPortal.Remove(portalableObj);
					portalableObj.sittingInPortal = null;
				}
			}
		}

		public void OnTriggerStay(Collider other) {
			if (!portalIsEnabled) return;

			Vector3 closestPoint = ClosestPoint(other.transform.position);
			// TODO: This check doesn't work properly, player will rapidly teleport back and forth if standing in the middle
			bool objectShouldBeTeleported = Mathf.Sign(Vector3.Dot(PortalNormal(), (other.transform.position - closestPoint).normalized)) > 0;
			if (!objectShouldBeTeleported) return;

			if (!other.TaggedAsPlayer()) {
				PortalableObject portalableObj = other.gameObject.GetComponent<PortalableObject>();
				if (portalableObj != null && objectsInPortal.Contains(portalableObj)) {
					TeleportObject(portalableObj);

					// Swap state to the other portal
					objectsInPortal.Remove(portalableObj);
					otherPortal.objectsInPortal.Add(portalableObj);
					portalableObj.sittingInPortal = otherPortal;
				}
			}
			else {
				playerCameraFollow.SetLerpSpeed(CameraFollow.desiredLerpSpeed);
				if (!teleportingPlayer) {
					StartCoroutine(TeleportPlayer(other.transform));
				}
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
			bool anyVolumetricPortalIsEnabled = false;
			foreach (var vp in volumetricPortals) {
				if (vp.enabled) {
					anyVolumetricPortalIsEnabled = true;
					break;
				}
			}
			return anyVolumetricPortalIsEnabled;
		}

		public void SetTexture(RenderTexture tex) {
			if (pauseRenderingOnly || pauseRenderingAndLogic) {
				debug.LogWarning($"Attempting to set MainTexture for disabled portal: {gameObject.name}");
				return;
			}
			foreach (var r in renderers) {
				r.material = portalMaterial;
			}
			foreach (var vp in volumetricPortals) {
				vp.material = portalMaterial;
			}
			Graphics.CopyTexture(tex, internalRenderTextureCopy);
		}

		public void SetDepthNormalsTexture(RenderTexture tex) {
			if (pauseRenderingOnly || pauseRenderingAndLogic) {
				debug.LogWarning($"Attempting to set DepthNormalsTexture for disabled portal: {gameObject.name}");
				return;
			}
			foreach (var r in renderers) {
				r.material = portalMaterial;
			}
			foreach (var vp in volumetricPortals) {
				vp.material = portalMaterial;
			}
			Graphics.CopyTexture(tex, internalDepthNormalsTextureCopy);
		}

		public void DefaultMaterial() {
			foreach (var r in renderers) {
				r.material = fallbackMaterial;
			}
			foreach (var vp in volumetricPortals) {
				vp.material = fallbackMaterial;
			}
		}

		public RenderTexture GetTexture() {
			return internalRenderTextureCopy;
		}

		public bool IsVisibleFrom(Camera cam) {
			return renderers.Any(r => r.IsVisibleFrom(cam)) || volumetricPortals.Any(vp => vp.IsVisibleFrom(cam));
		}

		public Vector3 PortalNormal() {
			if (renderers == null || renderers.Length == 0) {
				return transform.forward;
			}
			else {
				return renderers[0].transform.forward;
			}
		}

		public Vector3 ClosestPoint(Vector3 point) {
			float minDistance = float.MaxValue;
			Vector3 closestPoint = point + Vector3.up * minDistance;
			foreach (var c in colliders) {
				if (!c.gameObject.activeSelf || !c.enabled) continue;
				Vector3 thisClosestPoint = c.ClosestPoint(point);
				float thisDistance = Vector3.Distance(thisClosestPoint, point);
				if (thisDistance < minDistance) {
					minDistance = thisDistance;
					closestPoint = thisClosestPoint;
				}
			}
			return closestPoint;
		}

		public Rect[] GetScreenRects(Camera cam) {
			List<Rect> allRects = new List<Rect>();
			foreach (var r in renderers) {
				Vector3 cen = r.bounds.center;
				Vector3 ext = r.bounds.extents;

				Vector2 min = cam.WorldToViewportPoint(new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z - ext.z));
				Vector2 max = min;

				//0
				Vector2 point = min;
				setMinMax(point, ref min, ref max);

				//1
				point = cam.WorldToViewportPoint(new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z - ext.z));
				setMinMax(point, ref min, ref max);


				//2
				point = cam.WorldToViewportPoint(new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z + ext.z));
				setMinMax(point, ref min, ref max);

				//3
				point = cam.WorldToViewportPoint(new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z + ext.z));
				setMinMax(point, ref min, ref max);

				//4
				point = cam.WorldToViewportPoint(new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z - ext.z));
				setMinMax(point, ref min, ref max);

				//5
				point = cam.WorldToViewportPoint(new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z - ext.z));
				setMinMax(point, ref min, ref max);

				//6
				point = cam.WorldToViewportPoint(new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z + ext.z));
				setMinMax(point, ref min, ref max);

				//7
				point = cam.WorldToViewportPoint(new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z + ext.z));
				setMinMax(point, ref min, ref max);

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
			// Position
			objToTransform.position = TransformPoint(objToTransform.position);

			// Rotation
			objToTransform.rotation = TransformRotation(objToTransform.rotation);

			// Velocity?
			Rigidbody objRigidbody = objToTransform.GetComponent<Rigidbody>();
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

			TriggerEventsAfterTeleport(player.GetComponent<Collider>());
			DisableVolumetricPortal();
			otherPortal.EnableVolumetricPortal();
			yield return null;
			teleportingPlayer = false;
		}

		void TriggerEventsBeforeTeleport(Collider objBeingTeleported) {
			BeforePortalTeleport?.Invoke(this, objBeingTeleported);
			BeforeAnyPortalTeleport?.Invoke(this, objBeingTeleported);
			BeforePortalTeleportSimple?.Invoke(objBeingTeleported);
			BeforeAnyPortalTeleportSimple?.Invoke(objBeingTeleported);
		}

		void TriggerEventsAfterTeleport(Collider objBeingTeleported) {
			OnPortalTeleport?.Invoke(this, objBeingTeleported);
			OnAnyPortalTeleport?.Invoke(this, objBeingTeleported);
			OnPortalTeleportSimple?.Invoke(objBeingTeleported);
			OnAnyPortalTeleportSimple?.Invoke(objBeingTeleported);
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
						vp.material = portalMaterial;
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
		static void setMinMax(Vector2 point, ref Vector2 min, ref Vector2 max) {
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

		void SwapEdgeDetectionColors() {
			BladeEdgeDetection playerED = EpitaphScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();

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
		public override string ID => $"Portal_{UniqueId.uniqueId}";
		
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

			public PortalSave(Portal portal) {
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
