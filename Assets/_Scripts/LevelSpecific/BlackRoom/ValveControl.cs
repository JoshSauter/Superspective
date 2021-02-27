using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using Saving;
using System;
using SerializableClasses;

namespace LevelSpecific.BlackRoom {
	public class ValveControl : SaveableObject<ValveControl, ValveControl.ValveControlSave> {
		const float lookSpeedMultiplier = 0.5f;
		InteractableObject interactableObject;
		PlayerLook playerLook;
		bool isActive = false;
		Angle prevAngle;

		public delegate void ValveRotate(Angle diff);
		public event ValveRotate OnValveRotate;

		protected override void Awake() {
			base.Awake();
			interactableObject = GetComponent<InteractableObject>();
			if (interactableObject == null) {
				interactableObject = gameObject.AddComponent<InteractableObject>();
				interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;
			}
		}

		protected override void Start() {
			base.Start();
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
		public override bool SkipSave { get { return !gameObject.activeInHierarchy; } set { } }

		public override string ID => $"{transform.parent.name}_ValveControl";

		[Serializable]
		public class ValveControlSave : SerializableSaveObject<ValveControl> {
			SerializableQuaternion rotation;
			bool isActive;
			Angle prevAngle;

			public ValveControlSave(ValveControl toggle) : base(toggle) {
				this.rotation = toggle.transform.rotation;
				this.isActive = toggle.isActive;
				this.prevAngle = toggle.prevAngle;
			}

			public override void LoadSave(ValveControl toggle) {
				toggle.transform.rotation = this.rotation;
				toggle.isActive = this.isActive;
				toggle.prevAngle = this.prevAngle;
			}
		}
		#endregion
	}
}