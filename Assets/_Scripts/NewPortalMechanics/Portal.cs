using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using EpitaphUtils;
using System.Runtime.CompilerServices;

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

	private GameObject volumetricPortalPrefab;
	private Renderer volumetricPortal;
	private Material portalMaterial;
	private Material fallbackMaterial;
	private new Renderer renderer;
	private new Collider collider;

	[HorizontalLine]

	public Portal otherPortal;

	private RenderTexture internalRenderTextureCopy;

	public bool isEnabled { get { return otherPortal != null; } }

#region Events
	public delegate void PortalTeleportAction(Portal inPortal, Portal outPortal, Collider objectTeleported);
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

#region MonoBehaviour Methods
	private void Awake() {
		debug = new DebugLogger(gameObject, () => DEBUG);
		renderer = GetComponent<Renderer>();
		collider = GetComponent<Collider>();

		internalRenderTextureCopy = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);

		string depthWriteShaderPath = "Shaders/RecursivePortals/PortalMaterial";
		string noDepthWriteShaderPath = "Shaders/RecursivePortals/PortalMaterialNoDepthWrite";
		portalMaterial = new Material(Resources.Load<Shader>(writePortalSurfaceToDepthBuffer ? depthWriteShaderPath : noDepthWriteShaderPath));
		fallbackMaterial = Resources.Load<Material>("Materials/Invisible");
		portalMaterial.mainTexture = internalRenderTextureCopy;

		volumetricPortalPrefab = Resources.Load<GameObject>("Prefabs/VolumetricPortal");
		volumetricPortal = Instantiate(volumetricPortalPrefab, transform, false).GetComponent<Renderer>();

		volumetricPortal.enabled = false;
	}

	private void OnEnable() {
		StartCoroutine(AddReceiverCoroutine());
	}

	private void OnDisable() {
		NewPortalManager.instance.RemovePortal(channel, this);
	}

	// TODO: Improve this to work with any object, not just player
	private void OnTriggerEnter(Collider other) {
		if (other.TaggedAsPlayer()) {
			EnableVolumetricPortal();
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.TaggedAsPlayer()) {
			DisableVolumetricPortal();
		}
	}

	private void OnTriggerStay(Collider other) {
		if (!other.TaggedAsPlayer() || !isEnabled) return;

		if (!teleportingPlayer && Mathf.Sign(Vector3.Dot(transform.forward, (other.transform.position - transform.position).normalized)) > 0) {
			StartCoroutine(TeleportPlayer(other.transform));
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

	private bool teleportingPlayer = false;
	/// <summary>
	/// Frame 1: Do nothing (but ensure that this is not called twice)
	/// Frame 2: Teleport player and disable this portal's volumetric portal while enabling the otherPortal's volumetric portal
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	IEnumerator TeleportPlayer(Transform player) {
		teleportingPlayer = true;

		if (DEBUG) Debug.Break();
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

		TriggerEventsAfterTeleport(player.GetComponent<Collider>());
		otherPortal.EnableVolumetricPortal();
		DisableVolumetricPortal();
		yield return null;
		teleportingPlayer = false;
	}

	void TriggerEventsBeforeTeleport(Collider objBeingTeleported) {
		BeforePortalTeleport?.Invoke(this, otherPortal, objBeingTeleported);
		BeforeAnyPortalTeleport?.Invoke(this, otherPortal, objBeingTeleported);
		BeforePortalTeleportSimple?.Invoke(objBeingTeleported);
		BeforeAnyPortalTeleportSimple?.Invoke(objBeingTeleported);
	}

	void TriggerEventsAfterTeleport(Collider objBeingTeleported) {
		OnPortalTeleport?.Invoke(this, otherPortal, objBeingTeleported);
		OnAnyPortalTeleport?.Invoke(this, otherPortal, objBeingTeleported);
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

		NewPortalManager.instance.AddPortal(channel, this);
	}

	private void SwapEdgeDetectionColors() {
		BladeEdgeDetection playerED = EpitaphScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();
		BladeEdgeDetection portalED = VirtualPortalCamera.instance.GetComponent<BladeEdgeDetection>();

		BladeEdgeDetection.EdgeColorMode tempEdgeColorMode = playerED.edgeColorMode;
		Color tempColor = playerED.edgeColor;
		Gradient tempColorGradient = playerED.edgeColorGradient;
		Texture2D tempColorGradientTexture = playerED.edgeColorGradientTexture;

		CopyEdgeColors(source: portalED, dest: playerED);
		CopyEdgeColors(portalED, tempEdgeColorMode, tempColor, tempColorGradient, tempColorGradientTexture);
	}

	private void CopyEdgeColors(BladeEdgeDetection source, BladeEdgeDetection dest) {
		CopyEdgeColors(dest, source.edgeColorMode, source.edgeColor, source.edgeColorGradient, source.edgeColorGradientTexture);
	}

	private void CopyEdgeColors(BladeEdgeDetection dest, BladeEdgeDetection.EdgeColorMode edgeColorMode, Color edgeColor, Gradient edgeColorGradient, Texture2D edgeColorGradientTexture) {
		dest.edgeColorMode = edgeColorMode;
		dest.edgeColor = edgeColor;
		dest.edgeColorGradient = edgeColorGradient;
		dest.edgeColorGradientTexture = edgeColorGradientTexture;
	}
	#endregion
}
