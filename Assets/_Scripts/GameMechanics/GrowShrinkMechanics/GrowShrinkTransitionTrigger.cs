using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using PortalMechanics;
using UnityEngine;
using SuperspectiveUtils;

namespace GrowShrink {
	[RequireComponent(typeof(BetterTrigger))]
	public class GrowShrinkTransitionTrigger : MonoBehaviour, BetterTriggers {
		public bool DEBUG = false;
		private DebugLogger debug;
		Bounds bounds;

		private Dictionary<Collider, Vector3> positionsLastFrame = new Dictionary<Collider, Vector3>();

		public delegate void TransitionTriggerAction(Collider collider, float t);

		public delegate void HallwayEnterAction(Collider collider, bool enteredSmallSide);

		public delegate void HallwayExitAction(Collider collider, bool enteredSmallSide);

		public event TransitionTriggerAction OnTransitionTrigger;
		public event HallwayEnterAction OnHallwayEnter;
		public event HallwayExitAction OnHallwayExit;

		void Start() {
			SetupBoundaries(GetComponent<Collider>());
			bounds = GetComponent<Renderer>().bounds;
			debug = new DebugLogger(this, () => DEBUG);
		}

		public static string GetId(Collider c) => c.GetComponent<UniqueId>()?.uniqueId ?? c.gameObject.FullPath();

		float GetValue(Vector3 position) {
			Vector3 closestPoint = position.GetClosestPointOnFiniteLine(smallSidePointWorld, largeSidePointWorld);
			return Utils.Vector3InverseLerp(smallSidePointWorld, largeSidePointWorld, closestPoint);
		}

		public void OnBetterTriggerEnter(Collider other) {
			string id = GetId(other);
			float i = GetValue(other.transform.position);

			bool enteredSmallSide = i < 0.5f;
			debug.LogWarning($"{id} {(enteredSmallSide ? "entered small side" : "entered large side")}");
			positionsLastFrame[other] = other.transform.position;
			OnHallwayEnter?.Invoke(other, enteredSmallSide);
		}

		public void OnBetterTriggerStay(Collider other) {
			string id = GetId(other);
			float i = GetValue(other.transform.position);

			debug.LogWarning($"i for {id}: {i}");
			positionsLastFrame[other] = other.transform.position;
			OnTransitionTrigger?.Invoke(other, i);
		}

		public void OnBetterTriggerExit(Collider other) {
			string id = GetId(other);
			float i = GetValue(positionsLastFrame[other]);
			positionsLastFrame.Remove(other);

			bool exitedSmallSide = i < 0.5f;
			debug.LogWarning($"{id} {(exitedSmallSide ? "exited small side" : "exited large side")}");
			OnHallwayExit?.Invoke(other, exitedSmallSide);
		}

		private Plane largeSidePlane, smallSidePlane;

		[ShowNativeProperty]
		private Vector3 largePlaneSideNormal => largeSidePlane.normal;
		[ShowNativeProperty]
		private Vector3 boundsSize => bounds.size;

		// Stored as local positions, converted to world when used
		private Vector3 largeSidePoint, smallSidePoint;
		[ShowNativeProperty]
		public Vector3 largeSidePointWorld => transform.TransformPoint(largeSidePoint);
		[ShowNativeProperty]
		public Vector3 smallSidePointWorld => transform.TransformPoint(smallSidePoint);

		public void SetupBoundaries(Collider c) {
			Bounds b = c.bounds;
			// Use the Y position of the Collider (should set its pivot point to the floor)
			Vector3 center = new Vector3(b.center.x, c.transform.position.y, b.center.z);
			float tunnelExtent = b.extents.MaxComponent() - 0.5f; // Assume longest side to be the direction

			int maxComponentDirection = b.extents.MaxComponentDirection();
			Vector3 longestDirection = (new Vector3[3] { transform.right, transform.up, transform.forward })[maxComponentDirection];
			largeSidePoint = transform.InverseTransformPoint(center - longestDirection * tunnelExtent);
			smallSidePoint = transform.InverseTransformPoint(center + longestDirection * tunnelExtent);

			largeSidePlane = new Plane(longestDirection, largeSidePoint);
			smallSidePlane = new Plane(longestDirection, smallSidePoint);

			if (DEBUG) {
				Debug.DrawLine(center, largeSidePointWorld, Color.yellow, 30);
				Debug.DrawLine(center, smallSidePointWorld, Color.cyan, 30);
			}
		}

		void OnDrawGizmosSelected() {
			float size = 16f;
			DrawPlanes(smallSidePointWorld, smallSidePlane.normal, size * .25f, size * .25f, Color.cyan);
			DrawPlanes(largeSidePointWorld, largeSidePlane.normal, size, size, Color.yellow);
		}

		void DrawPlanes(Vector3 point, Vector3 normal, float height, float width, Color color) {
			// if (!Application.isPlaying) return;
			if (normal == Vector3.zero) return;
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