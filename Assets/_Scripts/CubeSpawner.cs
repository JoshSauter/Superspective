using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LevelManagement;
using SuperspectiveUtils;
using NaughtyAttributes;
using Saving;
using SerializableClasses;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using PickupObjectReference = SerializableClasses.SerializableReference<PickupObject, PickupObject.PickupObjectSave>;

[RequireComponent(typeof(UniqueId))]
public class CubeSpawner : SaveableObject<CubeSpawner, CubeSpawner.CubeSpawnerSave> {
    public enum GrowCubeState {
        Idle,
        Growing,
        Grown
    }

    public enum ShrinkCubeState {
        Idle,
        Shrinking
    }

    const float growTime = 0.75f;
    const float shrinkTime = 0.4f;

    public int maxNumberOfCubesThatCanBeSpawned = 1;
    public DynamicObject cubePrefab;
    public AnimationCurve cubeGrowCurve;
    public AnimationCurve cubeShrinkCurve;

    [ReadOnly]
    public Collider waterCollider;

    [ReadOnly]
    public PickupObject objectBeingSuspended;

    [ReadOnly]
    public Rigidbody rigidbodyOfObjectBeingSuspended;

    public int baseDimensionForCubes;
    GrowCubeState _growCubeState;
    ShrinkCubeState _shrinkCubeState;
    Vector3 growEndSize;
    Queue<PickupObjectReference> objectsGrabbedFromSpawner = new Queue<PickupObjectReference>();
    PickupObject objectShrinking;
    Vector3 shrinkStartSize;
    Collider thisCollider;
    float timeSinceGrowCubeStateChanged;
    float timeSinceShrinkCubeStateChanged;

    public GrowCubeState growCubeState {
        get => _growCubeState;
        set {
            if (value != _growCubeState) {
                timeSinceGrowCubeStateChanged = 0f;
                _growCubeState = value;
            }
        }
    }

    public ShrinkCubeState shrinkCubeState {
        get => _shrinkCubeState;
        set {
            if (value != _shrinkCubeState) {
                timeSinceShrinkCubeStateChanged = 0f;
                _shrinkCubeState = value;
            }
        }
    }

    private bool ReferenceIsReplaceable(PickupObjectReference obj) {
        return obj.Reference.Match(
            pickupObject => pickupObject.isReplaceable,
            saveObject => saveObject.isReplaceable
        );
    }
    
    [ShowNativeProperty]
    int numberOfReplaceableCubes => objectsGrabbedFromSpawner.Count(ReferenceIsReplaceable);

    [ShowNativeProperty]
    int numberOfIrreplaceableCubes => objectsGrabbedFromSpawner.Count(reference => !ReferenceIsReplaceable(reference));

    private bool ReplaceableCube(PickupObject o) {
        // Objects may be null if they're moved to another scene
        return o == null || o.isReplaceable;
    }

    protected override void Awake() {
        base.Awake();
        
        thisCollider = GetComponent<Collider>();
    }

    void Update() {
        if (!hasInitialized || GameManager.instance.IsCurrentlyLoading) return;

        if (objectBeingSuspended == null && numberOfIrreplaceableCubes < maxNumberOfCubesThatCanBeSpawned)
            SpawnNewCube();

        if (objectBeingSuspended != null)
            objectBeingSuspended.gameObject.SetActive(numberOfIrreplaceableCubes < maxNumberOfCubesThatCanBeSpawned);

        UpdateGrowCube(objectBeingSuspended);
        UpdateShrinkAndDestroyCube(objectShrinking);
    }

    void FixedUpdate() {
        if (objectBeingSuspended != null) {
            rigidbodyOfObjectBeingSuspended.useGravity = false;
            if (!objectBeingSuspended.isHeld) {
                Vector3 center = thisCollider.bounds.center;
                Vector3 objPos = objectBeingSuspended.transform.position;
                rigidbodyOfObjectBeingSuspended.AddForce(750 * (center - objPos), ForceMode.Force);
                rigidbodyOfObjectBeingSuspended.AddTorque(10 * Vector3.up, ForceMode.Force);
            }
        }
    }

    void OnTriggerExit(Collider other) {
        PickupObject movableObject = other.gameObject.GetComponent<PickupObject>();
        if (movableObject == objectBeingSuspended) {
            if (movableObject.isHeld) {
                objectsGrabbedFromSpawner.Enqueue(movableObject);
                movableObject.OnPickupSimple -= DestroyCubeAlreadyGrabbedFromSpawner;
                SpawnNewCube();

                Physics.IgnoreCollision(waterCollider, movableObject.GetComponent<Collider>(), false);
            }
        }
    }

