using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TogglePortalRender : MonoBehaviour {
	public DoorOpenClose enableDoor;
	public MagicTrigger disableTrigger;
	PortalCameraRenderTexture portal;

    IEnumerator Start() {
        while (portal == null) {
			portal = GetComponent<PortalCameraRenderTexture>();
			yield return null;
		}

		portal.pauseRendering = true;
		enableDoor.OnDoorOpenStart += ctx => ResumePortalRendering();
		disableTrigger.OnMagicTriggerStayOneTime += ctx => PausePortalRendering();
    }

	void ResumePortalRendering() {
		portal.pauseRendering = false;
	}

	void PausePortalRendering() {
		portal.pauseRendering = true;
	}
}
