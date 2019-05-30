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

	public void OnLeftMouseButton() {
		if (isActive) {
			Angle nextAngle = GetAngleOfMouse();
			Angle diff = Angle.WrappedAngleDiff(nextAngle, prevAngle);

			transform.Rotate(Vector3.down * diff.degrees);

			prevAngle = GetAngleOfMouse();

			if (Mathf.Abs(diff.radians) > 0 && OnValveRotate != null) {
				OnValveRotate(diff);
			}
		}
	}

	public void OnLeftMouseButtonDown() {
		isActive = true;
		playerLook.outsideMultiplier = lookSpeedMultiplier;
		prevAngle = GetAngleOfMouse();
	}
	public void OnLeftMouseButtonUp() {
		playerLook.outsideMultiplier = 1;
		isActive = false;
		prevAngle = null;
	}

	public void OnLeftMouseButtonFocusLost() {
		playerLook.outsideMultiplier = 1;
		isActive = false;
		prevAngle = null;
	}

	Angle GetAngleOfMouse() {
		Vector3 mouseLocation = Interact.instance.GetRaycastHit().point;
		Vector3 localMouseLocation = transform.InverseTransformPoint(mouseLocation);
		return PolarCoordinate.CartesianToPolar(localMouseLocation).angle;
	}
}
