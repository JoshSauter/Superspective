using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using Saving;
using System;
using SerializableClasses;

namespace LevelSpecific.BlackRoom {
	public class ValveControl : MonoBehaviour, SaveableObject {
		const float lookSpeedMultiplier = 0.5f;
		InteractableObject interactableObject;
		PlayerLook playerLook;
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

		#region Saving
		public bool SkipSave { get { return !gameObject.activeInHierarchy; } set { } }

		public string ID => $"{transform.parent.name}_ValveControl";

		[Serializable]
		class ValveControlSave {
			SerializableQuaternion rotation;
			bool isActive;
			Angle prevAngle;

			public ValveControlSave(ValveControl toggle) {
				this.rotation = toggle.transform.rotation;
				this.isActive = toggle.isActive;
				this.prevAngle = toggle.prevAngle;
			}

			public void LoadSave(ValveControl toggle) {
				toggle.transform.rotation = this.rotation;
				toggle.isActive = this.isActive;
				toggle.prevAngle = this.prevAngle;
			}
		}

		public object GetSaveObject() {
			return new ValveControlSave(this);
		}

		public void LoadFromSavedObject(object savedObject) {
			ValveControlSave save = savedObject as ValveControlSave;

			save.LoadSave(this);
		}
		#endregion
	}
}