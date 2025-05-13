using System;
using System.Collections.Generic;
using System.Linq;
using DimensionObjectMechanics;
using PortalMechanics;
using UnityEngine;

namespace SuperspectiveUtils {
    public static class RaycastUtils {
        public delegate void RaycastEvent();

        public static event RaycastEvent BeforeRaycast;
        public static event RaycastEvent AfterRaycast;
        
        private static int portalLayer => LayerMask.NameToLayer("Portal");
        public const int MAX_RAYCASTS = 8;

        /// <summary>
        /// Calculates the minimum Distance between two points, either straight line distance,
        /// or distance to a nearby in portal plus the distance from the out portal to the target
        /// </summary>
        /// <returns></returns>
        public static float MinDistanceBetweenPoints(Vector3 a, Vector3 b) {
            float minDistance = Vector3.Distance(a, b);
            // Look for portals that might shorten the distance nearby
            var hits = Physics.OverlapSphere(a, minDistance, 1 << portalLayer, QueryTriggerInteraction.Collide);
            foreach (var portalObj in hits) {
                if (portalObj.TryGetComponent(out Portal p) && p.otherPortal != null) {
                    Vector3 closestPointOnInPortal = p.ClosestPoint(a, false, true);
                    float distanceToInPortal = Vector3.Distance(closestPointOnInPortal, a);
                    float distanceFromOutPortal = Vector3.Distance(p.TransformPoint(closestPointOnInPortal), b);

                    float totalPortalDistance = distanceToInPortal + distanceFromOutPortal;
                    if (totalPortalDistance < minDistance) {
                        minDistance = totalPortalDistance;
                    }
                }
            }

            return minDistance;
        }

        public static SuperspectiveRaycast Raycast(Ray ray, float maxDistance, int layerMask, bool isPlayerCamRaycast = false) {
            SuperspectiveRaycast result = new SuperspectiveRaycast {
                distance = maxDistance,
                ray = ray
            };
            
            BeforeRaycast?.Invoke();

            Ray currentRay = ray;
            float distanceRemaining = maxDistance;
            int raycastsMade = 0;
            Portal lastPortalHit = null;
            while (distanceRemaining > 0 && raycastsMade < MAX_RAYCASTS) {
                // Temporarily ignore the out portal of the last portal hit
                bool shouldDisableOtherPortal = lastPortalHit != null && lastPortalHit.otherPortal != null && lastPortalHit != lastPortalHit.otherPortal;
                if (shouldDisableOtherPortal) {
                    foreach (Collider otherPortalCollider in lastPortalHit.otherPortal.colliders) {
                        otherPortalCollider.enabled = false;
                    }
                }

                if (Interact.instance.DEBUG) {
                    Debug.DrawRay(currentRay.origin, currentRay.direction * distanceRemaining, Color.Lerp(Color.green, Color.red, (float)raycastsMade / MAX_RAYCASTS));
                }

                RaycastHit[] hits = Physics.RaycastAll(
                    currentRay.origin,
                    currentRay.direction,
                    distanceRemaining,
                    layerMask,
                    QueryTriggerInteraction.Collide
                );
                raycastsMade++;
                if (shouldDisableOtherPortal) {
                    foreach (Collider otherPortalCollider in lastPortalHit.otherPortal.colliders) {
                        otherPortalCollider.enabled = true;
                    }
                }

                SuperspectiveRaycastPart part = new SuperspectiveRaycastPart(currentRay, distanceRemaining, hits, isPlayerCamRaycast);
                distanceRemaining -= part.distance;
                if (part.hitPortal && distanceRemaining > 0) {
                    currentRay = part.NextRay();
                    lastPortalHit = part.portalHit;
                    distanceRemaining *= part.portalHit.ScaleFactor;
                }

                result.AddPart(part);
            }

            if (raycastsMade == MAX_RAYCASTS) {
                Debug.LogWarning($"Max steps for raycast {MAX_RAYCASTS} exceeded, raycast shorted early");
            }
            
            AfterRaycast?.Invoke();

            return result;
        }
        
