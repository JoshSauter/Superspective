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
		public static RenderTextureFormat DepthNormalsTextureFormat = RenderTextureFormat.ARGBFloat;

		[Header("Make sure the Transform's Z-direction arrow points into the portal")]
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

		public bool renderRecursivePortals = false;
		[Tooltip("Enable composite portals if there are multiple renderers that make up the portal surface. Ensure that these renderers are the only children of the portal gameObject.")]
		public bool compositePortal = false;

		private GameObject volumetricPortalPrefab;
		private Renderer[] volumetricPortals;
		private Material portalMaterial;
		private Material fallbackMaterial;
		public Renderer[] renderers;
		public Collider[] colliders;
		private Transform playerCamera;
		private CameraFollow playerCameraFollow;

		[HorizontalLine]

		public Portal otherPortal;
		public HashSet<PortalableObject> objectsInPortal = new HashSet<PortalableObject>();

		private RenderTexture internalRenderTextureCopy;
		private RenderTexture internalDepthNormalsTextureCopy;

		public bool pauseRenderingOnly = false;
		public bool pauseRenderingAndLogic = false;
		public bool portalIsEnabled => otherPortal != null && !pauseRenderingAndLogic && gameObject.activeSelf;

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
		protected virtual void Awake() {
			debug = new DebugLogger(gameObject, () => DEBUG);
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

			string shaderPath = "Shaders/RecursivePortals/PortalMaterial";
			portalMaterial = new Material(Resources.Load<Shader>(shaderPath));
			fallbackMaterial = Resources.Load<Material>("Materials/Invisible");

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
			internalRenderTextureCopy = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
			internalDepthNormalsTextureCopy = new RenderTexture(width, height, 24, DepthNormalsTextureFormat);
			portalMaterial.mainTexture = internalRenderTextureCopy;
			portalMaterial.SetTexture("_DepthNormals", internalDepthNormalsTextureCopy);
		}

		private void Start() {
			playerCamera = EpitaphScreen.instance.playerCamera.transform;
			playerCameraFollow = playerCamera.GetComponent<CameraFollow>();

			CreateRenderTexture(EpitaphScreen.currentWidth, EpitaphScreen.currentHeight);
			EpitaphScreen.instance.OnScreenResolutionChanged += CreateRenderTexture;

			if (pauseRenderingOnly || pauseRenderingAndLogic) {
				DefaultMaterial();
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
		}

		private void OnEnable() {
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
				playerCameraFollow.SetLerpSpeed(4500f);
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
		private void CreatePortalTeleporter() {
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

		private bool teleportingPlayer = false;
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

		private bool playerRemainsInPortal = false;
		private void EnableVolumetricPortal() {
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

		private void DisableVolumetricPortal() {
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

		private void SwapEdgeDetectionColors() {
			BladeEdgeDetection playerED = EpitaphScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();

   			BladeEdgeDetection.EdgeColorMode tempEdgeColorMode = playerED.edgeColorMode;
			Color tempColor = playerED.edgeColor;
			Gradient tempColorGradient = playerED.edgeColorGradient;
			Texture2D tempColorGradientTexture = playerED.edgeColorGradientTexture;

			CopyEdgeColors(dest: playerED, edgeColorMode, edgeColor, edgeColorGradient, edgeColorGradientTexture);

			otherPortal.changeCameraEdgeDetection = true;
			otherPortal.edgeColorMode = tempEdgeColorMode;
			otherPortal.edgeColor = tempColor;
			otherPortal.edgeColorGradient = tempColorGradient;
			otherPortal.edgeColorGradientTexture = tempColorGradientTexture;
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
