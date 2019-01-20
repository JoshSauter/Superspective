﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using EpitaphUtils;
using UnityStandardAssets.ImageEffects;

public class PortalManager : Singleton<PortalManager> {
	Dictionary<int, HashSet<PortalSettings>> receiversByChannel = new Dictionary<int, HashSet<PortalSettings>>();
	Dictionary<int, GameObject> cameraContainersByChannel = new Dictionary<int, GameObject>();

	GameObject volumetricPortalPrefab;

	private void Awake() {
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
			Debug.LogError("Channel " + channel + " already has two receivers! Check the channels for the following receivers:\n" +
				string.Join("\n", receiversByChannel[channel].Select(r => r.name).ToArray()) + "\n" + receiver.name);
			return;
		}

		receiversByChannel[channel].Add(receiver);

		if (receiversByChannel[channel].Count == 2) {
			print("Enabling Portal for channel " + channel);
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
			Debug.LogWarning("Trying to remove a receiver for non-existent channel: " + channel);
			return false;
		}

		bool receiverRemoved = receiversByChannel[channel].Remove(receiver);
		if (receiverRemoved) {
			print("Disabling Portal for channel " + channel);
			// TODO: Disable portal for channel
			Debug.LogError("Not yet implemented: PortalManager.RemoveReceiver()");
		}
		return receiverRemoved;
	}

#region PortalInitialization
	private void InitializePortalsForChannel(int channel) {
		List<PortalSettings> settings = receiversByChannel[channel].ToList();

		// Create a container game object which contains all the elements of the portal
		List<PortalContainer> portalContainers = CreatePortalContainers(channel);

		// Create a Portal Camera which renders to a RenderTexture for each Receiver in the channel
		List<GameObject> portalCameras = CreatePortalCameras(channel);

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
			PortalCameraRenderTexture portalCameraRenderTexture = portalCameras[i].GetComponent<PortalCameraRenderTexture>();
			PortalCameraFollow portalCameraFollow = portalCameras[i].GetComponent<PortalCameraFollow>();
			PortalTeleporter portalTeleporter = portalTeleporters[i];
			GameObject volumetricPortalObject = volumetricPortalObjects[i];
			VolumetricPortalTrigger volumetricPortalTrigger = volumetricPortalTriggers[i];

			// PortalContainer references
			portalContainer.otherPortal = portalContainers[other];
			portalContainer.settings = settings[i];
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
	private List<GameObject> CreatePortalCameras(int channel) {
		// Create parent object to act as a container for the portal cameras
		GameObject newCameraContainer = new GameObject("Channel" + channel + " Cameras");
		newCameraContainer.transform.SetParent(transform);

		// Create a new camera for each receiver
		List<PortalSettings> receiversInChannel = receiversByChannel[channel].ToList();
		for (int i = 0; i < receiversInChannel.Count; i++) {
			CreatePortalCamera(newCameraContainer.transform, receiversInChannel[i], receiversInChannel[(i + 1) % receiversInChannel.Count]);
		}

		cameraContainersByChannel.Add(channel, newCameraContainer);
		return newCameraContainer.transform.GetComponentsInChildrenOnly<Transform>().Select(x => x.gameObject).ToList();
	}

	private GameObject CreatePortalCamera(Transform parentObj, PortalSettings receiver, PortalSettings otherPortal) {
		GameObject playerCam = EpitaphScreen.instance.playerCamera.gameObject;

		GameObject newCameraObj = new GameObject(receiver.name + " Camera");
		newCameraObj.transform.SetParent(parentObj);
		
		// Copy main camera component from player's camera
		newCameraObj.AddComponent<Camera>().CopyFrom(playerCam.GetComponent<Camera>());
		int hidePlayerMask = LayerMask.NameToLayer("Player");
		newCameraObj.GetComponent<Camera>().cullingMask &= ~(1 << hidePlayerMask);

		// Copy post-process effects from player's camera
		// Order of components here matters; it affects the rendering order of the postprocess effects
		newCameraObj.PasteComponent(playerCam.GetComponent<BloomOptimized>());											// Copy Bloom
		newCameraObj.PasteComponent(playerCam.GetComponent<ScreenSpaceAmbientOcclusion>());                             // Copy SSAO	
		BladeEdgeDetection edgeDetection = newCameraObj.PasteComponent(playerCam.GetComponent<BladeEdgeDetection>());   // Copy Edge Detection (maybe change color)
		if (!receiver.useCameraEdgeDetectionColor) {
			edgeDetection.edgeColor = receiver.portalEdgeDetectionColor;
		}
		newCameraObj.PasteComponent(playerCam.GetComponent<ColorfulFog>());                                             // Copy Fog

		// Initialize PortalCameraRenderTexture component
		PortalCameraRenderTexture newPortalCameraRenderTexture = newCameraObj.AddComponent<PortalCameraRenderTexture>();

		// Initialize PortalCameraFollow component
		PortalCameraFollow newPortalCameraFollow = newCameraObj.AddComponent<PortalCameraFollow>();

		// Fix Camera name
		newCameraObj.name = receiver.name + " Camera";
		return newCameraObj;
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
		newTeleporter.teleporter.OnTeleport += SwapEdgeDetectionColorAfterTeleport;
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
			if (i == 1) {
				volumetricPortal.transform.Rotate(volumetricPortal.transform.up * 180);
			}

			Vector3 portalSize = receivers[i].GetComponent<MeshFilter>().mesh.bounds.size;
			Vector3 volumetricBoxSize = volumetricPortal.GetComponent<MeshFilter>().mesh.bounds.size;
			volumetricPortal.transform.localScale = new Vector3(portalSize.x / volumetricBoxSize.x, portalSize.y/volumetricBoxSize.y, 1);

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

	private void SwapEdgeDetectionColorAfterTeleport(Collider teleportEnter, Collider teleportExit, Collider player) {
		PortalSettings portalInfo = teleportEnter.GetComponentInParent<PortalSettings>();
		SwapEdgeDetectionColors(portalInfo.channel);
	}


	private void SwapEdgeDetectionColors(int channel) {
		BladeEdgeDetection playerED = EpitaphScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();
		Transform camerasParent = cameraContainersByChannel[channel].transform;
		BladeEdgeDetection[] portalEDs = camerasParent.GetComponentsInChildrenOnly<BladeEdgeDetection>();

		BladeEdgeDetection temp = playerED;
		playerED = portalEDs[0];
		portalEDs.ToList().ForEach(ed => ed = temp);
	}

}