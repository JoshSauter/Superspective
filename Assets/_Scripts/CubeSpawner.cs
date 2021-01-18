using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EpitaphUtils;
using NaughtyAttributes;
using Saving;
using SerializableClasses;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

[RequireComponent(typeof(UniqueId))]
public class CubeSpawner : MonoBehaviour, SaveableObject {
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
    public PickupObject cubePrefab;
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
    UniqueId _id;
    ShrinkCubeState _shrinkCubeState;
    Vector3 growEndSize;
    Queue<PickupObject> objectsGrabbedFromSpawner = new Queue<PickupObject>();
    PickupObject objectShrinking;
    Vector3 shrinkStartSize;
    Collider thisCollider;
    float timeSinceGrowCubeStateChanged;
    float timeSinceShrinkCubeStateChanged;

    UniqueId id {
        get {
            if (_id == null) _id = GetComponent<UniqueId>();
            return _id;
        }
    }

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

    [ShowNativeProperty]
    int numberOfReplaceableCubes => objectsGrabbedFromSpawner.Count(o => o.isReplaceable);

    [ShowNativeProperty]
    int numberOfIrreplaceableCubes => objectsGrabbedFromSpawner.Count(o => !o.isReplaceable);

    IEnumerator Start() {
        thisCollider = GetComponent<Collider>();
        yield return new WaitForSeconds(0.5f);
        if (objectBeingSuspended == null) SpawnNewCube();
    }

    void Update() {
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
        PickupObject newCube = Instantiate(
            cubePrefab,
            thisCollider.bounds.center + Random.insideUnitSphere * Random.Range(0, randomizeOffset),
            new Quaternion()
        );
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
            objectShrinking = objectsGrabbedFromSpawner.First(o => o.isReplaceable);
            // Workaround because the Queue class has no built-in support for removing specific elements
            objectsGrabbedFromSpawner =
                new Queue<PickupObject>(objectsGrabbedFromSpawner.Where(o => o != objectShrinking));

            shrinkStartSize = objectShrinking.transform.localScale;
            shrinkCubeState = ShrinkCubeState.Shrinking;
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
                Destroy(cube.gameObject);
                objectShrinking = null;
                shrinkCubeState = ShrinkCubeState.Idle;
            }
        }
    }

#region Saving
    public bool SkipSave { get; set; }

    // All components on PickupCubes share the same uniqueId so we need to qualify with component name
    public string ID => $"CubeSpawner_{id.uniqueId}";

    [Serializable]
    class CubeSpawnerSave {
        int baseDimensionForCubes;
        SerializableAnimationCurve cubeGrowCurve;
        SerializableAnimationCurve cubeShrinkCurve;
        SerializableReference<PickupObject> objectBeingSuspended;
        Queue<SerializableReference<PickupObject>> objectsGrabbedFromSpawner;

        public CubeSpawnerSave(CubeSpawner spawner) {
            cubeGrowCurve = spawner.cubeGrowCurve;
            cubeShrinkCurve = spawner.cubeShrinkCurve;
            objectBeingSuspended = spawner.objectBeingSuspended;
            objectsGrabbedFromSpawner = new Queue<SerializableReference<PickupObject>>(
                spawner.objectsGrabbedFromSpawner.Select<PickupObject, SerializableReference<PickupObject>>(o => o)
            );
            baseDimensionForCubes = spawner.baseDimensionForCubes;
        }

        public void LoadSave(CubeSpawner spawner) {
            spawner.cubeGrowCurve = cubeGrowCurve;
            spawner.cubeShrinkCurve = cubeShrinkCurve;
            spawner.objectBeingSuspended = objectBeingSuspended;
            spawner.objectsGrabbedFromSpawner = new Queue<PickupObject>(
                objectsGrabbedFromSpawner.Select<SerializableReference<PickupObject>, PickupObject>(o => o)
            );
            if (spawner.objectBeingSuspended != null)
                spawner.rigidbodyOfObjectBeingSuspended = spawner.objectBeingSuspended.GetComponent<Rigidbody>();
            spawner.baseDimensionForCubes = baseDimensionForCubes;
        }
    }

    public object GetSaveObject() {
        return new CubeSpawnerSave(this);
    }

    public void LoadFromSavedObject(object savedObject) {
        CubeSpawnerSave save = savedObject as CubeSpawnerSave;

        save.LoadSave(this);
    }
#endregion
}