using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SuperspectivePhysics {
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
				return Equals((ColliderPair) obj);
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
}