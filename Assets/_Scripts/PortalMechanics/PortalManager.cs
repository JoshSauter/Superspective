using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using EpitaphUtils;
using UnityStandardAssets.ImageEffects;

public class PortalManager : Singleton<PortalManager> {
    DebugLogger debug;
	Dictionary<int, HashSet<PortalSettings>> receiversByChannel = new Dictionary<int, HashSet<PortalSettings>>();
	Dictionary<int, GameObject> cameraContainersByChannel = new Dictionary<int, GameObject>();

	GameObject volumetricPortalPrefab;

	private void Awake() {
        debug = new DebugLogger(this, false);
		volumetricPortalPrefab = Resources.Load<GameObject>("Prefabs/VolumetricPortal");
	}

	/// <summary>
	/// Adds a receiver to the portal dictionary for this channel, and if the portal has two receivers, instantiate the portal
	/// </summary>
	/// <param name="channel"></param>
	/// <param name="receiver"></param>
	public void AddReceiver(int channel, PortalSettings receiver) {
		if (!receiversByChannel.ContainsKey(channel)) {
			receiversByChannel[channel] = new HashSet<PortalSettings>();
		}

		if (receiversByChannel[channel].Count == 2) {
			debug.LogError("Channel " + channel + " already has two receivers! Check the channels for the following receivers:\n" +
				string.Join("\n", receiversByChannel[channel].Select(r => r.name).ToArray()) + "\n" + receiver.name);
			return;
		}

		receiversByChannel[channel].Add(receiver);

		if (receiversByChannel[channel].Count == 2) {
			debug.Log("Enabling Portal for channel " + channel);
			InitializePortalsForChannel(channel);
		}
	}

	/// <summary>
	/// Removes a receiver from the portal dictionary, and cleans up portal pieces
	/// </summary>
	/// <param name="channel"></param>
	/// <param name="receiver"></param>
	/// <returns>Returns true if the receiver was successfully found and removed, false otherwise</returns>
	public bool RemoveReceiver(int channel, PortalSettings receiver) {
		if (!receiversByChannel.ContainsKey(channel)) {
			debug.LogWarning("Trying to remove a receiver for non-existent channel: " + channel);
			return false;
		}

		bool receiverRemoved = receiversByChannel[channel].Remove(receiver);
		if (receiverRemoved && receiversByChannel[channel].Count == 1) {
			debug.Log("Disabling Portal for channel " + channel);
			TeardownPortalsForChannel(channel);
		}
		return receiverRemoved;
	}

#region PortalInitialization
	private void InitializePortalsForChannel(int channel) {
		List<PortalSettings> settings = receiversByChannel[channel].ToList();

		// Create a container game object which contains all the elements of the portal
		List<PortalContainer> portalContainers = CreatePortalContainers(channel);

		// Create a Portal Camera which renders to a RenderTexture for each Receiver in the channel
		List<PortalCameraFollow> portalCameras = CreatePortalCameras(channel);

		// Create the Portal Teleporters on each PortalSettings
		List<PortalTeleporter> portalTeleporters = CreatePortalTeleporters(channel);

		// Create the volumetric portal gameobjects
		List<GameObject> volumetricPortalObjects = CreateVolumetricPortalObjects(channel);

		// Create the trigger zones for volumetric portals
		List<VolumetricPortalTrigger> volumetricPortalTriggers = CreateVolumetricPortalTriggers(channel);

		// Initialize all references needed
		for (int i = 0; i < portalContainers.Count; i++) {
			int other = (i + 1) % 2;

			PortalContainer portalContainer = portalContainers[i];
            PortalSettings portalSettings = settings[i];
			PortalCameraRenderTexture portalCameraRenderTexture = portalSettings.gameObject.AddComponent<PortalCameraRenderTexture>();
			PortalCameraFollow portalCameraFollow = portalCameras[i];
			PortalTeleporter portalTeleporter = portalTeleporters[i];
			GameObject volumetricPortalObject = volumetricPortalObjects[i];
			VolumetricPortalTrigger volumetricPortalTrigger = volumetricPortalTriggers[i];

			// PortalContainer references
			portalContainer.otherPortal = portalContainers[other];
            portalContainer.portalCamera = portalCameraFollow.GetComponent<Camera>();
			portalContainer.settings = portalSettings;
			portalContainer.teleporter = portalTeleporters[i];
			portalContainer.volumetricPortal = volumetricPortalObject;
			portalContainer.volumetricPortalTrigger = volumetricPortalTrigger;

			// PortalCameraRenderTexture references
			portalCameraRenderTexture.portal = portalContainer;

			// PortalCameraFollow references
			portalCameraFollow.portalBeingRendered = portalContainer;

			// PortalTeleporter references
			portalTeleporter.portal = portalContainer;

			// VolumetricPortalTrigger references
			volumetricPortalTrigger.portal = portalContainer;

			if (Vector3.Dot(portalTeleporter.portalNormal, portalTeleporter.transform.forward) > 0)
				volumetricPortalObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
		}
	}