        public static SuperspectiveRaycast Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            int layerMask,
            bool isPlayerCamRaycast = false // We only do VisibilityMask checks for player camera raycasts (since we only know the cursor pixel mask value on the CPU)
        ) {
            return Raycast(new Ray(origin, direction), maxDistance, layerMask, isPlayerCamRaycast);
        }
    }
    
    [Serializable]
    public class SuperspectiveRaycast {
        public Ray ray;
        public float distance;
        public List<SuperspectiveRaycastPart> raycastParts = new List<SuperspectiveRaycastPart>();
        // Only counts portals that are not occluded by another object
        public bool DidHitPortal => effectivePortalsHit.Count > 0;
        public bool DidHitAnyPortal => allPortalsHit.Count > 0;
        public bool DidHitObject => allObjectHits.Count > 0;

        public RaycastHit FirstObjectHit => allObjectHits.First();

        // A "Valid" Portal is a Portal hit in the first raycast, without any object in front of it
        public Portal FirstValidPortalHit {
            get {
                try {
                    // We never hit a portal, don't bother checking
                    return !DidHitPortal ? null : effectivePortalsHit.First();
                }
                catch {
                    return null;
                }
            }
        }
        public Portal FirstAnyPortalHit {
            get {
                try {
                    return allPortalsHit.First();
                }
                catch {
                    return null;
                }
            }
        }
        public List<RaycastHit> allObjectHits = new List<RaycastHit>();
        public List<Portal> allPortalsHit = new List<Portal>();
        public List<Portal> effectivePortalsHit = new List<Portal>(); // Portals that are not blocked by objects

        public Vector3 FinalPosition {
            get {
                SuperspectiveRaycastPart lastPart = raycastParts.Last();
                return lastPart.ray.origin + lastPart.ray.direction * lastPart.distance;
            }
        }

        public void AddPart(SuperspectiveRaycastPart part) {
            float portalHitDistance = float.MaxValue;
            if (part.portalHit != null) {
                allPortalsHit.Add(part.portalHit);
                portalHitDistance = part.distance;
            }

            bool hitPortalFirst = part.hitPortal;
            if (part.objectsHit != null && part.objectsHit.Length > 0) {
                allObjectHits.AddRange(part.objectsHit);
                float minDistance = part.objectsHit.Select(hit => hit.distance).Min();
                hitPortalFirst = part.hitPortal && minDistance > portalHitDistance;
            }
            
            if (part.portalHit != null && hitPortalFirst) {
                effectivePortalsHit.Add(part.portalHit);
            }

            raycastParts.Add(part);
        }
    }

    public struct SuperspectiveRaycastPart {
        // Origin and direction of raycast
        public Ray ray;
        // Distance to the portal hit, or the full distance of the raycast if no portal was hit
        public float distance;
        // Non-portal objects that should be considered hit by the raycast (not invisible DimensionObjects nor behind a hit portal)
        public bool hitObject => objectsHit.Length > 0;
        public RaycastHit[] objectsHit;
        // The unfiltered RaycastHits returned from the raycast (including invisible DimensionObjects, a hit portal, and objects behind a hit portal)
        public RaycastHit[] rawHitInfos;
        // First portal hit by the raycast, if any
        public bool hitPortal => portalHit != null;
        public Portal portalHit;
        readonly public int portalHitIndex;

        public SuperspectiveRaycastPart(Ray ray, float raycastDistance, RaycastHit[] raycastHits, bool isPlayerCamRaycast = false) {
            // Make sure we sort the results into distance-based order
            raycastHits = raycastHits.OrderBy(hit => hit.distance).ToArray();

            portalHit = null;
            portalHitIndex = -1;
            for (int i = 0; i < raycastHits.Length; i++) {
                raycastHits[i] = AdjustedHit(ray, raycastHits[i], raycastDistance, out Portal portalActuallyHit);

                if (portalActuallyHit != null) {
                    portalHit = portalActuallyHit;
                    portalHitIndex = i;
                    break;
                }
            }
            
            float distanceOfPortal = portalHit != null ?
                raycastHits[portalHitIndex].distance :
                float.MaxValue;
            this.ray = ray;
            this.distance = portalHit ? distanceOfPortal : raycastDistance;
            
            this.rawHitInfos = raycastHits;

            bool VisibilityMaskCheck(RaycastHit hit) {
                // We only care about the visibility mask if this is a player camera raycast
                return !isPlayerCamRaycast || DimensionObjectManager.instance.RaycastHitCollider(hit.collider);
            }

            bool InFrontOfPortalCheck(RaycastHit hit) {
                return hit.distance < distanceOfPortal;
            }

            bool IsValidRaycastHit(RaycastHit hit) {
                // Make sure the raycast hit was not set to a new RaycastHit by AdjustedHit
                return hit.collider != null;
            }
            
            bool IsNotPortal(RaycastHit hit) {
                // Don't count the portal itself as an object hit
                return hit.collider.gameObject.layer != SuperspectivePhysics.PortalLayer;
            }

            objectsHit = rawHitInfos
                .Where(IsValidRaycastHit)
                .Where(VisibilityMaskCheck)
                .Where(InFrontOfPortalCheck)
                .Where(IsNotPortal)
                .ToArray();
        }

        public Ray NextRay() {
            if (portalHit) {
                try {
                    Vector3 newOrigin = portalHit.TransformPoint(rawHitInfos[portalHitIndex].point);
                    Vector3 newDirection = portalHit.TransformDirection(ray.direction);

                    return new Ray(newOrigin, newDirection);
                }
                catch {
                    return new Ray();
                }
            }
            else {
                return new Ray();
            }
        }
        
        private static Portal HitPortalTrigger(RaycastHit hit) {
            GameObject hitObject = hit.collider.gameObject;
            Portal result = hitObject.GetComponent<Portal>();
            // If we didn't hit the portal itself, see if we hit the PortalCollider
            if (result == null) {
                NonPlayerPortalTriggerZone nonPlayerPortalTriggerZone = hitObject.GetComponent<NonPlayerPortalTriggerZone>();
                if (nonPlayerPortalTriggerZone != null) {
                    result = nonPlayerPortalTriggerZone.portal;
                }
            }

            // If we didn't hit either of those, check if it's the VolumetricPortal or the raycast blocker
            if (result == null && hitObject.name.Contains("VolumetricPortal") || hitObject.name.Contains("Raycast Blocker")) {
                result = hitObject.transform.parent?.GetComponent<Portal>();
            }

            // Revealer portals are not valid redirectors of raycasts
            if (result is RevealerPortal) return null;
            
            return result;
        }

        // Just because a Portal trigger was hit doesn't mean the raycast actually hit the portal, this adjusts for that
        private static RaycastHit AdjustedHit(Ray ray, RaycastHit hit, float distanceRemaining, out Portal portalHit) {
            portalHit = null;
            RaycastHit newHit = hit;

            Portal portalWhoseTriggerWasHit = HitPortalTrigger(hit);
            if (portalWhoseTriggerWasHit != null) {
                // First check if we hit the backside of the portal, if we did, we can skip all the calculations
                if (Vector3.Dot(portalWhoseTriggerWasHit.IntoPortalVector, ray.direction) < 0) {
                    return new RaycastHit();
                }
                
                bool didWeActuallyHitPortal = false;
                Vector3 finalHitPosition = hit.point;
                float finalDistance = hit.distance;
                
                // Okay, so we hit the portal trigger zone, but did we actually hit the Portal?
                // First we construct a plane for the Portal plane, slightly pushed into the portal to allow for the case where the player is standing just slightly past the actual portal plane
                Vector3 planeOffset = portalWhoseTriggerWasHit.IntoPortalVector.normalized * 0.1f;
                Plane portalPlane = new Plane(portalWhoseTriggerWasHit.IntoPortalVector, portalWhoseTriggerWasHit.transform.position + planeOffset);
                // Then we see if the original raycast actually hits the plane
                if (portalPlane.Raycast(ray, out float distanceToIntersect) && distanceToIntersect <= distanceRemaining) {
                    // Okay so the raycast hit the infinitely extending plane, but did it hit the Portal?
                    Vector3 hitPosition = ray.GetPoint(distanceToIntersect) - planeOffset;
                    // Find the closest point on the Portal to the hit position
                    Vector3 testPosition = portalWhoseTriggerWasHit.ClosestPoint(hitPosition, true, true);
                    
                    // If they're basically the same point, then we hit the Portal
                    if (Vector3.Distance(testPosition, hitPosition) < 0.001f) {
                        didWeActuallyHitPortal = true;
                        finalHitPosition = testPosition;
                        finalDistance = distanceToIntersect;
                    }
                }

                if (didWeActuallyHitPortal) {
                    // We actually did hit the portal, here is the actual hit information
                    newHit.point = finalHitPosition;
                    newHit.distance = finalDistance;
                    portalHit = portalWhoseTriggerWasHit;
                    return newHit;
                }
                else {
                    // We thought we hit a portal, but we did not
                    return new RaycastHit();
                }
            }
            
            return newHit;
        }
    }
}
