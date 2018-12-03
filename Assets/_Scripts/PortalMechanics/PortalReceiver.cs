using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalReceiver : MonoBehaviour {
	public int channel;

	private void OnEnable() {
		Portal.receiversByChannel.Add(channel, this);
	}

	private void OnDisable() {
		Portal.receiversByChannel.Remove(channel);
	}
}