	///////////////////////
	// Portal Containers //
	///////////////////////
	private List<PortalContainer> CreatePortalContainers(int channel) {
		List<PortalSettings> receiversInChannel = receiversByChannel[channel].ToList();
		List<PortalContainer> newContainers = new List<PortalContainer>();
		for (int i = 0; i < receiversInChannel.Count; i++) {
			newContainers.Add(CreatePortalContainer(receiversInChannel[i]));
		}

		return newContainers;
	}

	private PortalContainer CreatePortalContainer(PortalSettings receiver) {
		GameObject portalContainer = new GameObject(receiver.gameObject.name + " Container");
		portalContainer.transform.SetParent(receiver.transform.parent);
		portalContainer.transform.localPosition = receiver.transform.localPosition;

		receiver.transform.SetParent(portalContainer.transform);

		PortalContainer containerScript = portalContainer.AddComponent<PortalContainer>();
		return containerScript;
	}

	////////////////////
	// Portal Cameras //
	////////////////////
	private List<PortalCameraFollow> CreatePortalCameras(int channel) {
		// Create parent object to act as a container for the portal cameras
		GameObject newCameraContainer = new GameObject("Channel" + channel + " Cameras");
		newCameraContainer.transform.SetParent(transform);

		// Create a new camera for each receiver
		List<PortalSettings> receiversInChannel = receiversByChannel[channel].ToList();
        List<PortalCameraFollow> portalCameraFollows = new List<PortalCameraFollow>();
		for (int i = 0; i < receiversInChannel.Count; i++) {
			portalCameraFollows.Add(CreatePortalCamera(newCameraContainer.transform, receiversInChannel[i], receiversInChannel[(i + 1) % receiversInChannel.Count]));
		}

		cameraContainersByChannel.Add(channel, newCameraContainer);
		return portalCameraFollows;
	}

	private PortalCameraFollow CreatePortalCamera(Transform parentObj, PortalSettings receiver, PortalSettings otherPortal) {
		GameObject playerCam = EpitaphScreen.instance.playerCamera.gameObject;

		GameObject newCameraObj = new GameObject(receiver.name + " Camera");
		newCameraObj.transform.SetParent(parentObj);
		
		// Copy main camera component from player's camera
		newCameraObj.AddComponent<Camera>().CopyFrom(playerCam.GetComponent<Camera>());
		int hidePlayerMask = LayerMask.NameToLayer("Player");
		newCameraObj.GetComponent<Camera>().cullingMask &= ~(1 << hidePlayerMask);

		// Copy post-process effects from player's camera
		// Order of components here matters; it affects the rendering order of the postprocess effects
		newCameraObj.PasteComponent(playerCam.GetComponent<BloomOptimized>());                                          // Copy Bloom
		newCameraObj.PasteComponent(playerCam.GetComponent<ScreenSpaceAmbientOcclusion>());                             // Copy SSAO
		BladeEdgeDetection edgeDetection = newCameraObj.PasteComponent(playerCam.GetComponent<BladeEdgeDetection>());   // Copy Edge Detection (maybe change color)
		if (!receiver.useCameraEdgeDetectionColor) {
			edgeDetection.edgeColor = receiver.portalEdgeDetectionColor;
		}
		newCameraObj.PasteComponent(playerCam.GetComponent<ColorfulFog>());                                             // Copy Fog

		// Initialize PortalCameraFollow component
		PortalCameraFollow newPortalCameraFollow = newCameraObj.AddComponent<PortalCameraFollow>();

		// Fix Camera name
		newCameraObj.name = receiver.name + " Camera";
		return newPortalCameraFollow;
	}

	////////////////////////
	// Portal Teleporters //
	////////////////////////
	private List<PortalTeleporter> CreatePortalTeleporters(int channel) {
		// Create a teleporter for each side of the portal
		List<PortalSettings> receivers = receiversByChannel[channel].ToList();
		List<PortalTeleporter> portalTeleporters = new List<PortalTeleporter>();
		
		for (int i = 0; i < receivers.Count; i++) {
			portalTeleporters.Add(CreatePortalTeleporter(receivers[i]));
		}

		return portalTeleporters;
	}

