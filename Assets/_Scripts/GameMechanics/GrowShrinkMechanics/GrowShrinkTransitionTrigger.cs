using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using PortalMechanics;
using UnityEngine;
using SuperspectiveUtils;

namespace GrowShrink {
	[RequireComponent(typeof(BetterTrigger), typeof(MeshCollider))]
	public class GrowShrinkTransitionTrigger : MonoBehaviour, BetterTriggers {
		public bool DEBUG = false;

		private DebugLogger _debug;
		private DebugLogger debug {
			get {
				if (_debug == null) {
					_debug = new DebugLogger(this, () => DEBUG);
				}

				return _debug;
			}
			set => _debug = value;
		}
		Bounds bounds;
		
		public MeshCollider MeshCollider => GetComponent<MeshCollider>();

		private Dictionary<Collider, Vector3> positionsLastFrame = new Dictionary<Collider, Vector3>();

		public delegate void TransitionTriggerAction(Collider collider, float t);

		public delegate void HallwayEnterAction(Collider collider, bool enteredSmallSide);

		public delegate void HallwayExitAction(Collider collider, bool enteredSmallSide);

		public event TransitionTriggerAction OnTransitionTrigger;
		public event HallwayEnterAction OnHallwayEnter;
		public event HallwayExitAction OnHallwayExit;

		void Start() {
			SetupBoundaries(GetComponent<MeshCollider>());
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
		[ShowNonSerializedField]
		private Vector3 largeSidePoint, smallSidePoint;
		[ShowNativeProperty]
		public Vector3 largeSidePointWorld => transform.TransformPoint(largeSidePoint);
		[ShowNativeProperty]
		public Vector3 smallSidePointWorld => transform.TransformPoint(smallSidePoint);

		public void SetupBoundaries(MeshCollider c) {
			Bounds b = c.bounds;
			// Use the Y position of the Collider (should set its pivot point to the floor)
			Vector3 center = new Vector3(b.center.x, c.transform.position.y, b.center.z);
			b = c.sharedMesh.bounds; // Switch to local bounds for tunnelExtent calculation
			float tunnelExtent = (transform.position - center).magnitude - 0.5f;

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
			ExtDebug.DrawPlane(smallSidePointWorld, smallSidePlane.normal, size * .25f, size * .25f, Color.cyan);
			ExtDebug.DrawPlane(largeSidePointWorld, largeSidePlane.normal, size, size, Color.yellow);
		}
	}
}