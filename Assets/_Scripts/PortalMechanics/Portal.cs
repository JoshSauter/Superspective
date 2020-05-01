using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using EpitaphUtils;
using System.Runtime.CompilerServices;
using UnityEditor;
using System.Linq;

namespace PortalMechanics {
	public class Portal : MonoBehaviour {
		public bool DEBUG = false;
		public string channel = "<Not set>";

		public bool changeCameraEdgeDetection;
		[ShowIf("changeCameraEdgeDetection")]
		public BladeEdgeDetection.EdgeColorMode edgeColorMode;
		[ShowIf("changeCameraEdgeDetection")]
		public Color edgeColor = Color.black;
		[ShowIf("changeCameraEdgeDetection")]
		public Gradient edgeColorGradient;
		[ShowIf("changeCameraEdgeDetection")]
		public Texture2D edgeColorGradientTexture;

		public bool writePortalSurfaceToDepthBuffer = false;
		public bool renderRecursivePortals = false;

		private GameObject volumetricPortalPrefab;
		private Renderer volumetricPortal;
		private Material portalMaterial;
		private Material fallbackMaterial;
		public new Renderer renderer;
		private new Collider collider;
		private Transform playerCamera;
		private CameraFollow playerCameraFollow;

		[HorizontalLine]

		public Portal otherPortal;
		public HashSet<PortalableObject> objectsInPortal = new HashSet<PortalableObject>();

		private RenderTexture internalRenderTextureCopy;

		public bool pauseRenderingAndLogic = false;
		public bool portalIsEnabled { get { return otherPortal != null && !pauseRenderingAndLogic; } }

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
		private DebugLogger debug;

#if UNITY_EDITOR
		[MenuItem("CONTEXT/Portal/Select other side of portal")]
		public static void SelectOtherSideOfPortal(MenuCommand command) {
			Portal thisPortal = (Portal)command.context;
			Selection.objects = FindObjectsOfType<Portal>().Where(p => p != thisPortal && p.channel == thisPortal.channel).Select(p => p.gameObject).ToArray();
		}
#endif

		#region MonoBehaviour Methods
		private void Awake() {
			debug = new DebugLogger(gameObject, () => DEBUG);
			renderer = GetComponent<Renderer>();
			collider = GetComponent<Collider>();


			string depthWriteShaderPath = "Shaders/RecursivePortals/PortalMaterial";
			string noDepthWriteShaderPath = "Shaders/RecursivePortals/PortalMaterialNoDepthWrite";
			portalMaterial = new Material(Resources.Load<Shader>(writePortalSurfaceToDepthBuffer ? depthWriteShaderPath : noDepthWriteShaderPath));
			fallbackMaterial = Resources.Load<Material>("Materials/Invisible");

			volumetricPortalPrefab = Resources.Load<GameObject>("Prefabs/VolumetricPortal");
			volumetricPortal = Instantiate(volumetricPortalPrefab, transform, false).GetComponent<Renderer>();

			volumetricPortal.enabled = false;
		}

		void CreateRenderTexture(int width, int height) {
			internalRenderTextureCopy = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
			portalMaterial.mainTexture = internalRenderTextureCopy;
		}

		private void Start() {
			playerCamera = EpitaphScreen.instance.playerCamera.transform;
			playerCameraFollow = playerCamera.GetComponent<CameraFollow>();

			CreateRenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);
			EpitaphScreen.instance.OnScreenResolutionChanged += CreateRenderTexture;
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
		}

		private void LateUpdate() {
			foreach (var portalableObj in objectsInPortal) {
				portalableObj.EnableAndUpdatePortalCopy(this);
			}
		}

		private void OnEnable() {
			StartCoroutine(AddReceiverCoroutine());
		}

		private void OnDisable() {
			PortalManager.instance.RemovePortal(channel, this);
		}

		private void OnTriggerEnter(Collider other) {
			if (other.TaggedAsPlayer()) {
				EnableVolumetricPortal();
			}
			else {
				PortalableObject portalableObj = other.gameObject.GetComponent<PortalableObject>();
				if (portalableObj != null) {
					objectsInPortal.Add(portalableObj);
					portalableObj.EnableAndUpdatePortalCopy(this);
					portalableObj.sittingInPortal = this;
				}
			}
		}

		private void OnTriggerExit(Collider other) {
			if (other.TaggedAsPlayer()) {
				DisableVolumetricPortal();
			}
			else {
				PortalableObject portalableObj = other.gameObject.GetComponent<PortalableObject>();
				if (portalableObj != null && objectsInPortal.Contains(portalableObj)) {
					objectsInPortal.Remove(portalableObj);
					portalableObj.DisablePortalCopy();
					portalableObj.sittingInPortal = null;
				}
			}
		}

