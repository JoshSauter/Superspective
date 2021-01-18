using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PortalMechanics {
	[RequireComponent(typeof(Collider))]
	public class PortalCollider : MonoBehaviour {
		public Portal portal;

		void OnTriggerEnter(Collider other) {
			portal.OnTriggerEnter(other);
		}

		void OnTriggerStay(Collider other) {
			portal.OnTriggerStay(other);
		}

		void OnTriggerExit(Collider other) {
			portal.OnTriggerExit(other);
		}
	}
}
