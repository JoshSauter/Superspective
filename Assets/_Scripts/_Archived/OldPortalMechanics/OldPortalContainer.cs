using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OldPortalMechanics {
	public class OldPortalContainer : MonoBehaviour {
		public OldPortalContainer otherPortal;

		public Camera portalCamera;             // Should be shared with otherPortal.portalCamera
		public OldPortalSettings settings;
		public OldPortalTeleporter teleporter;
		public OldVolumetricPortalTrigger volumetricPortalTrigger;
		public GameObject volumetricPortal;
	}
}
