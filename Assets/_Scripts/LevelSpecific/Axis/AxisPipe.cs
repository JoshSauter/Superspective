using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.Axis {
	public class AxisPipe : MonoBehaviour {
		public float rotateSpeed = 45;
		public float speed = 5;
		public float distanceToTravelBeforeDespawn = 448;

		float distanceTravelled = 0;
		Vector3 forwardDirection = Vector3.forward;
		Vector3 axisOfRotation = Vector3.up;

		public bool stopped = false;
		enum AxisPipeState {
			rotating,
			moving,
			rotatingAndMoving
		}
		AxisPipeState state;

		AxisPipe pipeInContact;

		// Use this for initialization
		void Start() {
			state = AxisPipeState.rotatingAndMoving;
		}

		// Update is called once per frame
		void FixedUpdate() {
			if (Mathf.Abs(distanceTravelled) > distanceToTravelBeforeDespawn) {
				Destroy(gameObject);
			}
			if (stopped) return;

			switch (state) {
				case AxisPipeState.rotatingAndMoving:
					Rotate();
					Move();
					break;
				case AxisPipeState.moving:
					Move();
					break;
				case AxisPipeState.rotating:
					Rotate();
					if (pipeInContact != null) {
						float angleBetween = Quaternion.Angle(pipeInContact.transform.rotation, transform.rotation);
						float closeEnoughCutoff = 2;
						if ((angleBetween + closeEnoughCutoff) % 60 < closeEnoughCutoff) {
							pipeInContact.state = state = AxisPipeState.moving;
						}
					}
					break;
			}


		}

		void Rotate() {
			transform.Rotate(axisOfRotation, Time.fixedDeltaTime * rotateSpeed);
		}
		void Move() {
			float distance = Time.fixedDeltaTime * speed;
			transform.position += forwardDirection * distance;
			distanceTravelled += distance;
		}

		void OnTriggerEnter(Collider other) {
			pipeInContact = other.gameObject.GetComponent<AxisPipe>();
			if (pipeInContact != null) {
				pipeInContact.state = state = AxisPipeState.rotating;
			}
		}

		void OnTriggerExit(Collider other) {
			if (pipeInContact != null) {
				pipeInContact.state = state = AxisPipeState.rotatingAndMoving;
			}
			pipeInContact = null;
		}
	}
}