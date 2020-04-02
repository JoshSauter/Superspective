using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using System.Linq;
using UnityStandardAssets.ImageEffects;

public class NewPortalManager : Singleton<NewPortalManager> {
	public bool DEBUG = false;
	public LayerMask hideFromPortalCamera;
	DebugLogger debug;
	Dictionary<string, HashSet<Portal>> portalsByChannel = new Dictionary<string, HashSet<Portal>>();
	public List<Portal> activePortals = new List<Portal>();
	Camera portalCamera;

	GameObject volumetricPortalPrefab;

	private void Awake() {
		debug = new DebugLogger(this, () => DEBUG);
		volumetricPortalPrefab = Resources.Load<GameObject>("Prefabs/VolumetricPortal");
	}

	private void Start() {
		InitializeVirtualPortalCamera();
	}

	/// <summary>
	/// Adds a portal to the portal dictionary for this channel, and if the channel has two portals, enable the portal
	/// </summary>
	/// <param name="channelName"></param>
	/// <param name="portal"></param>
	public void AddPortal(string channelName, Portal portal) {
		if (!portalsByChannel.ContainsKey(channelName)) {
			portalsByChannel[channelName] = new HashSet<Portal>();
		}

		if (portalsByChannel[channelName].Count == 2) {
			debug.LogError("Channel " + channelName + " already has two portals! Check the channels for the following portals:\n" +
				string.Join("\n", portalsByChannel[channelName].Select(p => p.name).ToArray()) + "\n" + portal.name);
			return;
		}

		portalsByChannel[channelName].Add(portal);

		if (portalsByChannel[channelName].Count == 2) {
			debug.Log("Enabling portals for channel: " + channelName);
			EnablePortalsForChannel(portalsByChannel[channelName]);
			foreach (var activePortal in portalsByChannel[channelName]) {
				activePortals.Add(activePortal);
			}
		}
	}

	/// <summary>
	/// Removes a portal from the portal dictionary, and disables portal
	/// </summary>
	/// <param name="channelName"></param>
	/// <param name="portal"></param>
	/// <returns>Returns true if the portal was successfully found and removed, false otherwise</returns>
	public bool RemovePortal(string channelName, Portal portal) {
		if (!portalsByChannel.ContainsKey(channelName)) {
			debug.LogWarning("Trying to remove a receiver for non-existent channel: " + channelName);
			return false;
		}

		bool receiverRemoved = portalsByChannel[channelName].Remove(portal);
		activePortals.Remove(portal);
		if (receiverRemoved && portalsByChannel[channelName].Count == 1) {
			debug.Log("Disabling portal for channel " + channelName);
			DisablePortalsForChannel(portalsByChannel[channelName]);
			foreach (var inactivePortal in portalsByChannel[channelName]) {
				activePortals.Remove(inactivePortal);
			}
		}
		return receiverRemoved;
	}

	#region PortalEnableDisable
	private void EnablePortalsForChannel(HashSet<Portal> portals) {
		foreach (var portal in portals) {
			Portal otherPortal = portals.First(x => x != portal);
			portal.EnablePortal(otherPortal);
		}
	}

	private void DisablePortalsForChannel(HashSet<Portal> portals) {
		foreach (var portal in portals) {
			portal.DisablePortal();
		}
	}
	#endregion

	/// <summary>
	/// Instantiates portalCamera as a copy of the player's Camera with post-processing effects copied, as a child of the PortalManager, and disables the Camera.
	/// </summary>
	private void InitializeVirtualPortalCamera() {
		Camera playerCam = EpitaphScreen.instance.playerCamera;
		portalCamera = new GameObject("VirtualPortalCamera").AddComponent<Camera>();
		portalCamera.transform.SetParent(transform, false);
		portalCamera.enabled = false;

		// Copy main camera component from player's camera
		portalCamera.CopyFrom(playerCam);
		portalCamera.cullingMask &= ~hideFromPortalCamera;

		portalCamera.gameObject.AddComponent<VirtualPortalCamera>();

		// Copy post-process effects from player's camera
		// Order of components here matters; it affects the rendering order of the postprocess effects
		portalCamera.gameObject.PasteComponent(playerCam.GetComponent<BloomOptimized>());                                          // Copy Bloom
		portalCamera.gameObject.PasteComponent(playerCam.GetComponent<ScreenSpaceAmbientOcclusion>());                             // Copy SSAO
		BladeEdgeDetection edgeDetection = portalCamera.gameObject.PasteComponent(playerCam.GetComponent<BladeEdgeDetection>());   // Copy Edge Detection (maybe change color)
		portalCamera.gameObject.PasteComponent(playerCam.GetComponent<ColorfulFog>());                                             // Copy Fog

		portalCamera.gameObject.name = "VirtualPortalCamera";
	}
}
