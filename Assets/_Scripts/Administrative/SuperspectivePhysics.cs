using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PortalMechanics;
using SuperspectiveUtils;
using UnityEditor;
using UnityEngine;

public readonly struct ColliderPair : IEquatable<ColliderPair> {
	public readonly Collider col1;
	public readonly Collider col2;

	public bool IsValid => col1 != null && col2 != null;
	
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

public static class SuperspectivePhysics {
	public static readonly Vector3 originalGravity = Physics.gravity;

#region Layers
	private static int _defaultLayer = -1;
	private static int _visibleButNoPlayerCollisionLayer = -1;
	private static int _ignoreRaycastLayer = -1;
	private static int _blockRaycastOnlyLayer = -1;
	private static int _triggerZoneLayer = -1;
	private static int _playerLayer = -1;
	private static int _invisibleLayer = -1;
	private static int _collideWithPlayerOnlyLayer = -1;
	private static int _cullEverythingLayer = -1;
	private static int _visibilityMaskLayer = -1;
	private static int _portalLayer = -1;
	private static int _hideFromPortalRendering = -1;
	private static int _volumetricPortalLayer = -1;
	private static int _inverseBloomLayer = -1;

	private static int LazyLayer(ref int layer, string layerName) {
		return layer < 0 ? layer = LayerMask.NameToLayer(layerName) : layer;
	}

	public static int DefaultLayer => LazyLayer(ref _defaultLayer, "Default");
	public static int VisibleButNoPlayerCollisionLayer => LazyLayer(ref _visibleButNoPlayerCollisionLayer, "VisibleButNoPlayerCollision");
	public static int IgnoreRaycastLayer => LazyLayer(ref _ignoreRaycastLayer, "Ignore Raycast");
	public static int BlockRaycastOnlyLayer => LazyLayer(ref _blockRaycastOnlyLayer, "BlockRaycastOnly");
	public static int TriggerZoneLayer => LazyLayer(ref _triggerZoneLayer, "TriggerZone");
	public static int PlayerLayer => LazyLayer(ref _playerLayer, "Player");
	public static int InvisibleLayer => LazyLayer(ref _invisibleLayer,"Invisible");
	public static int CollideWithPlayerOnlyLayer => LazyLayer(ref _collideWithPlayerOnlyLayer, "CollideWithPlayerOnly");
	public static int CullEverythingLayer => LazyLayer(ref _cullEverythingLayer, "CullEverythingLayer");
	public static int VisibilityMaskLayer => LazyLayer(ref _visibilityMaskLayer, "VisibilityMask");
	public static int PortalLayer => LazyLayer(ref _portalLayer, "Portal");
	public static int HideFromPortalLayer => LazyLayer(ref _hideFromPortalRendering, "HideFromPortalRendering");
	public static int VolumetricPortalLayer => LazyLayer(ref _volumetricPortalLayer, "VolumetricPortal");
	// TODO: Remove?:
	public static int InverseBloomLayer => LazyLayer(ref _inverseBloomLayer, "InverseBloom");
	
	// For finding things the player can interact with in the game world
	public static int PhysicsRaycastLayerMask =>
		~((1 << IgnoreRaycastLayer) |
		  (1 << TriggerZoneLayer) |
		  (1 << PlayerLayer) |
		  (1 << InvisibleLayer) |
		  (1 << VisibilityMaskLayer) |
		  (1 << CollideWithPlayerOnlyLayer));

	// For finding solid objects that the player physically collides with
	public static int PlayerPhysicsCollisionLayerMask => Player.instance.interactsWithPlayerLayerMask;
	
#endregion

#region Collisions

	// Colliders ignoring each other -> # times the colliders have been instructed to ignore each other
	private static readonly Dictionary<ColliderPair, HashSet<string>> ignoredCollisions = new Dictionary<ColliderPair, HashSet<string>>();

	public static bool CollisionsAreIgnored(Collider collider1, Collider collider2) {
		ColliderPair pair = new ColliderPair(collider1, collider2);
		return ignoredCollisions.ContainsKey(pair) && ignoredCollisions[pair].Count > 0;
	}

	/// <summary>
	/// Ignores collisions between two Colliders, identified by a string identifier.
	/// If multiple sources (given by identifier) ignore the same collision, they must all restore it before it will be restored.
	/// </summary>
	/// <param name="collider1">Collider 1</param>
	/// <param name="collider2">Collider 2</param>
	/// <param name="identifier">Unique identifier for this source of this collision ignore request</param>
	public static void IgnoreCollision(Collider collider1, Collider collider2, string identifier) {
		ColliderPair pair = new ColliderPair(collider1, collider2);
		if (ignoredCollisions.TryGetValue(pair, out HashSet<string> ignoredCollisionsForPair)) {
			ignoredCollisionsForPair.Add(identifier);
		}
		else {
			Physics.IgnoreCollision(pair.col1, pair.col2, true);
			ignoredCollisions.Add(pair, new HashSet<string>() { identifier });
		}
	}

	/// <summary>
	/// Restores a collision between two Colliders that was previously ignored by a specific source.
	/// If multiple sources (given by identifier) ignore the same collision, they must all restore it before it will be restored.
	/// </summary>
	/// <param name="collider1">Collider 1</param>
	/// <param name="collider2">Collider 2</param>
	/// <param name="identifier">Unique identifier for this source of this collision restore request</param>
	public static void RestoreCollision(Collider collider1, Collider collider2, string identifier) {
		ColliderPair pair = new ColliderPair(collider1, collider2);
		if (ignoredCollisions.ContainsKey(pair)) {
			ignoredCollisions[pair].Remove(identifier);
			
			if (ignoredCollisions[pair].Count == 0) {
				Physics.IgnoreCollision(pair.col1, pair.col2, false);
				ignoredCollisions.Remove(pair);
			}
		}
	}

	/// <summary>
	/// Checks if two Colliders are overlapping.
	/// </summary>
	/// <param name="c1">Collider 1</param>
	/// <param name="c2">Collider 2</param>
	/// <returns>True if the two Colliders overlap, false otherwise</returns>
	public static bool CollidersOverlap(Collider c1, Collider c2) {
		// This function isn't typically used for this purpose (hence the two throwaway out arguments),
		// but it's the best I could find that did a proper check of whether two Colliders would be in contact with each other
		return Physics.ComputePenetration(
			c1, c1.transform.position, c1.transform.rotation, 
			c2, c2.transform.position, c2.transform.rotation,
			out _, out _);
	}

	public static void ResetState() {
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

	public static List<string> IgnoredColliderPairsDebugString(Func<ColliderPair, bool> filterOutCondition = null) {
		string GetDebugString(KeyValuePair<ColliderPair, HashSet<string>> kv) {
			(ColliderPair pair, HashSet<string> ignoreSources) = kv;

			return $"{pair.col1.FullPath()}\n<>\n{pair.col2.FullPath()}\nTimes Ignored: {ignoreSources.Count}\nIgnored by:\n{string.Join("\n", ignoreSources)}";
		}

		bool AppliedFilter(KeyValuePair<ColliderPair, HashSet<string>> kv) {
			ColliderPair pair = kv.Key;
			// Ignore bad entries for now, may want to change behavior later
			// Also filter by the filterOutCondition, if provided
			return !(pair.col1 == null || pair.col2 == null || (filterOutCondition != null && filterOutCondition.Invoke(pair)));
		}

		return ignoredCollisions.Where(AppliedFilter).Select(GetDebugString).ToList();
	}
#endif

#endregion

#region Custom Physics Helpers
	
	public static float ShortestDistance(Vector3 p1, Vector3 p2) {
		return ShortestVectorPointToPoint(p1, p2).magnitude;
	}

	/// <summary>
	/// Returns the shortest vector from p1 to p2, taking Portals into account.
	/// Gotcha: If the returned vector goes to a Portal, the direction will be from p1 to the portal,
	/// while the distance will be from p1 to the portal + the distance from the portal to p2.
	/// </summary>
	/// <param name="p1">From point</param>
	/// <param name="p2">To point</param>
	/// <returns>Shortest vector to get from p1 to p2, with respect to non-euclidean geometry</returns>
	public static Vector3 ShortestVectorPointToPoint(Vector3 p1, Vector3 p2, bool debug = false) {
		return ShortestVectorWithPortalPointToPoint(p1, p2, debug).Item1;
	}

	/// <summary>
	/// Returns the shortest vector from p1 to p2, taking Portals into account.
	/// Gotcha: If the returned vector goes to a Portal, the direction will be from p1 to the portal,
	/// while the distance will be from p1 to the portal + the distance from the portal to p2.
	/// </summary>
	/// <param name="p1">From point</param>
	/// <param name="p2">To point</param>
	/// <returns>Shortest vector to get from p1 to p2, with respect to non-euclidean geometry, along with the portal used to get there, if any</returns>
	public static (Vector3, Portal) ShortestVectorWithPortalPointToPoint(Vector3 p1, Vector3 p2, bool debug = false) {
		Vector3 pointToPoint = p2 - p1;
		float pointToPointDistance = pointToPoint.magnitude;

		Vector3 directionOfShortestVector = pointToPoint.normalized;
		float minDistance = pointToPointDistance;
		Portal minDistancePortal = null;
		
		// Because non-euclidean geometry kinda breaks the whole "shortest distance between two points is a straight line" thing
		// we need to do some extra work to figure out the shortest distance between two points, taking Portals into account
		foreach (Collider portalCollider in Physics.OverlapSphere(p1, pointToPointDistance, 1 << PortalLayer, QueryTriggerInteraction.Collide)) {
			if (!portalCollider.TryGetComponent(out Portal portal)) continue;
			if (!portal || !portal.otherPortal) continue;
			
			// Find the closest point on the in Portal to p1
			Vector3 closestPointOnInPortalToP1 = portal.ClosestPoint(p1, true, true);
			// Find the closest point on the out Portal to p2, then transform it to the in Portal's space
			Vector3 closestPointOnOutPortalToP2 = portal.otherPortal.ClosestPoint(p2, true, true);
			Vector3 closestPointOnInPortalToP2 = portal.otherPortal.TransformPoint(closestPointOnOutPortalToP2);
            
			// Calculate the distance from p1 and p2 to their respective portals
			float distanceFromP1ToInPortal = Vector3.Distance(closestPointOnInPortalToP1, p1);
			float distanceFromP2ToOutPortal = Vector3.Distance(closestPointOnOutPortalToP2, p2);
			
			// Use the ratio of distances to calculate the "midpoint" on the portal plane that should be used for a straight line
			Vector3 midpointOnPortalPlane = Vector3.Lerp(closestPointOnInPortalToP1, closestPointOnInPortalToP2, distanceFromP1ToInPortal / (distanceFromP1ToInPortal + portal.otherPortal.ScaleFactor * distanceFromP2ToOutPortal));

			// The two Vectors that make up the non-euclidean straight line
			Vector3 p1ToMidpoint = midpointOnPortalPlane - p1;
			Vector3 midpointToP2 = p2 - portal.TransformPoint(midpointOnPortalPlane);
			
			float distance = p1ToMidpoint.magnitude + portal.otherPortal.ScaleFactor * midpointToP2.magnitude;
			
			// If the total distance is the smallest we've seen so far, save it
			if (distance < minDistance) {
				minDistance = distance;
				// The direction of the shortest vector is the direction from p1 to the midpoint on the portal plane
				directionOfShortestVector = p1ToMidpoint.normalized;
				minDistancePortal = portal;

				if (debug) {
					DebugDraw.Sphere("p1", closestPointOnInPortalToP1, 0.05f, Color.blue);
					DebugDraw.Sphere("p2", closestPointOnInPortalToP2, 0.05f, Color.red);
					DebugDraw.Sphere("midpoint", midpointOnPortalPlane, 0.15f, new Color(.45f, 0.075f, .85f));
				}
			}
		}

		return (directionOfShortestVector * minDistance, minDistancePortal);
	}

#endregion
}

public static class RigidbodyExt {
	/// <summary>
	/// Gets the mass normalized kinetic energy of a Rigidbody.
	/// </summary>
	public static float GetMassNormalizedKineticEnergy(this Rigidbody r) {
		// Linear energy
		float E = 0.5f * r.mass * Mathf.Pow(r.velocity.magnitude, 2f);
 
		// Angular energy
		E += 0.5f * r.inertiaTensor.x * Mathf.Pow(r.angularVelocity.x, 2f);
		E += 0.5f * r.inertiaTensor.y * Mathf.Pow(r.angularVelocity.y, 2f);
		E += 0.5f * r.inertiaTensor.z * Mathf.Pow(r.angularVelocity.z, 2f);
 
		// Mass-normalized
		return E /= r.mass;
	}
}
