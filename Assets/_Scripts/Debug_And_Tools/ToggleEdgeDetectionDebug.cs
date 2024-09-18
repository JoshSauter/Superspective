using UnityEngine;

public class ToggleEdgeDetectionDebug : Singleton<ToggleEdgeDetectionDebug> {
	void Update() {
		if (DebugInput.GetKey(KeyCode.LeftShift) && DebugInput.GetKeyDown(KeyCode.B)) {
			ToggleDebugMode();
		}
	}
	
	void ToggleDebugMode () {
		BladeEdgeDetection ed = SuperspectiveScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();
		if (ed != null) {
			ed.debugMode = !ed.debugMode;
		}
	}
}
