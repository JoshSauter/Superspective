using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PortalMechanics {
	[RequireComponent(typeof(Collider))]
	public class PortalCollider : MonoBehaviour {
		public Portal portal;

		private void OnTriggerEnter(Collider other) {
			portal.OnTriggerEnter(other);
		}

		private void OnTriggerStay(Collider other) {
			portal.OnTriggerStay(other);
		}

		private void OnTriggerExit(Collider other) {
			portal.OnTriggerExit(other);
		}
	}
}
