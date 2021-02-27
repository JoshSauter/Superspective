using SuperspectiveUtils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PortalMechanics {
    public class ReflectorPortal : Portal {
        public Transform outTransform;

		// Reflector portals should add themselves twice to the PortalManager; there is no "other" portal to wait for registration
		protected override IEnumerator AddPortalCoroutine() {
            while (!gameObject.scene.isLoaded) {
                //debug.Log("Waiting for scene " + gameObject.scene + " to be loaded before adding receiver...");
                yield return null;
            }

            PortalManager.instance.AddPortal(channel, this, 1);
        }

        protected override void OnDisable() {
            PortalManager.instance.RemovePortal(channel, this, 1);
        }

        public override Vector3 TransformPoint(Vector3 point) {
            Vector3 relativeObjPos = transform.InverseTransformPoint(point);
            relativeObjPos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeObjPos;
            return outTransform.TransformPoint(relativeObjPos);
        }

		public override Vector3 TransformDirection(Vector3 direction) {
            Vector3 relativeDir = Quaternion.Inverse(transform.rotation) * direction;
            relativeDir = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeDir;
            return outTransform.rotation * relativeDir;
        }

		public override Quaternion TransformRotation(Quaternion rotation) {
            Quaternion relativeRot = Quaternion.Inverse(transform.rotation) * rotation;
            relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
            return outTransform.rotation * relativeRot;
        }
	}
}