	private PortalTeleporter CreatePortalTeleporter(PortalSettings receiver) {
		GameObject newTeleporterObj = new GameObject("PortalTeleporterTrigger");
		newTeleporterObj.transform.SetParent(receiver.transform, false);
		
		PortalTeleporter newTeleporter = newTeleporterObj.AddComponent<PortalTeleporter>();
		if (!receiver.useCameraEdgeDetectionColor) {
			newTeleporter.teleporter.OnTeleport += SwapEdgeDetectionColorAfterTeleport;
		}
		newTeleporter.transformationsBeforeTeleport = receiver.objectsToTransformOnTeleport;

		return newTeleporter;
	}

	///////////////////////////////
	// Volumetric Portal Objects //
	///////////////////////////////
	private List<GameObject> CreateVolumetricPortalObjects(int channel) {
		List<PortalSettings> receivers = receiversByChannel[channel].ToList();
		List<GameObject> volumetricPortals = new List<GameObject>();
		for (int i = 0; i < receivers.Count; i++) {
			// Initialize volumetric portal components
			GameObject volumetricPortal = Instantiate(volumetricPortalPrefab, receivers[i].transform, false);
			volumetricPortal.name = "VolumetricPortal";
			volumetricPortal.SetActive(false);

			Vector3 portalSize = receivers[i].GetComponent<MeshFilter>().mesh.bounds.size;
			Vector3 volumetricBoxSize = volumetricPortal.GetComponent<MeshFilter>().mesh.bounds.size;
			volumetricPortal.transform.localScale = new Vector3(portalSize.x / volumetricBoxSize.x - 0.01f, portalSize.y/volumetricBoxSize.y - 0.01f, 1);

			volumetricPortals.Add(volumetricPortal);
		}
		return volumetricPortals;
	}

	////////////////////////////////
	// Volumetric Portal Triggers //
	////////////////////////////////
	private List<VolumetricPortalTrigger> CreateVolumetricPortalTriggers(int channel) {
		List<PortalSettings> receivers = receiversByChannel[channel].ToList();
		List<VolumetricPortalTrigger> triggers = new List<VolumetricPortalTrigger>();
		for (int i = 0; i < receivers.Count; i++) {
			GameObject volumetricPortalTrigger = new GameObject();
			volumetricPortalTrigger.name = "VolumetricPortalTrigger";
			volumetricPortalTrigger.transform.SetParent(receivers[i].transform, false);
			triggers.Add(volumetricPortalTrigger.AddComponent<VolumetricPortalTrigger>());
		}

		return triggers;
	}

	#endregion

#region PortalTeardown

	private void TeardownPortalsForChannel(int channel) {
		List<PortalSettings> portalSettings = receiversByChannel[channel].ToList();
		GameObject cameraContainerToDestroy = cameraContainersByChannel[channel];
		cameraContainersByChannel.Remove(channel);
		Destroy(cameraContainerToDestroy);

		foreach (PortalSettings receiver in portalSettings) {
			TeardownPortalReceiver(receiver);
		}
	}

	private void TeardownPortalReceiver(PortalSettings receiver) {
		Transform container = receiver.transform.parent;
		receiver.transform.SetParent(container.parent);
		Destroy(container.gameObject);

		Destroy(receiver.GetComponent<PortalCameraRenderTexture>());

		PortalTeleporter portalTeleporter = receiver.GetComponentInChildren<PortalTeleporter>();
		Transform volumetricPortal = receiver.transform.Find("VolumetricPortal");
		VolumetricPortalTrigger volumetricPortalTrigger = receiver.GetComponentInChildren<VolumetricPortalTrigger>();

		if (portalTeleporter != null) Destroy(portalTeleporter.gameObject);
		if (volumetricPortal != null) Destroy(volumetricPortal.gameObject);
		if (volumetricPortalTrigger != null) Destroy(volumetricPortalTrigger.gameObject);
	}

#endregion

	private void SwapEdgeDetectionColorAfterTeleport(Collider teleportEnter, Collider teleportExit, Collider player) {
		PortalSettings portalInfo = teleportEnter.GetComponentInParent<PortalSettings>();
		SwapEdgeDetectionColors(portalInfo.channel);
	}


	private void SwapEdgeDetectionColors(int channel) {
		BladeEdgeDetection playerED = EpitaphScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();
		Transform camerasParent = cameraContainersByChannel[channel].transform;
		BladeEdgeDetection[] portalEDs = camerasParent.GetComponentsInChildrenOnly<BladeEdgeDetection>();

		BladeEdgeDetection.EdgeColorMode tempEdgeColorMode = playerED.edgeColorMode;
		Color tempColor = playerED.edgeColor;
		Gradient tempColorGradient = playerED.edgeColorGradient;
		Texture2D tempColorGradientTexture = playerED.edgeColorGradientTexture;

		CopyEdgeColors(source: portalEDs[0], dest: playerED);
		portalEDs.ToList().ForEach(ed => CopyEdgeColors(ed, tempEdgeColorMode, tempColor, tempColorGradient, tempColorGradientTexture));
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

}