using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DimensionObjectMechanics {
    /// <summary>
    /// This is where the behavior of individual Renderers and Colliders is managed, based on all of the DimensionObjects that are affecting them
    /// </summary>
    public class DimensionObjectManager : Singleton<DimensionObjectManager> {
        private const string DIMENSION_OBJECT_KEYWORD = "DIMENSION_OBJECT";
        private const string MASK_SOLUTION_PROPERTY_KEY = "_MaskSolution";
        public const string DIMENSION_OBJECT_SUFFIX = " (DimensionObject)";
        
        // We use these to keep track of which DimensionObjects are affecting which colliders and renderers.
        // This way, we can figure out exactly how each renderer and collider
        // should behave based on the DimensionObjects that are affecting them.
        private static readonly Dictionary<Collider, ListHashSet<DimensionObject>> collidersAffectedByDimensionObjects = new Dictionary<Collider, ListHashSet<DimensionObject>>();
        private static readonly Dictionary<SuperspectiveRenderer, ListHashSet<DimensionObject>> renderersAffectedByDimensionObjects = new Dictionary<SuperspectiveRenderer, ListHashSet<DimensionObject>>();
        
        // We keep a cache of computed collisions for each collider pair so that we don't recompute them
        // every call to ShouldCollideWithDimensionObject, and so that we can recompute them when needed, such as when a DimensionObject changes state
        private static readonly CollisionsCache collisionsCache = new CollisionsCache();
        
        // We keep a cache of the materials used by DimensionObjects so that we can share them across all DimensionObjects that use the same material
        // This way, we don't create a new material instance for every DimensionObject Renderer
        private static readonly Dictionary<Material, Material> dimensionObjectMaterials = new Dictionary<Material, Material>();

        // We keep track of the original layers of GameObjects that are affected by DimensionObjects
        // so that we can restore them when they are no longer invisible
        private static readonly Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
	
#region Registration
        /// <summary>
        /// Register a Collider as being affected by a DimensionObject
        /// </summary>
        /// <param name="collider">Collider to register</param>
        /// <param name="dimensionObject">DimensionObject affecting the Collider</param>
        public static void RegisterCollider(Collider collider, DimensionObject dimensionObject) {
            dimensionObject.debug.Log($"Registering {collider.name} for {dimensionObject.ID}");
            if (!collidersAffectedByDimensionObjects.ContainsKey(collider)) {
                collidersAffectedByDimensionObjects[collider] = new ListHashSet<DimensionObject>();
            }
            collidersAffectedByDimensionObjects[collider].Add(dimensionObject);
        }

        /// <summary>
        /// Register a Renderer as being affected by a DimensionObject
        /// </summary>
        /// <param name="renderer">SuperspectiveRenderer to register</param>
        /// <param name="dimensionObject">DimensionObject affecting the Renderer</param>
        public static void RegisterRenderer(SuperspectiveRenderer renderer, DimensionObject dimensionObject) {
            if (!renderersAffectedByDimensionObjects.ContainsKey(renderer)) {
                renderersAffectedByDimensionObjects[renderer] = new ListHashSet<DimensionObject>();
            }
            renderersAffectedByDimensionObjects[renderer].Add(dimensionObject);
        }
	
        /// <summary>
        /// Unregister a Collider from being affected by a DimensionObject
        /// </summary>
        /// <param name="collider">Collider to unregister</param>
        /// <param name="dimensionObject">DimensionObject no longer affecting the Collider</param>
        public static void UnregisterCollider(Collider collider, DimensionObject dimensionObject) {
            dimensionObject.debug.Log($"Unregistering {collider.name} from {dimensionObject.ID}");
            
            if (collidersAffectedByDimensionObjects.TryGetValue(collider, out ListHashSet<DimensionObject> dimObjs)) {
                dimObjs.Remove(dimensionObject);
            }
            
            // If there are no more DimensionObjects affecting this collider, we can remove it from the dictionary
            if (!collidersAffectedByDimensionObjects.ContainsKey(collider) || collidersAffectedByDimensionObjects[collider].Count == 0) {
                collidersAffectedByDimensionObjects.Remove(collider);
                
                // If we're removing the last DimensionObject affecting this collider, we can restore the original layer
                if (originalLayers.ContainsKey(collider.gameObject)) {
                    collider.gameObject.layer = originalLayers[collider.gameObject];
                    originalLayers.Remove(collider.gameObject);
                }
            }
            
            // Remove all cached collision checks for this collider
            collisionsCache.RemoveCollider(collider);
        }
	
        /// <summary>
        /// Unregister a Renderer from being affected by a DimensionObject
        /// </summary>
        /// <param name="renderer">SuperspectiveRenderer to unregister</param>
        /// <param name="dimensionObject">DimensionObject no longer affecting the Renderer</param>
        public static void UnregisterRenderer(SuperspectiveRenderer renderer, DimensionObject dimensionObject) {
            if (renderersAffectedByDimensionObjects.TryGetValue(renderer, out ListHashSet<DimensionObject> dimObjs)) {
                dimObjs.Remove(dimensionObject);
            }
            
            // If there are no more DimensionObjects affecting this renderer, we can remove it from the dictionary
            if (!renderersAffectedByDimensionObjects.ContainsKey(renderer) || renderersAffectedByDimensionObjects[renderer].Count == 0) {
                renderersAffectedByDimensionObjects.Remove(renderer);
                
                // If we're removing the last DimensionObject affecting this renderer, we can restore the original layer
                if (originalLayers.ContainsKey(renderer.gameObject)) {
                    renderer.gameObject.layer = originalLayers[renderer.gameObject];
                    originalLayers.Remove(renderer.gameObject);
                }
            }
        }
#endregion

#region Rendering
        
        /// <summary>
        /// Refresh the renderers for a DimensionObject, updating their shader properties and layers as needed.
        /// </summary>
        /// <param name="dimensionObject">DimensionObject to refresh renderers for</param>
        public void RefreshRenderersForDimensionObject(DimensionObject dimensionObject) {
            foreach (SuperspectiveRenderer r in dimensionObject.renderers) {
                if (renderersAffectedByDimensionObjects[r].Contains(dimensionObject)) {
                    RefreshRenderer(r);
                }
            }
        }

        /// <summary>
        /// Considers all DimensionObjects affecting the provided Renderer, and sets its shader/layer properties accordingly.
        /// Behavior should be as follows:
        /// 1) If any DimensionObject affecting the Renderer is Invisible, the Renderer should be set to the Invisible layer and no further processing is needed.
        /// 2) Otherwise, collect all PartiallyVisible/PartiallyInvisible DimensionObjects affecting the Renderer and pass their data to the shader.
        /// If you're calling this, make sure all PillarDimensionObjects affecting the renderer have already been updated for the camera rendering.
        /// </summary>
        /// <param name="r">Renderer to set properties for</param>
        private void RefreshRenderer(SuperspectiveRenderer r) {
            GameObject rendererGameObject = r.gameObject;
            
            // Get all the DimensionObjects that are affecting this renderer
            ListHashSet<DimensionObject> dimensionObjects = renderersAffectedByDimensionObjects[r];

            (bool anyIsInvisible, DimensionObjectBitmask finalBitmask) = GetDimensionObjectBitmask(dimensionObjects);
            
            // Set the original layer of this Renderer if it hasn't been set yet (we might be about to change it to InvisibleLayer)
            if (!originalLayers.ContainsKey(rendererGameObject)) {
                originalLayers[rendererGameObject] = rendererGameObject.layer;
            }
            
            // If any of the DimensionObjects affecting this renderer are Invisible, the end result is invisible
            if (anyIsInvisible) {
                rendererGameObject.layer = SuperspectivePhysics.InvisibleLayer;
                return;
            }

            // We're not invisible, make sure we're on the original layer
            rendererGameObject.layer = originalLayers[rendererGameObject];
            
            // Set the shader properties for this renderer
            SetShaderProperties(r, finalBitmask);
        }

        private void SetShaderProperties(SuperspectiveRenderer r, DimensionObjectBitmask bitmask) {
            Material[] sharedMaterials = r.GetSharedMaterials();
            Material[] dimensionMaterials = new Material[sharedMaterials.Length];
            for (int i = 0; i < sharedMaterials.Length; i++) {
                Material originalMaterial = sharedMaterials[i];
                dimensionMaterials[i] = GetOrCreateDimensionObjectMaterial(originalMaterial);
            }
            r.SetSharedMaterials(dimensionMaterials, false);
            r.SetFloatArray(MASK_SOLUTION_PROPERTY_KEY, bitmask.ShaderData);
        }

        /// <summary>
        /// Get or create a DimensionObject Material for a given original Material
        /// </summary>
        /// <param name="originalMaterial">The original Material to get or create a DimensionObject version of</param>
        /// <returns>A shared Material based on the original Material with the DIMENSION_OBJECT keyword enabled</returns>
        public Material GetOrCreateDimensionObjectMaterial(Material originalMaterial) {
            if (originalMaterial.name.Contains(DIMENSION_OBJECT_SUFFIX)) {
                // If this material is already a DimensionObject material, we don't need to do anything
                return originalMaterial;
            }
                
            // Check our cache to see if we've already created a material for this original material
            if (!dimensionObjectMaterials.TryGetValue(originalMaterial, out Material dimensionObjectMaterial)) {
                // If we haven't created a material for this original material yet, create one now and set the DIMENSION_OBJECT keyword
                dimensionObjectMaterial = new Material(originalMaterial);
                dimensionObjectMaterial.name += DIMENSION_OBJECT_SUFFIX;
                dimensionObjectMaterial.EnableKeyword(DIMENSION_OBJECT_KEYWORD);
                return dimensionObjectMaterial;
            }

            return dimensionObjectMaterial;
        }

        /// <summary>
        /// Get all DimensionObjects affecting a Renderer
        /// </summary>
        /// <param name="r">Renderer to get DimensionObjects for</param>
        /// <returns>ListHashSet of DimensionObjects affecting the Renderer</returns>
        public ListHashSet<DimensionObject> GetDimensionObjectsAffectingRenderer(SuperspectiveRenderer r) {
            return renderersAffectedByDimensionObjects[r];
        }
#endregion

#region Physics

        /// <summary>
        /// Refresh the colliders for a DimensionObject, updating their collision state based on ALL the DimensionObjects affecting each of them.
        /// </summary>
        /// <param name="dimensionObject">DimensionObject to refresh colliders for</param>
        public void RefreshCollidersForDimensionObject(DimensionObject dimensionObject) {
            foreach (Collider c in dimensionObject.colliders) {
                if (collidersAffectedByDimensionObjects[c].Contains(dimensionObject)) {
                    RefreshCollider(c, dimensionObject.ID);
                }
            }
        }

        /// <summary>
        /// Refresh the physics of a Collider, updating its collision state based on the DimensionObjects affecting it.
        /// </summary>
        /// <param name="c">Collider to refresh physics for</param>
        /// <param name="identifier">Unique identifier for who is setting the collision state</param>
        public void RefreshCollider(Collider c, string identifier) {
            if (collisionsCache.TryGetValue(c, out Dictionary<Collider, CollisionCacheValue> cachedCollisions)) {
                // To avoid modifying the dictionary while iterating over it, we need to cache the keys
                var keys = cachedCollisions.Keys.ToArray();
                foreach (Collider otherCollider in keys) {
                    SetCollision(c, otherCollider, identifier, true);
                }
            }
        }

        /// <summary>
        /// Set the collision state of two Colliders based on the DimensionObjects affecting them.
        /// </summary>
        /// <param name="a">Collider 1</param>
        /// <param name="b">Collider 2</param>
        /// <param name="identifier">Unique identifier for who is setting the collision state</param>
        /// <param name="skipCache">If true, will force a recompute on the collision logic. Defaults to false.</param>
        public void SetCollision(Collider a, Collider b, string identifier, bool skipCache = false) {
            // Clean up the cache if either collider is null
            if (!a || !b) {
                if (!a) {
                    collisionsCache.RemoveCollider(a);
                }
                if (!b) {
                    collisionsCache.RemoveCollider(b);
                }
                return;
            }
                
            bool aIsAffected = collidersAffectedByDimensionObjects.ContainsKey(a);
            bool bIsAffected = collidersAffectedByDimensionObjects.ContainsKey(b);
            ListHashSet<DimensionObject> aDimensionObjects;
            ListHashSet<DimensionObject> bDimensionObjects = new ListHashSet<DimensionObject>();
            
            bool CollidersShouldInteract() {
                // If we have the value cached, use that
                if (!skipCache && collisionsCache.TryGetValue(a, b, out CollisionCacheValue cachedCollision)) {
                    switch (cachedCollision) {
                        case CollisionCacheValue.CollisionIgnored:
                            return false;
                        case CollisionCacheValue.CollisionNotIgnored:
                            return true;
                        default:
                            throw new Exception("Unhandled CollisionCacheValue: " + cachedCollision);
                    }
                }
                
                // If neither collider is affected by any DimensionObjects, they should interact
                if (!aIsAffected && !bIsAffected) {
                    collisionsCache.AddCollision(a, b, CollisionCacheValue.CollisionNotIgnored);
                    return true;
                }

                // If one collider is affected and the other isn't, we need to check if the affected collider should interact with non-DimensionObjects
                if (aIsAffected != bIsAffected) {
                    Collider affected = aIsAffected ? a : b;
                    Collider unaffected = aIsAffected ? b : a;
                    
                    ListHashSet<DimensionObject> affectingDimensionObjects = collidersAffectedByDimensionObjects[affected];
                    // Avoid using LINQ to avoid heap allocations
                    foreach (DimensionObject dimObj in affectingDimensionObjects) {
                        if (!dimObj.ShouldCollideWithNonDimensionCollider(unaffected)) {
                            collisionsCache.AddCollision(a, b, CollisionCacheValue.CollisionIgnored);
                            return false;
                        }
                    }
                    collisionsCache.AddCollision(a, b, CollisionCacheValue.CollisionNotIgnored);
                    return true;
                }
                
                // If both colliders are affected, we need to check if they should interact with each other
                aDimensionObjects = collidersAffectedByDimensionObjects[a];
                bDimensionObjects = collidersAffectedByDimensionObjects[b];

                bool channelOverlapExists = false;
                foreach (DimensionObject aDimensionObject in aDimensionObjects) {
                    // Check if all bDimensionObjects satisfy ShouldCollideWithDimensionObject
                    foreach (DimensionObject bDimensionObject in bDimensionObjects) {
                        if (!aDimensionObject.HasChannelOverlapWith(bDimensionObject)) continue;
                        channelOverlapExists = true;
                        
                        if (!aDimensionObject.ShouldCollideWithDimensionObject(bDimensionObject)) {
                            // If any collision check fails, return false immediately
                            collisionsCache.AddCollision(a, b, CollisionCacheValue.CollisionIgnored);
                            return false;
                        }
                    }
                }
                // If all checks pass, return true if some channel overlap exists, false otherwise
                collisionsCache.AddCollision(a, b, channelOverlapExists ? CollisionCacheValue.CollisionNotIgnored : CollisionCacheValue.CollisionIgnored);
                return channelOverlapExists;
            }
            
            bool collidersShouldInteract = CollidersShouldInteract();
            if (collidersShouldInteract) {
                SuperspectivePhysics.RestoreCollision(a, b, identifier);
                // If a and b are both affected by DimensionObjects, we need to symmetrically restore collision between them
                if (aIsAffected && bIsAffected) {
                    foreach (DimensionObject bDimensionObj in bDimensionObjects) {
                        SuperspectivePhysics.RestoreCollision(a, b, bDimensionObj.ID);
                    }
                }
            }
            else {
                SuperspectivePhysics.IgnoreCollision(a, b, identifier);
                if (aIsAffected && bIsAffected) {
                    foreach (DimensionObject bDimensionObj in bDimensionObjects) {
                        SuperspectivePhysics.IgnoreCollision(a, b, bDimensionObj.ID);
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if two Colliders should interact based on the DimensionObjects affecting them.
        /// a and b do not need to be registered with the DimensionObjectManager for this to work.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool CollidersShouldInteract(Collider a, Collider b, bool skipCache = false) {
            // If we have the value cached, use that
            if (!skipCache && collisionsCache.TryGetValue(a, b, out CollisionCacheValue cachedCollision)) {
                switch (cachedCollision) {
                    case CollisionCacheValue.CollisionIgnored:
                        return false;
                    case CollisionCacheValue.CollisionNotIgnored:
                        return true;
                    default:
                        throw new Exception("Unhandled CollisionCacheValue: " + cachedCollision);
                }
            }
            
            bool aIsAffected = collidersAffectedByDimensionObjects.ContainsKey(a);
            bool bIsAffected = collidersAffectedByDimensionObjects.ContainsKey(b);
            
            // If neither collider is affected by any DimensionObjects, they should interact
            if (!aIsAffected && !bIsAffected) {
                collisionsCache.AddCollision(a, b, CollisionCacheValue.CollisionNotIgnored);
                return true;
            }

            // If one collider is affected and the other isn't, we need to check if the affected collider should interact with non-DimensionObjects
            if (aIsAffected != bIsAffected) {
                Collider affected = aIsAffected ? a : b;
                Collider unaffected = aIsAffected ? b : a;
                
                ListHashSet<DimensionObject> affectingDimensionObjects = collidersAffectedByDimensionObjects[affected];
                // Avoid using LINQ to avoid heap allocations
                foreach (DimensionObject dimObj in affectingDimensionObjects) {
                    if (!dimObj.ShouldCollideWithNonDimensionCollider(unaffected)) {
                        collisionsCache.AddCollision(a, b, CollisionCacheValue.CollisionIgnored);
                        return false;
                    }
                }
                collisionsCache.AddCollision(a, b, CollisionCacheValue.CollisionNotIgnored);
                return true;
            }
            
            // If both colliders are affected, we need to check if they should interact with each other
            ListHashSet<DimensionObject> aDimensionObjects = collidersAffectedByDimensionObjects[a];
            ListHashSet<DimensionObject> bDimensionObjects = collidersAffectedByDimensionObjects[b];

            foreach (DimensionObject aDimensionObject in aDimensionObjects) {
                // Check if all bDimensionObjects satisfy ShouldCollideWithDimensionObject
                foreach (DimensionObject bDimensionObject in bDimensionObjects) {
                    if (!aDimensionObject.HasChannelOverlapWith(bDimensionObject)) continue;
                    
                    if (!aDimensionObject.ShouldCollideWithDimensionObject(bDimensionObject)) {
                        // If any collision check fails, return false immediately
                        collisionsCache.AddCollision(a, b, CollisionCacheValue.CollisionIgnored);
                        return false;
                    }
                }
            }
            // If all checks pass, return true
            collisionsCache.AddCollision(a, b, CollisionCacheValue.CollisionNotIgnored);
            return true;
        }

        /// <summary>
        /// Check if a Collider is hit by Player camera Interact raycast, considering all DimensionObjects affecting it.
        /// </summary>
        /// <param name="c">Collider to test raycast for</param>
        /// <returns></returns>
        public bool RaycastHitCollider(Collider c) {
            // If this collider isn't affected by any DimensionObjects, we can skip this check
            if (!collidersAffectedByDimensionObjects.ContainsKey(c)) return true;
            
            // Get the visibility bitmask value at the cursor position
            int maskValue = MaskBufferRenderTextures.instance.visibilityMaskValue;
            
            // Get the combined/effective bitmask for this collider
            ListHashSet<DimensionObject> allDimensionObjects = collidersAffectedByDimensionObjects[c];
            ListHashSet<DimensionObject> dimensionObjects = new ListHashSet<DimensionObject>();
            for (int i = 0; i < allDimensionObjects.Count; i++) {
                DimensionObject dimObj = allDimensionObjects[i];
                // Add only if bypassRaycastCheck is false
                if (!dimObj.bypassRaycastCheck) {
                    dimensionObjects.Add(dimObj);
                }
            }
            (bool anyIsInvisible, DimensionObjectBitmask finalBitmask) = GetDimensionObjectBitmask(dimensionObjects);

            if (anyIsInvisible) return false;
            
            // Check if the maskValue is in the finalBitmask
            return finalBitmask.Test(maskValue);
        }

#endregion
        
#region Utility Methods
        
        /// <summary>
        /// Refreshes the rendering and physics of all Renderers and Colliders of a DimensionObject
        /// </summary>
        /// <param name="dimensionObject">DimensionObject to refresh rendering and physics for</param>
        public void RefreshDimensionObject(DimensionObject dimensionObject) {
            RefreshRenderersForDimensionObject(dimensionObject);
            RefreshCollidersForDimensionObject(dimensionObject);
        }

        // Only used for debugging
        public DimensionObjectBitmask GetDimensionObjectBitmask(SuperspectiveRenderer r) {
            if (!renderersAffectedByDimensionObjects.ContainsKey(r)) return DimensionObjectBitmask.Zero;
            return GetDimensionObjectBitmask(renderersAffectedByDimensionObjects[r]).Item2;
        }
        
        /// <summary>
        /// Get the combined DimensionObject bitmask for a list of DimensionObjects
        /// </summary>
        /// <param name="dimensionObjects">List of DimensionObjects to get the bitmask for</param>
        /// <returns>(anyIsInvisible, combined DimensionObjectBitmask) result of combining the DimensionObjects</returns>
        private (bool, DimensionObjectBitmask) GetDimensionObjectBitmask(ListHashSet<DimensionObject> dimensionObjects) {
            // Keep track of any DimensionObjects in the PartiallyVisible or PartiallyInvisible state
            List<DimensionObject> partiallyVisibleDimensionObjects = new List<DimensionObject>();
            foreach (DimensionObject dimensionObject in dimensionObjects) {
                // If any of the DimensionObjects affecting this renderer are Invisible, the end result is invisible and we can stop looking
                if (dimensionObject.EffectiveVisibilityState == VisibilityState.Invisible) {
                    return (true, DimensionObjectBitmask.Zero);
                }
                
                if (dimensionObject.visibilityState is VisibilityState.PartiallyVisible or VisibilityState.PartiallyInvisible) {
                    partiallyVisibleDimensionObjects.Add(dimensionObject);
                }
            }
            
            /* Had to remove this beautiful LINQ code because it allocates memory :(
            DimensionObjectBitmask finalBitmask = partiallyVisibleDimensionObjects
                // Use either the inverse or regular mask solution based on the DimensionObject's InverseShouldBeEnabled property
                .Select(dimObj => dimObj.EffectiveMaskSolution)
                // Aggregate all the masks into a single bitmask
                .Aggregate(DimensionObjectBitmask.One, (a, b) => a & b);
            */
            DimensionObjectBitmask finalBitmask = DimensionObjectBitmask.One;

            // For every partially visible/invisible DimensionObject,
            // we need to pass all of their DimensionObject data to the shader as a combined bitmask
            for (int i = 0; i < partiallyVisibleDimensionObjects.Count; i++) {
                DimensionObject dimObj = partiallyVisibleDimensionObjects[i];
                // Perform the bitwise AND operation
                finalBitmask &= dimObj.EffectiveMaskSolution;
            }
            

            return (false, finalBitmask);
        }
#endregion
    }
}

#region Collisions Cache
public enum CollisionCacheValue {
    NotCached,
    CollisionIgnored,
    CollisionNotIgnored
}

public class CollisionsCache {
    private readonly Dictionary<Collider, Dictionary<Collider, CollisionCacheValue>> collisionsCache = new Dictionary<Collider, Dictionary<Collider, CollisionCacheValue>>();
    
    /// <summary>
    /// Try to get the value of a cached collision check
    /// </summary>
    /// <param name="key">One of the colliders involved in the cached collision data</param>
    /// <param name="otherKey">The other collider involved in the cached collision data</param>
    /// <param name="value">Will be set to CollisionIgnored if cached collision is ignored, CollisionNotIgnored if the cached collision is not ignored, NotCached otherwise</param>
    /// <returns>True if collision (either true or false) is cached, false otherwise</returns>
    public bool TryGetValue(Collider key, Collider otherKey, out CollisionCacheValue value) {
        if (collisionsCache.TryGetValue(key, out Dictionary<Collider, CollisionCacheValue> valueDict)) {
            return valueDict.TryGetValue(otherKey, out value);
        }
        value = CollisionCacheValue.NotCached;
        return false;
    }
    
    public bool TryGetValue(Collider key, out Dictionary<Collider, CollisionCacheValue> value) {
        return collisionsCache.TryGetValue(key, out value);
    }
    
    /// <summary>
    /// Symmetrically add a collision check to the cache
    /// </summary>
    /// <param name="key">One of the colliders involved in the cached collision data</param>
    /// <param name="otherKey">The other collider involved in the cached collision data</param>
    /// <param name="value">CollisionIgnored or CollisionNotIgnored</param>
    public void AddCollision(Collider key, Collider otherKey, CollisionCacheValue value) {
        if (value == CollisionCacheValue.NotCached) {
            throw new ArgumentException("Cannot add a NotCached value to the cache, use RemoveCollider instead");
        }
        
        if (!collisionsCache.ContainsKey(key)) {
            collisionsCache[key] = new Dictionary<Collider, CollisionCacheValue>();
        }
        collisionsCache[key][otherKey] = value;
        if (!collisionsCache.ContainsKey(otherKey)) {
            collisionsCache[otherKey] = new Dictionary<Collider, CollisionCacheValue>();
        }
        collisionsCache[otherKey][key] = value;
    }
        
    public void RemoveCollider(Collider collider) {
        if (collisionsCache.TryGetValue(collider, out Dictionary<Collider, CollisionCacheValue> collisions)) {
            // Before removing the collider, we need to remove it from all other colliders' collision checks
            List<Collider> keys = new List<Collider>(collisions.Keys);
            foreach (Collider otherCollider in keys) {
                if (collisionsCache.TryGetValue(otherCollider, out Dictionary<Collider, CollisionCacheValue> otherCollisions)) {
                    otherCollisions.Remove(collider);
                }
            }
                
            collisionsCache.Remove(collider);
        }
    }
}
#endregion

// Custom data structure that combines the functionality of a List and a HashSet for fast lookup and iteration
// Tradeoff is it's slightly slower for some operations like removal
public class ListHashSet<T> : IEnumerable<T> {
    private readonly List<T> list = new List<T>();
    private readonly HashSet<T> hashSet = new HashSet<T>();

    // Add an item if it doesn't already exist
    public bool Add(T item) {
        if (hashSet.Add(item)) { // Adds to HashSet, returns true if it was not already in the set
            list.Add(item);      // Add to the List if it's new
            return true;
        }
        return false; // Item was already present
    }

    // Remove an item
    public bool Remove(T item) {
        if (hashSet.Remove(item)) {  // Removes from HashSet
            list.Remove(item);       // Removes from List
            return true;
        }
        return false; // Item was not present
    }

    // Indexer to access elements like a List
    public T this[int index] => list[index];

    // Fast lookup for whether an item exists
    public bool Contains(T item) {
        return hashSet.Contains(item);
    }

    // The number of items in the collection
    public int Count => list.Count;

    // Clear the collection
    public void Clear() {
        list.Clear();
        hashSet.Clear();
    }

    // IEnumerable implementation for foreach loops
    public IEnumerator<T> GetEnumerator() {
        return list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return list.GetEnumerator();
    }
}
