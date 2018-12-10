using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class OutOfBoundsTrigger : MonoBehaviour {
	GameObject outOfBoundsCanvas;

	private void Start() {
		outOfBoundsCanvas = MainCanvas.instance.transform.Find("OutOfBoundsCanvas").gameObject;
	}

	private void OnTriggerEnter(Collider other) {
		if (other.tag.TaggedAsPlayer()) {
			outOfBoundsCanvas.SetActive(true);
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.tag.TaggedAsPlayer()) {
			outOfBoundsCanvas.SetActive(false);
		}
	}
}
