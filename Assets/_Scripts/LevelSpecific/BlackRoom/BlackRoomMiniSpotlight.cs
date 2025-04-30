using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class BlackRoomMiniSpotlight : SuperspectiveObject<BlackRoomMiniSpotlight, BlackRoomMiniSpotlight.BlackRoomMiniSpotlightSave> {
	public Renderer lightSource;
	public Renderer lightBeam;

	[SerializeField]
	private bool isOn = false;
    
    public void TurnOff(bool force = false) {
	    if (isOn || force) {
		    isOn = false;
		    SetLights(isOn);
	    }
    }

    public void TurnOn() {
	    if (!isOn) {
		    isOn = true;
		    SetLights(isOn);
		    AudioManager.instance.PlayAtLocation(AudioName.LightSwitch, "BlackRoom_MainConsole", lightSource.transform.position);
	    }
    }

    void SetLights(bool on) {
	    lightSource.GetOrAddComponent<SuperspectiveRenderer>().SetInt("_EmissionEnabled", on ? 1 : 0);
	    lightBeam.enabled = on;
    }
    
#region Saving

	public override void LoadSave(BlackRoomMiniSpotlightSave save) {
		SetLights(isOn);
	}

	[Serializable]
	public class BlackRoomMiniSpotlightSave : SaveObject<BlackRoomMiniSpotlight> {
		public BlackRoomMiniSpotlightSave(BlackRoomMiniSpotlight script) : base(script) { }
	}
#endregion
}
