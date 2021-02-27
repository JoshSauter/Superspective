using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;

public class OutOfBoundsTrigger : MonoBehaviour {
	GameObject outOfBoundsCanvas;

	void Start() {
		outOfBoundsCanvas = MainCanvas.instance.transform.Find("OutOfBoundsCanvas").gameObject;
	}

	void OnTriggerEnter(Collider other) {
		if (other.TaggedAsPlayer()) {
			outOfBoundsCanvas.SetActive(true);
		}
	}

	void OnTriggerExit(Collider other) {
		if (other.TaggedAsPlayer()) {
			outOfBoundsCanvas.SetActive(false);
		}
	}
}
