using EpitaphUtils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModifyInteractDistance : MonoBehaviour {
    public float desiredInteractDistance = 8f;

	private void OnTriggerStay(Collider other) {
		if (other.TaggedAsPlayer()) {
			Interact.instance.interactionDistance = desiredInteractDistance;
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.TaggedAsPlayer()) {
			Interact.instance.interactionDistance = Interact.defaultInteractionDistance;
		}
	}
}