    void SpawnNewCube() {
        const float randomizeOffset = 1.0f;
        PickupObject newCube = Instantiate(cubePrefab, transform).GetComponent<PickupObject>();
        newCube.transform.SetParent(null);
        newCube.transform.position =
            thisCollider.bounds.center + Random.insideUnitSphere * Random.Range(0, randomizeOffset);
        objectBeingSuspended = newCube;
        SceneManager.MoveGameObjectToScene(newCube.gameObject, gameObject.scene);
        newCube.GetComponent<GravityObject>().useGravity = false;

        PillarDimensionObject[] dimensionObjs =
            newCube.transform.GetComponentsInChildrenRecursively<PillarDimensionObject>();
        foreach (PillarDimensionObject newCubeDimensionObj in dimensionObjs) {
            newCubeDimensionObj.Dimension = baseDimensionForCubes;
        }

        Physics.IgnoreCollision(waterCollider, newCube.GetComponent<Collider>(), true);
        rigidbodyOfObjectBeingSuspended = newCube.thisRigidbody;
        rigidbodyOfObjectBeingSuspended.useGravity = false;

        // TODO: Make this happen when the cube is pulled out rather than when it is clicked on
        objectBeingSuspended.OnPickupSimple += DestroyCubeAlreadyGrabbedFromSpawner;

        growEndSize = newCube.transform.localScale;
        growCubeState = GrowCubeState.Growing;
    }

    public void DestroyCubeAlreadyGrabbedFromSpawner() {
        if (numberOfReplaceableCubes > 0 && objectsGrabbedFromSpawner.Count == maxNumberOfCubesThatCanBeSpawned) {
            PickupObjectReference objToDestroy = objectsGrabbedFromSpawner.First(ReferenceIsReplaceable);
            objToDestroy.Reference.MatchAction(
                // If the object is loaded, play the shrink animation for this cube
                pickupObject => {
                    objectShrinking = pickupObject;
                    
                    shrinkStartSize = objectShrinking.transform.localScale;
                    shrinkCubeState = ShrinkCubeState.Shrinking;
                },
                // If the object is not loaded, just destroy it directly
                saveObject => saveObject.Destroy()
                );
            // Workaround because the Queue class has no built-in support for removing specific elements
            objectsGrabbedFromSpawner =
                new Queue<PickupObjectReference>(objectsGrabbedFromSpawner.Where(o => o != objToDestroy));
        }
    }

    void UpdateGrowCube(PickupObject cube) {
        switch (growCubeState) {
            case GrowCubeState.Idle:
            case GrowCubeState.Grown:
                break;
            case GrowCubeState.Growing:
                // Don't allow growing cubes to be picked up (don't want to break some logic and end up with a mis-sized cube)
                cube.interactable = false;
                if (timeSinceGrowCubeStateChanged == 0) cube.transform.localScale = Vector3.zero;
                if (timeSinceGrowCubeStateChanged < growTime) {
                    timeSinceGrowCubeStateChanged += Time.deltaTime;
                    float t = Mathf.Clamp01(timeSinceGrowCubeStateChanged / growTime);

                    cube.transform.localScale = growEndSize * cubeGrowCurve.Evaluate(t);
                }
                else {
                    cube.transform.localScale = growEndSize;
                    cube.interactable = true;
                    growCubeState = GrowCubeState.Grown;
                }

                break;
        }
    }

    void UpdateShrinkAndDestroyCube(PickupObject cube) {
        if (shrinkCubeState == ShrinkCubeState.Shrinking) {
            // Don't allow shrinking cubes to be picked up
            cube.interactable = false;
            // Trick to get the cube to not interact with the player anymore but still collide with ground
            cube.gameObject.layer = LayerMask.NameToLayer("VisibleButNoPlayerCollision");
            cube.thisRigidbody.useGravity = false;

            if (timeSinceShrinkCubeStateChanged < shrinkTime) {
                timeSinceShrinkCubeStateChanged += Time.deltaTime;
                float t = Mathf.Clamp01(timeSinceShrinkCubeStateChanged / shrinkTime);

                cube.transform.localScale = Vector3.LerpUnclamped(
                    shrinkStartSize,
                    Vector3.zero,
                    cubeShrinkCurve.Evaluate(t)
                );
            }
            else {
                cube.transform.localScale = Vector3.zero;
                cube.GetComponent<DynamicObject>().Destroy();
                objectShrinking = null;
                shrinkCubeState = ShrinkCubeState.Idle;
            }
        }
    }

#region Saving

    [Serializable]
    public class CubeSpawnerSave : SerializableSaveObject<CubeSpawner> {
        int baseDimensionForCubes;
        SerializableAnimationCurve cubeGrowCurve;
        SerializableAnimationCurve cubeShrinkCurve;
        PickupObjectReference objectBeingSuspended;
        Queue<PickupObjectReference> objectsGrabbedFromSpawner;

        public CubeSpawnerSave(CubeSpawner spawner) : base(spawner) {
            cubeGrowCurve = spawner.cubeGrowCurve;
            cubeShrinkCurve = spawner.cubeShrinkCurve;
            objectBeingSuspended = spawner.objectBeingSuspended;
            objectsGrabbedFromSpawner = spawner.objectsGrabbedFromSpawner;
            baseDimensionForCubes = spawner.baseDimensionForCubes;
        }

        public override void LoadSave(CubeSpawner spawner) {
            spawner.cubeGrowCurve = cubeGrowCurve;
            spawner.cubeShrinkCurve = cubeShrinkCurve;
            spawner.objectBeingSuspended = objectBeingSuspended.GetOrNull();
            spawner.objectsGrabbedFromSpawner = this.objectsGrabbedFromSpawner;
            if (spawner.objectBeingSuspended != null)
                spawner.rigidbodyOfObjectBeingSuspended = spawner.objectBeingSuspended.GetComponent<Rigidbody>();
            spawner.baseDimensionForCubes = baseDimensionForCubes;
        }
    }
#endregion
}