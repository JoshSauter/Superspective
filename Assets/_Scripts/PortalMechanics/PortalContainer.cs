using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalContainer : MonoBehaviour {
	public PortalContainer otherPortal;

    public Camera portalCamera;				// Should be shared with otherPortal.portalCamera
	public PortalSettings settings;
	public PortalTeleporter teleporter;
	public VolumetricPortalTrigger volumetricPortalTrigger;
	public GameObject volumetricPortal;
}
