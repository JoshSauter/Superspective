using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperspectiveUtils;
using UnityEditor;
using UnityEngine;

public static class SuperspectivePhysics {
	private static readonly Vector3 originalGravity = Physics.gravity;

	private static int _ignoreRaycastLayer = -1;
	private static int _playerLayer = -1;
	private static int _invisibleLayer = -1;
	private static int _collideWithPlayerOnlyLayer = -1;
	private static int _cullEverythingLayer = -1;
	private static int _visibilityMaskLayer = -1;

	private static int LazyLayer(ref int layer, string layerName) {
		return layer < 0 ? layer = LayerMask.NameToLayer(layerName) : layer;
	}
	public static int IgnoreRaycastLayer => LazyLayer(ref _ignoreRaycastLayer, "Ignore Raycast");
	public static int PlayerLayer => LazyLayer(ref _playerLayer, "Player");
	public static int InvisibleLayer => LazyLayer(ref _invisibleLayer,"Invisible");
	public static int CollideWithPlayerOnlyLayer => LazyLayer(ref _collideWithPlayerOnlyLayer, "CollideWithPlayerOnly");
	public static int CullEverythingLayer => LazyLayer(ref _cullEverythingLayer, "CullEverythingLayer");
	public static int VisibilityMaskLayer => LazyLayer(ref _visibilityMaskLayer, "VisibilityMask");
	public static int PhysicsRaycastLayerMask =>
		~((1 << IgnoreRaycastLayer) |
		  (1 << PlayerLayer) |
		  (1 << InvisibleLayer) |
		  (1 << VisibilityMaskLayer) |
		  (1 << CollideWithPlayerOnlyLayer));

	public readonly struct ColliderPair : IEquatable<ColliderPair> {
		public readonly Collider col1;
		public readonly Collider col2;

		public ColliderPair(Collider col1, Collider col2) {
			this.col1 = col1;
			this.col2 = col2;
		}

		public bool Equals(ColliderPair other) {
			return (this.col1 == other.col1 && this.col2 == other.col2) ||
			       (this.col2 == other.col1 && this.col1 == other.col2);
		}

		public override bool Equals(System.Object obj) {
			//Check for null and compare run-time types.
			if ((obj == null) || this.GetType() != obj.GetType()) {
				return false;
			}
			else {
				return Equals((ColliderPair)obj);
			}
		}

		public static bool operator ==(ColliderPair pair1, ColliderPair pair2) {
			return pair1.Equals(pair2);
		}

		public static bool operator !=(ColliderPair pair1, ColliderPair pair2) {
			return !(pair1 == pair2);
		}

		public override int GetHashCode() {
			int a = col1.GetHashCode();
			int b = col2.GetHashCode();
			int smaller = a < b ? a : b;
			int larger = a < b ? b : a;

			return $"{smaller}_{larger}".GetHashCode();
		}
	}

	// Colliders ignoring each other -> # times the colliders have been instructed to ignore each other
	private static readonly Dictionary<ColliderPair, int> ignoredCollisions = new Dictionary<ColliderPair, int>();

	public static bool CollisionsAreIgnored(Collider collider1, Collider collider2) {
		ColliderPair pair = new ColliderPair(collider1, collider2);
		return ignoredCollisions.ContainsKey(pair) && ignoredCollisions[pair] > 0;
	}

	public static void IgnoreCollision(Collider collider1, Collider collider2) {
		ColliderPair pair = new ColliderPair(collider1, collider2);
		if (ignoredCollisions.ContainsKey(pair)) {
			ignoredCollisions[pair]++;
		}
		else {
			Physics.IgnoreCollision(pair.col1, pair.col2, true);
			ignoredCollisions.Add(pair, 1);
		}
	}

	public static void RestoreCollision(Collider collider1, Collider collider2) {
		ColliderPair pair = new ColliderPair(collider1, collider2);
		if (ignoredCollisions.ContainsKey(pair)) {
			if (ignoredCollisions[pair] <= 1) {
				Physics.IgnoreCollision(pair.col1, pair.col2, false);
				ignoredCollisions.Remove(pair);
			}
			else {
				ignoredCollisions[pair]--;
			}
		}
	}

	public static bool CollidersOverlap(Collider c1, Collider c2) {
		// This function isn't typically used for this purpose (hence the two throwaway out arguments),
		// but it's the best I could find that did a proper check of whether two Colliders would be in contact with each other
		return Physics.ComputePenetration(
			c1, c1.transform.position, c1.transform.rotation, 
			c2, c2.transform.position, c2.transform.rotation,
			out _, out _);
	}

	public static void ClearAllState() {
		Physics.gravity = originalGravity;

		foreach (var ignoredCollision in ignoredCollisions.Keys) {
			if (ignoredCollision.col1 == null || ignoredCollision.col2 == null) continue;
			
			Physics.IgnoreCollision(ignoredCollision.col1, ignoredCollision.col2, false);
		}
		
		ignoredCollisions.Clear();
	}

#if UNITY_EDITOR
	[MenuItem("My Tools/Superspective Physics/Print Ignored Collider Pairs")]
	public static void PrintIgnoredColliderPairs() {
		IgnoredColliderPairsDebugString().ForEach(Debug.Log);
	}

	[MenuItem("My Tools/Superspective Physics/Print Non-Player Ignored Collider Pairs")]
	public static void PrintNonPlayerIgnoredColliderPairs() {
		IgnoredColliderPairsDebugString(
				(pair) => pair.col1 == Player.instance.collider || pair.col2 == Player.instance.collider)
			.ForEach(Debug.Log);
	}

	private static List<string> IgnoredColliderPairsDebugString(Func<ColliderPair, bool> filterOutCondition = null) {
		string GetDebugString(KeyValuePair<ColliderPair, int> kv) {
			(ColliderPair pair, int timesIgnored) = kv;

			return $"{pair.col1.FullPath()}\n<>\n{pair.col2.FullPath()}\nTimes Ignored: {timesIgnored}\n\n";
		}

		bool AppliedFilter(KeyValuePair<ColliderPair, int> kv) {
			ColliderPair pair = kv.Key;
			// Ignore bad entries for now, may want to change behavior later
			// Also filter by the filterOutCondition, if provided
			return !(pair.col1 == null || pair.col2 == null || (filterOutCondition != null && filterOutCondition.Invoke(pair)));
		}

		return ignoredCollisions.Where(AppliedFilter).Select(GetDebugString).ToList();
	}
#endif
}
