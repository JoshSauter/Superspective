using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;

[RequireComponent(typeof(Collider))]
public class TransitionTrigger : MonoBehaviour {
	Bounds bounds;
	Dictionary<Collider, Vector3> startPositions = new Dictionary<Collider, Vector3>();
	Dictionary<Collider, Vector3> endPositions = new Dictionary<Collider, Vector3>();

	public delegate void TransitionTriggerAction(Collider collider, float t);
	public event TransitionTriggerAction OnTransitionTrigger;

    void Start() {
		bounds = GetComponent<Renderer>().bounds;
    }

    void OnTriggerEnter(Collider other) {

		if (!startPositions.ContainsKey(other)) {
			Vector3 startPos = transform.InverseTransformPoint(other.transform.position);	// Store start position as local, read out as world
			startPositions.Add(other, startPos);
		}
		if (!endPositions.ContainsKey(other)) {
			Vector3 center = bounds.center;
			center.y = other.transform.position.y;
			Vector3 endPos = transform.InverseTransformPoint(2 * center - other.transform.position);	// Store end position as local, read out as world
			endPositions.Add(other, endPos);
		}
		//GameObject go1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		//go1.transform.localScale = Vector3.one * 0.1f;
		//go1.transform.position = transform.TransformPoint(startPositions[other]);
		//go1.GetComponent<Collider>().enabled = false;
		//go1.transform.parent = transform;
		//GameObject go2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		//go2.transform.localScale = Vector3.one * 0.1f;
		//go2.transform.position = transform.TransformPoint(endPositions[other]);
		//go2.GetComponent<Collider>().enabled = false;
		//go2.transform.parent = transform;
	}

    void OnTriggerStay(Collider other) {
		if (startPositions.ContainsKey(other) && endPositions.ContainsKey(other)) {
			Vector3 start = transform.TransformPoint(startPositions[other]);
			Vector3 end = transform.TransformPoint(endPositions[other]);
			Vector3 currentPos = other.transform.position;
			if (OnTransitionTrigger != null) {
				OnTransitionTrigger(other, Utils.Vector3InverseLerp(start, end, currentPos));
			}
		}
	}

    void OnTriggerExit(Collider other) {
		if (startPositions.ContainsKey(other) && endPositions.ContainsKey(other)) {
			if (OnTransitionTrigger != null) {
				Vector3 start = transform.TransformPoint(startPositions[other]);
				Vector3 end = transform.TransformPoint(endPositions[other]);
				Vector3 currentPos = other.transform.position;
				float finalAmount = Utils.Vector3InverseLerp(start, end, currentPos) < 0.5f ? 0 : 1;
				OnTransitionTrigger(other, finalAmount);
			}

			//startPositions.Remove(other);
			//endPositions.Remove(other);
		}
	}
}
