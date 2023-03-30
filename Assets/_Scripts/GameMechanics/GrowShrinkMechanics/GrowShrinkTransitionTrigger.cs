using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using SuperspectiveUtils;

namespace GrowShrink {
	[RequireComponent(typeof(Collider), typeof(Renderer))]
	public class GrowShrinkTransitionTrigger : MonoBehaviour {
		public bool DEBUG = false;
		private DebugLogger debug;
		Bounds bounds;

		public delegate void TransitionTriggerAction(Collider collider, float t);

		public delegate void HallwayEnterAction(Collider collider, bool enteredSmallSide);

		public delegate void HallwayExitAction(Collider collider, bool enteredSmallSide);

		public event TransitionTriggerAction OnTransitionTrigger;
		public event HallwayEnterAction OnHallwayEnter;
		public event HallwayExitAction OnHallwayExit;

		void Start() {
			SetupBoundaries();
			bounds = GetComponent<Renderer>().bounds;
			debug = new DebugLogger(this, () => DEBUG);
		}

		public static string GetId(Collider c) => c.GetComponent<UniqueId>()?.uniqueId ?? c.gameObject.FullPath();

		float GetValue(Vector3 position) {
			Vector3 closestPoint = position.GetClosestPointOnFiniteLine(smallSidePointLocal, bigSidePointLocal);
			return Utils.Vector3InverseLerp(smallSidePointLocal, bigSidePointLocal, closestPoint);
		}

		void OnTriggerEnter(Collider other) {
			string id = GetId(other);
			float i = GetValue(other.transform.position);

			bool enteredSmallSide = i < 0.5f;
			OnHallwayEnter?.Invoke(other, enteredSmallSide);
		}

		void OnTriggerStay(Collider other) {
			string id = GetId(other);
			float i = GetValue(other.transform.position);
			debug.LogWarning($"i for {id}: {i}");
			OnTransitionTrigger?.Invoke(other, i);
		}

		void OnTriggerExit(Collider other) {
			string id = GetId(other);
			float i = GetValue(other.transform.position);

			bool exitedSmallSide = i < 0.5f;
			OnHallwayExit?.Invoke(other, exitedSmallSide);
		}

		private Plane bigSidePlane, smallSidePlane;

		// Stored as local positions, converted to world when used
		private Vector3 bigSidePoint, smallSidePoint;
		private Vector3 bigSidePointLocal => transform.TransformPoint(bigSidePoint);
		private Vector3 smallSidePointLocal => transform.TransformPoint(smallSidePoint);

		void SetupBoundaries() {
			Quaternion rotation = transform.rotation;
			transform.rotation = Quaternion.identity;
			Bounds b = GetComponent<Collider>().bounds;
			Vector3 center = b.center - transform.up * b.extents.y;
			float tunnelExtent = b.extents.z - 0.5f;
			transform.rotation = rotation;

			bigSidePoint = transform.InverseTransformPoint(center - transform.forward * tunnelExtent);
			smallSidePoint = transform.InverseTransformPoint(center + transform.forward * tunnelExtent);

			bigSidePlane = new Plane(transform.forward, bigSidePoint);
			smallSidePlane = new Plane(transform.forward, smallSidePoint);

			Debug.DrawLine(center, bigSidePoint, Color.yellow, 10000);
			Debug.DrawLine(center, smallSidePoint, Color.cyan, 10000);
		}

		void OnDrawGizmosSelected() {
			DrawPlanes(transform.TransformPoint(smallSidePoint), smallSidePlane.normal, 2.5f, 2.5f, Color.cyan);
			DrawPlanes(transform.TransformPoint(bigSidePoint), bigSidePlane.normal, 10f, 10f, Color.yellow);
		}

		void DrawPlanes(Vector3 point, Vector3 normal, float height, float width, Color color) {
			if (!Application.isPlaying) return;
			Quaternion rotation = Quaternion.LookRotation(normal);
			Matrix4x4 trs = Matrix4x4.TRS(point, rotation, Vector3.one);
			Gizmos.matrix = trs;
			Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
			float depth = 0.0001f;
			Gizmos.DrawCube(Vector3.up * height * 0.5f, new Vector3(width, height, depth));
			Gizmos.color = new Color(color.r, color.g, color.b, 1f);
			Gizmos.DrawWireCube(Vector3.up * height * 0.5f, new Vector3(width, height, depth));
			Gizmos.matrix = Matrix4x4.identity;
			Gizmos.color = Color.white;
		}
	}
}