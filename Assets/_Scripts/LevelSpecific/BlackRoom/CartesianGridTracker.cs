using SuperspectiveUtils;
using SwizzleUtils;
using UnityEngine;

namespace LevelSpecific.BlackRoom {
	public class CartesianGridTracker : MonoBehaviour {
		public Vector2 min;
		public Vector2 max;

		LightProjector projector;
		ProjectorControls projectorControls;
		public InteractableObject gridInteractableObj;

		void Start() {
			projectorControls = transform.parent.GetComponentInParent<ProjectorControls>();
			projector = projectorControls.projector;

			gridInteractableObj.enabledHelpText = "Guide fixture";
			gridInteractableObj.OnLeftMouseButton += GuideFixture;
		}

		void Update() {
			float tx = projector.curSideToSideAnimTime;
			float ty = projector.curUpAndDownAnimTime;
			transform.localPosition = new Vector2(Mathf.Lerp(min.x, max.x, tx), Mathf.Lerp(min.y, max.y, ty));
		}

		bool skipFrame = false;
		void GuideFixture() {
			Vector2 curPos = transform.localPosition.xy();
			SuperspectiveRaycast raycast = Interact.instance.GetRaycastHits();
			Vector3 pointOfCollision = raycast.allObjectHits.Find(raycastHit => raycastHit.collider.gameObject == gridInteractableObj.gameObject).point;
			Vector3 posToRestore = transform.localPosition;
			transform.position = pointOfCollision;
			Vector2 desiredPos = transform.localPosition.xy();
			transform.localPosition = posToRestore;

			Vector2 delta = desiredPos - curPos;
			const float threshold = 0.01f;
			if (Mathf.Abs(delta.x) > threshold && !skipFrame) {
				if (delta.x > 0) {
					projectorControls.RotateProjectorRightOnAxis();
				}
				else {
					projectorControls.RotateProjectorLeftOnAxis();
				}

				skipFrame = true;
			} else if (skipFrame) {
				// Only move left/right half as often because it animates much faster. hack
				skipFrame = false;
			}

			if (Mathf.Abs(delta.y) > threshold) {
				if (delta.y > 0) {
					projectorControls.RotateProjectorUpOnAxis();
				}
				else {
					projectorControls.RotateProjectorDownOnAxis();
				}
			}
		}
	}
}