		private void OnTriggerStay(Collider other) {
			if (!portalIsEnabled) return;

			bool objectShouldBeTeleported = Mathf.Sign(Vector3.Dot(transform.forward, (other.transform.position - transform.position).normalized)) > 0;
			if (!objectShouldBeTeleported) return;

			if (!other.TaggedAsPlayer()) {
				PortalableObject portalableObj = other.gameObject.GetComponent<PortalableObject>();
				if (portalableObj != null && objectsInPortal.Contains(portalableObj)) {
					TeleportObject(portalableObj);

					// Swap state to the other portal
					otherPortal.objectsInPortal.Add(portalableObj);
					objectsInPortal.Remove(portalableObj);
					portalableObj.EnableAndUpdatePortalCopy(otherPortal);
					portalableObj.sittingInPortal = otherPortal;
				}
			}
			else {
				playerCameraFollow.currentLerpSpeed = 4500f;
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
			return volumetricPortal.enabled;
		}

		public void SetTexture(RenderTexture tex) {
			renderer.material = portalMaterial;
			volumetricPortal.material = portalMaterial;
			Graphics.CopyTexture(tex, internalRenderTextureCopy);
		}

		public void DefaultMaterial() {
			renderer.material = fallbackMaterial;
			volumetricPortal.material = fallbackMaterial;
		}

		public RenderTexture GetTexture() {
			return internalRenderTextureCopy;
		}

		public bool IsVisibleFrom(Camera cam) {
			return renderer.IsVisibleFrom(cam) || volumetricPortal.IsVisibleFrom(cam);
		}

		public Vector3 ClosestPoint(Vector3 point) {
			return collider.ClosestPoint(point);
		}

		public Rect GetScreenRect(Camera cam) {
			Vector3 cen = renderer.bounds.center;
			Vector3 ext = renderer.bounds.extents;

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

			return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
		}
		#endregion

		#region Portal Teleporter
		private void CreatePortalTeleporter() {
			collider.isTrigger = true;
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
			Vector3 relativeObjPos = transform.InverseTransformPoint(objToTransform.position);
			relativeObjPos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeObjPos;
			objToTransform.position = otherPortal.transform.TransformPoint(relativeObjPos);

			// Rotation
			Quaternion relativeRot = Quaternion.Inverse(transform.rotation) * objToTransform.rotation;
			relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
			objToTransform.rotation = otherPortal.transform.rotation * relativeRot;

			// Velocity?
			Rigidbody objRigidbody = objToTransform.GetComponent<Rigidbody>();
			if (transformVelocity && objRigidbody != null) {
				Vector3 objRelativeVelocity = transform.InverseTransformDirection(objRigidbody.velocity);
				objRelativeVelocity = Quaternion.Euler(0.0f, 180.0f, 0.0f) * objRelativeVelocity;
				objRigidbody.velocity = otherPortal.transform.TransformDirection(objRelativeVelocity);
			}
		}

		private bool teleportingPlayer = false;
		/// <summary>
		/// Frame 1: Do nothing (but ensure that this is not called twice)
		/// Frame 2: Teleport player and disable this portal's volumetric portal while enabling the otherPortal's volumetric portal
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		IEnumerator TeleportPlayer(Transform player) {
			teleportingPlayer = true;

			//if (DEBUG) Debug.Break();
			TriggerEventsBeforeTeleport(player.GetComponent<Collider>());

			// Position
			Vector3 relativePlayerPos = transform.InverseTransformPoint(player.position);
			relativePlayerPos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePlayerPos;
			player.position = otherPortal.transform.TransformPoint(relativePlayerPos);

			// Rotation
			Quaternion relativeRot = Quaternion.Inverse(transform.rotation) * player.rotation;
			relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
			player.rotation = otherPortal.transform.rotation * relativeRot;

			// Velocity
			Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
			Vector3 playerRelativeVelocity = transform.InverseTransformDirection(playerRigidbody.velocity);
			playerRelativeVelocity = Quaternion.Euler(0.0f, 180.0f, 0.0f) * playerRelativeVelocity;
			playerRigidbody.velocity = otherPortal.transform.TransformDirection(playerRelativeVelocity);

			Physics.gravity = Physics.gravity.magnitude * -player.up;
			//PlayerMovement.instance.enabled = false;

			if (changeCameraEdgeDetection) {
				SwapEdgeDetectionColors();
			}

			TriggerEventsAfterTeleport(player.GetComponent<Collider>());
			otherPortal.EnableVolumetricPortal();
			DisableVolumetricPortal();
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

		private bool playerRemainsInPortal = false;
		private void EnableVolumetricPortal() {
			if (!volumetricPortal.enabled) {
				debug.Log("Enabling Volumetric Portal for " + gameObject.name);
				volumetricPortal.material = portalMaterial;
				volumetricPortal.enabled = true;
			}
		}

		private void DisableVolumetricPortal() {
			if (volumetricPortal.enabled) {
				volumetricPortal.enabled = false;
				debug.Log("Disabling Volumetric Portal for " + gameObject.name);
			}
		}
		#endregion

		#region Helper Methods
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void setMinMax(Vector2 point, ref Vector2 min, ref Vector2 max) {
			min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
			max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
		}

		IEnumerator AddReceiverCoroutine() {
			while (!gameObject.scene.isLoaded) {
				//debug.Log("Waiting for scene " + gameObject.scene + " to be loaded before adding receiver...");
				yield return null;
			}

			PortalManager.instance.AddPortal(channel, this);
		}

		private void SwapEdgeDetectionColors() {
			BladeEdgeDetection playerED = EpitaphScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();

   			BladeEdgeDetection.EdgeColorMode tempEdgeColorMode = playerED.edgeColorMode;
			Color tempColor = playerED.edgeColor;
			Gradient tempColorGradient = playerED.edgeColorGradient;
			Texture2D tempColorGradientTexture = playerED.edgeColorGradientTexture;

			CopyEdgeColors(dest: playerED, edgeColorMode, edgeColor, edgeColorGradient, edgeColorGradientTexture);

			edgeColorMode = tempEdgeColorMode;
			edgeColor = tempColor;
			edgeColorGradient = tempColorGradient;
			edgeColorGradientTexture = tempColorGradientTexture;
		}

		public void CopyEdgeColors(BladeEdgeDetection dest, BladeEdgeDetection.EdgeColorMode edgeColorMode, Color edgeColor, Gradient edgeColorGradient, Texture2D edgeColorGradientTexture) {
			dest.edgeColorMode = edgeColorMode;
			dest.edgeColor = edgeColor;
			dest.edgeColorGradient = edgeColorGradient;
			dest.edgeColorGradientTexture = edgeColorGradientTexture;
		}
		#endregion
	}
}
