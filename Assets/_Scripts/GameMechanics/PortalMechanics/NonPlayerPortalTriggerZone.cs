using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PortalMechanics {
	/// <summary>
	/// PortalColliders handle non-player PortalableObjects entering the portal
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class NonPlayerPortalTriggerZone : MonoBehaviour {
		public Portal portal;

		void OnTriggerEnter(Collider other) {
			portal.OnPortalTriggerEnter(other);
		}

		void OnTriggerStay(Collider other) {
			portal.OnPortalTriggerStay(other);
		}

		void OnTriggerExit(Collider other) {
			portal.OnPortalTriggerExit(other);
		}
	}
}
