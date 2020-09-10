using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PortalMechanics;
using MagicTriggerMechanics;

public class TogglePortalRender : MonoBehaviour {
	public DoorOpenClose enableDoor;
	public MagicTrigger disableTrigger;
	Portal portal;

    IEnumerator Start() {
        while (portal == null || portal.otherPortal == null) {
			portal = GetComponent<Portal>();
			yield return null;
		}

		PausePortalRendering();
		enableDoor.OnDoorOpenStart += ctx => ResumePortalRendering();
		disableTrigger.OnMagicTriggerStayOneTime += ctx => PausePortalRendering();
    }

	void ResumePortalRendering() {
		portal.pauseRenderingAndLogic = false;
		portal.otherPortal.pauseRenderingAndLogic = false;
	}

	void PausePortalRendering() {
		portal.pauseRenderingAndLogic = true;
		portal.otherPortal.pauseRenderingAndLogic = true;
	}
}
