using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

namespace LevelSpecific.BlackRoom {
	public class ValveControl : MonoBehaviour {
		InteractableObject interactableObject;
		PlayerLook playerLook;
		float lookSpeedMultiplier = 0.5f;
		bool isActive = false;
		Angle prevAngle;

		public delegate void ValveRotate(Angle diff);
		public event ValveRotate OnValveRotate;

		public void Awake() {
			interactableObject = GetComponent<InteractableObject>();
			if (interactableObject == null) {
				interactableObject = gameObject.AddComponent<InteractableObject>();
				interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;
			}
		}

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

		public void OnLeftMouseButtonDown() {
			isActive = true;
			playerLook.outsideMultiplier = lookSpeedMultiplier;
			Interact.instance.enabled = false;
			prevAngle = GetAngleOfMouse();
		}

		Angle GetAngleOfMouse() {
			Vector3 mouseLocation = Interact.instance.GetRaycastHits().lastRaycast.hitInfo.point;
			Vector3 localMouseLocation = transform.InverseTransformPoint(mouseLocation);
			return PolarCoordinate.CartesianToPolar(localMouseLocation).angle;
		}
	}
}