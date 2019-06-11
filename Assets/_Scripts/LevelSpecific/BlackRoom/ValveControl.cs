using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class ValveControl : MonoBehaviour, InteractableObject {
	PlayerLook playerLook;
	float lookSpeedMultiplier = 0.5f;
	bool isActive = false;
	Angle prevAngle;

	public delegate void ValveRotate(Angle diff);
	public event ValveRotate OnValveRotate;

    void Start() {
		playerLook = PlayerLook.instance;
    }

	void Update() {
		if (isActive) {
			if (PlayerButtonInput.instance.Action1Held) {
				Angle nextAngle = GetAngleOfMouse();
				Angle diff = Angle.WrappedAngleDiff(nextAngle, prevAngle);

				transform.Rotate(Vector3.down * diff.degrees);

				prevAngle = GetAngleOfMouse();

				if (Mathf.Abs(diff.radians) > 0 && OnValveRotate != null) {
					OnValveRotate(diff);
				}
			}
			else {
				playerLook.outsideMultiplier = 1;
				isActive = false;
				prevAngle = null;
				Interact.instance.enabled = true;
			}
		}
	}

	public void OnLeftMouseButton() {}

	public void OnLeftMouseButtonDown() {
		isActive = true;
		playerLook.outsideMultiplier = lookSpeedMultiplier;
		Interact.instance.enabled = false;
		prevAngle = GetAngleOfMouse();
	}
	public void OnLeftMouseButtonUp() {}

	public void OnLeftMouseButtonFocusLost() {}

	Angle GetAngleOfMouse() {
		Vector3 mouseLocation = Interact.instance.GetRaycastHit().point;
		Vector3 localMouseLocation = transform.InverseTransformPoint(mouseLocation);
		return PolarCoordinate.CartesianToPolar(localMouseLocation).angle;
	}
}
