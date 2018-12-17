using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using EpitaphUtils;
using UnityStandardAssets.ImageEffects;

public class PortalManager : Singleton<PortalManager> {
	Dictionary<int, HashSet<PortalReceiver>> receiversByChannel = new Dictionary<int, HashSet<PortalReceiver>>();
	Dictionary<int, GameObject> cameraContainersByChannel = new Dictionary<int, GameObject>();

	/// <summary>
	/// Adds a receiver to the portal dictionary for this channel, and if the portal has two receivers, instantiate the portal
	/// </summary>
	/// <param name="channel"></param>
	/// <param name="receiver"></param>
	public void AddReceiver(int channel, PortalReceiver receiver) {
		if (!receiversByChannel.ContainsKey(channel)) {
			receiversByChannel[channel] = new HashSet<PortalReceiver>();
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
	public bool RemoveReceiver(int channel, PortalReceiver receiver) {
		if (!receiversByChannel.ContainsKey(channel)) {
			Debug.LogWarning("Trying to remove a receiver for non-existent channel: " + channel);
			return false;
		}

		bool receiverRemoved = receiversByChannel[channel].Remove(receiver);
		if (receiverRemoved) {
			print("Disabling Portal for channel " + channel);
			// TODO: Disable portal for channel
		}
		return receiverRemoved;
	}

#region PortalInitialization
	private void InitializePortalsForChannel(int channel) {
		// Create a Portal Camera which renders to a RenderTexture for each Receiver in the channel
		InitializePortalCameras(channel);

		// Create the Portal Teleporters on each PortalReceiver
		InitializePortalTeleporters(channel);
	}

	////////////////////
	// Portal Cameras //
	////////////////////
	private void InitializePortalCameras(int channel) {
		// Create parent object to act as a container for the portal cameras
		GameObject newCameraContainer = new GameObject("Channel" + channel + " Cameras");
		newCameraContainer.transform.SetParent(transform);

		// Create a new camera for each receiver
		List<PortalReceiver> receiversInChannel = receiversByChannel[channel].ToList();
		for (int i = 0; i < receiversInChannel.Count; i++) {
			InitializePortalCamera(newCameraContainer.transform, receiversInChannel[i], receiversInChannel[(i + 1) % receiversInChannel.Count]);
		}

		cameraContainersByChannel.Add(channel, newCameraContainer);
	}

	private void InitializePortalCamera(Transform parentObj, PortalReceiver receiver, PortalReceiver otherPortal) {
		GameObject playerCam = Camera.main.gameObject;

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
		newPortalCameraRenderTexture.portal = receiver;

		// Initialize PortalCameraFollow component
		PortalCameraFollow newPortalCameraFollow = newCameraObj.AddComponent<PortalCameraFollow>();
		newPortalCameraFollow.portalBeingRendered = receiver;
		newPortalCameraFollow.otherPortal = otherPortal;

		// Fix Camera name
		newCameraObj.name = receiver.name + " Camera";
	}

	////////////////////////
	// Portal Teleporters //
	////////////////////////
	private void InitializePortalTeleporters(int channel) {
		// Create a teleporter for each side of the portal
		List<PortalTeleporter> portalTeleporters = receiversByChannel[channel].Select(r => CreatePortalTeleporter(r)).ToList();
		portalTeleporters[0].otherPortalTeleporter = portalTeleporters[1];
		portalTeleporters[1].otherPortalTeleporter = portalTeleporters[0];
	}

	private PortalTeleporter CreatePortalTeleporter(PortalReceiver receiver) {
		GameObject newTeleporterObj = new GameObject("PortalTeleporterTrigger");
		newTeleporterObj.transform.SetParent(receiver.transform, false);
		
		PortalTeleporter newTeleporter = newTeleporterObj.AddComponent<PortalTeleporter>();
		newTeleporter.portal = receiver;
		newTeleporter.teleporter.OnTeleport += SwapEdgeDetectionColorAfterTeleport;
		return newTeleporter;
	}
	#endregion

	private void SwapEdgeDetectionColorAfterTeleport(Collider teleportEnter, Collider teleportExit, Collider player) {
		PortalReceiver portalInfo = teleportEnter.GetComponentInParent<PortalReceiver>();
		SwapEdgeDetectionColors(portalInfo.channel);
	}


	private void SwapEdgeDetectionColors(int channel) {
		BladeEdgeDetection playerED = EpitaphScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();
		Transform camerasParent = cameraContainersByChannel[channel].transform;
		BladeEdgeDetection[] portalEDs = camerasParent.GetComponentsInChildrenOnly<BladeEdgeDetection>();

		Color temp = playerED.edgeColor;
		playerED.edgeColor = portalEDs[0].edgeColor;
		portalEDs.ToList().ForEach(ed => ed.edgeColor = temp);
	}

}