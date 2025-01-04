using UnityEngine;

namespace PortalMechanics {
    public class ReflectorPortal : Portal {
        public Transform outTransform;

        protected override int PortalsRequiredToActivate => 1;

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
