using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutOfBoundsTrigger : MonoBehaviour {
	GameObject outOfBoundsCanvas;

	private void Start() {
		outOfBoundsCanvas = MainCanvas.instance.transform.Find("OutOfBoundsCanvas").gameObject;
	}

	private void OnTriggerEnter(Collider other) {
		if (other.tag == "Player") {
			outOfBoundsCanvas.SetActive(true);
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.tag == "Player") {
			outOfBoundsCanvas.SetActive(false);
		}
	}
}
