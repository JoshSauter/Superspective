using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleEdgeDetectionDebug : Singleton<ToggleEdgeDetectionDebug> {
	void Update() {
		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.B)) {
			ToggleDebugMode();
		}
		if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.B)) {
			ToggleDoubleSidedEdges();
		}
	}
	
	void ToggleDebugMode () {
		BladeEdgeDetection ed = EpitaphScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();
		if (ed != null) {
			ed.debugMode = !ed.debugMode;
		}
	}

	void ToggleDoubleSidedEdges() {
		BladeEdgeDetection ed = EpitaphScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();
		if (ed != null) {
			ed.doubleSidedEdges = !ed.doubleSidedEdges;
		}
	}
}
