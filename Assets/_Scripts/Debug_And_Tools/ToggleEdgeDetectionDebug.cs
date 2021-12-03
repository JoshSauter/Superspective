using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleEdgeDetectionDebug : Singleton<ToggleEdgeDetectionDebug> {
	void Update() {
		if (DebugInput.GetKey(KeyCode.LeftShift) && DebugInput.GetKeyDown(KeyCode.B)) {
			ToggleDebugMode();
		}
		if (DebugInput.GetKey(KeyCode.LeftShift) && DebugInput.GetKeyDown(KeyCode.V)) {
			ToggleDoubleSidedEdges();
		}
	}
	
	void ToggleDebugMode () {
		BladeEdgeDetection ed = SuperspectiveScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();
		if (ed != null) {
			ed.debugMode = !ed.debugMode;
		}
	}

	void ToggleDoubleSidedEdges() {
		BladeEdgeDetection ed = SuperspectiveScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();
		if (ed != null) {
			ed.doubleSidedEdges = !ed.doubleSidedEdges;
		}
	}
}
