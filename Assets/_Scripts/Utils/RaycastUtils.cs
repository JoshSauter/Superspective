using System;
using System.Collections.Generic;
using System.Linq;
using PortalMechanics;
using UnityEngine;

namespace SuperspectiveUtils {
    public static class RaycastUtils {
        const int MAX_RAYCASTS = 8;

        public static SuperspectiveRaycast Raycast(Ray ray, float maxDistance, int layerMask) {
            SuperspectiveRaycast result = new SuperspectiveRaycast();
            result.distance = maxDistance;

            Ray currentRay = ray;
            float distanceRemaining = maxDistance;
            int raycastsMade = 0;
            while (distanceRemaining > 0 && raycastsMade < MAX_RAYCASTS) {
                RaycastHit[] hits = Physics.RaycastAll(
                    currentRay.origin,
                    currentRay.direction,
                    distanceRemaining,
                    layerMask,
                    QueryTriggerInteraction.Collide
                );
                raycastsMade++;

                SuperspectiveRaycastPart part = new SuperspectiveRaycastPart(currentRay, distanceRemaining, hits);
                distanceRemaining -= part.distance;
                if (part.hitPortal && distanceRemaining > 0) {
                    currentRay = part.NextRay();
                }

                result.AddPart(part);
            }

            if (raycastsMade == MAX_RAYCASTS) {
                Debug.LogWarning($"Max steps for raycast {MAX_RAYCASTS} exceeded, raycast shorted early");
            }

            return result;
        }
        
        public static SuperspectiveRaycast Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            int layerMask
        ) {
            return Raycast(new Ray(origin, direction), maxDistance, layerMask);
        }
    }
        
    public class SuperspectiveRaycast {
        public float distance;
        public List<SuperspectiveRaycastPart> raycastParts = new List<SuperspectiveRaycastPart>();
        public bool hitPortal => allPortalsHit.Count > 0;
        public bool hitObject => allObjectHits.Count > 0;

        public RaycastHit firstObjectHit => allObjectHits.First();

        public Portal firstPortalHit {
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

        public Vector3 finalPosition {
            get {
                SuperspectiveRaycastPart lastPart = raycastParts.Last();
                return lastPart.ray.origin + lastPart.ray.direction * lastPart.distance;
            }
        }

        public void AddPart(SuperspectiveRaycastPart part) {
            if (part.portalHit != null) {
                allPortalsHit.Add(part.portalHit);
            }

            if (part.objectsHit != null && part.objectsHit.Length > 0) {
                allObjectHits.AddRange(part.objectsHit);
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
        readonly int portalHitIndex;

        public SuperspectiveRaycastPart(Ray ray, float fullRaycastDistance, RaycastHit[] raycastHits) {
            // Make sure we sort the results into distance-based order
            raycastHits = raycastHits.OrderBy(hit => hit.distance).ToArray();
            
            Portal HitPortal(RaycastHit hit) {
                GameObject hitObject = hit.collider.gameObject;
                Portal result = hitObject.GetComponent<Portal>();
                // If we didn't hit the portal itself, see if we hit the PortalCollider
                if (result == null) {
                    PortalCollider portalCollider = hitObject.GetComponent<PortalCollider>();
                    if (portalCollider != null) {
                        result = portalCollider.portal;
                    }
                }

                // If we didn't hit either of those, check if it's the VolumetricPortal
                if (result == null && hitObject.name.Contains("VolumetricPortal")) {
                    result = hitObject.transform.parent?.GetComponent<Portal>();
                }

                return result;
            }

            portalHit = null;
            portalHitIndex = -1;
            for (int i = 0; i < raycastHits.Length; i++) {
                portalHit = HitPortal(raycastHits[i]);

                if (portalHit != null) {
                    portalHitIndex = i;
                    break;
                }
            }
            
            float distanceOfPortal = portalHit != null ? raycastHits[portalHitIndex].distance : float.MaxValue;
            this.ray = ray;
            this.distance = portalHit ? distanceOfPortal : fullRaycastDistance;
            this.rawHitInfos = raycastHits;

            bool VisibilityMaskCheck(RaycastHit hit) {
                DimensionObject dimensionObject = hit.collider.FindDimensionObjectRecursively<DimensionObject>();
                // If this object is rendered as a DimensionObject, only consider it a hit if the cursor is hovering
                // over the appropriate visibility masks to show the DimensionObject
                if (dimensionObject != null) {
                    int maskValue = MaskBufferRenderTextures.instance.visibilityMaskValue;
                    return dimensionObject.IsVisibleFromMask(maskValue);
                }
                // Everything else is rendered by default
                else {
                    return true;
                }
            }

            bool InFrontOfPortalCheck(RaycastHit hit) {
                return hit.distance < distanceOfPortal;
            }

            objectsHit = rawHitInfos
                .Where(VisibilityMaskCheck)
                .Where(InFrontOfPortalCheck)
                .ToArray();
        }

        public Ray NextRay() {
            if (portalHit) {
                Vector3 newOrigin = portalHit.TransformPoint(rawHitInfos[portalHitIndex].point);
                Vector3 newDirection = portalHit.TransformDirection(ray.direction);

                return new Ray(newOrigin, newDirection);
            }
            else {
                return new Ray();
            }
        }
    }
}