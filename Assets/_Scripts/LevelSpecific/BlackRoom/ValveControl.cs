using UnityEngine;
using SuperspectiveUtils;
using Saving;
using System;
using SerializableClasses;

namespace LevelSpecific.BlackRoom {
	[RequireComponent(typeof(UniqueId))]
	public class ValveControl : SuperspectiveObject<ValveControl, ValveControl.ValveControlSave> {
		const float LOOK_SPEED_MULTIPLIER = 0.5f;
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
			}
			interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;
		}

		protected override void Start() {
			base.Start();
			playerLook = PlayerLook.instance;
		}

		void Update() {
			if (isActive) {
				if (PlayerButtonInput.instance.InteractHeld) {
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
			playerLook.outsideMultiplier = LOOK_SPEED_MULTIPLIER;
			Interact.instance.enabled = false;
			prevAngle = GetAngleOfMouse();
		}

		Angle GetAngleOfMouse() {
			SuperspectiveRaycast raycast = Interact.instance.GetRaycastHits();
			Vector3 mouseLocation = raycast.DidHitObject ? raycast.FirstObjectHit.point : raycast.FinalPosition;
			Vector3 localMouseLocation = transform.InverseTransformPoint(mouseLocation);
			return PolarCoordinate.CartesianToPolar(localMouseLocation).angle;
		}

#region Saving

		public override void LoadSave(ValveControlSave save) {
			transform.rotation = save.rotation;
			isActive = save.isActive;
			prevAngle = save.prevAngle;
		}

		public override bool SkipSave => !gameObject.activeInHierarchy;

		public override string ID => $"{transform.parent.name}_ValveControl";

		[Serializable]
		public class ValveControlSave : SaveObject<ValveControl> {
			public SerializableQuaternion rotation;
			public bool isActive;
			public Angle prevAngle;

			public ValveControlSave(ValveControl toggle) : base(toggle) {
				this.rotation = toggle.transform.rotation;
				this.isActive = toggle.isActive;
				this.prevAngle = toggle.prevAngle;
			}
		}
#endregion
	}
}