using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface InteractableObject {
	void OnLeftMouseButtonDown();
	void OnLeftMouseButton();
	void OnLeftMouseButtonUp();
	void OnLeftMouseButtonFocusLost();
}
