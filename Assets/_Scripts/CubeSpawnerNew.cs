using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Saving;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using PickupObjectReference = SerializableClasses.SerializableReference<PickupObject, PickupObject.PickupObjectSave>;

public class CubeSpawnerNew : MonoBehaviour {
    public DimensionObject backWall;
    public Button button;
    const float spawnOffset = 12;
    static float timeToFallHalfway = Mathf.Sqrt(spawnOffset / Physics.gravity.magnitude);
    public DynamicObject cubePrefab;
    public PickupObject cubeSpawned;
    [Header("Cube grabbed from spawner:")]
    public PickupObjectReference cubeGrabbedFromSpawner;
    public Collider roofCollider;
    public Transform glass;
    const float glassLowerDelay = 1.75f;
    const float glassRaiseDelay = 0.25f;
    const float glassOffset = 1.5f;
    const float glassMoveTime = 1.2f;
    float startHeight;

    enum State {
        // Initial state
        NoCubeSpawned,
        // After button is pressed but before cube is removed from spawner
        CubeSpawnedButNotTaken,
        // After cube is removed from spawner area
        CubeTaken
    }

    float timeSinceStateChanged = 0f;
    [ReadOnly]
    [SerializeField]
    State _state = State.NoCubeSpawned;
    State state {
        get { return _state; }
        set {
            if (value != _state) {
                _state = value;
                timeSinceStateChanged = 0f;
            }
        }
    }

    PickupObject cubeShrinking;
    float timeSinceCubeShrink = 0f;
    const float shrinkTime = 0.4f;
    Vector3 shrinkStartSize = Vector3.one;
    public AnimationCurve cubeShrinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    void Awake() {
        button.OnButtonPressBegin += (_) => SpawnNewCube();
        button.OnButtonUnpressBegin += (_) => DestroyCubeAlreadyGrabbedFromSpawner();

        startHeight = glass.transform.localPosition.y;
    }

    bool ReferenceIsReplaceable(PickupObjectReference obj) {
        return obj.Reference.Match(
            pickupObject => pickupObject.isReplaceable,
            saveObject => saveObject.isReplaceable
        );
    }

    void Update() {
        timeSinceStateChanged += Time.deltaTime;

        // Make it so the button is only clickable when it will do something (spawn a cube or destroy a spawned cube)
        button.interactableObject.interactable = state != State.CubeSpawnedButNotTaken;
        
        switch (state) {
            case State.NoCubeSpawned:
                break;
            case State.CubeSpawnedButNotTaken:
                // Restore collision for the cube halfway through its fall
                if (timeSinceStateChanged > timeToFallHalfway) {
                    foreach (var newCubeColliders in cubeSpawned.GetComponentInParent<DimensionObject>().colliders) {
                        newCubeColliders.enabled = true;
                    }
                }
                else {
                    // Disable new cube's colliders for the first half of the fall
                    foreach (var newCubeColliders in cubeSpawned.GetComponentInParent<DimensionObject>().colliders) {
                        newCubeColliders.enabled = false;
                    }
                }

                if (timeSinceStateChanged > glassLowerDelay && timeSinceStateChanged < glassMoveTime + glassLowerDelay) {
                    float time = timeSinceStateChanged - glassLowerDelay;
                    float t = time / glassMoveTime;
                    
                    Vector3 startPos = new Vector3(glass.localPosition.x, startHeight, glass.localPosition.z);
                    Vector3 endPos = new Vector3(glass.localPosition.x, startHeight-glassOffset, glass.localPosition.z);
                    glass.localPosition = Vector3.Lerp(startPos, endPos, t*t);
                }
                break;
            case State.CubeTaken:
                if (timeSinceStateChanged > glassRaiseDelay && timeSinceStateChanged < glassMoveTime + glassRaiseDelay) {
                    float time = timeSinceStateChanged - glassRaiseDelay;
                    float t = time / glassMoveTime;

                    Vector3 startPos = new Vector3(glass.localPosition.x, startHeight, glass.localPosition.z);
                    Vector3 endPos = new Vector3(glass.localPosition.x, startHeight-glassOffset, glass.localPosition.z);
                    glass.localPosition = Vector3.Lerp(endPos, startPos, t*t);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        UpdateShrinkAndDestroyCube(cubeShrinking);
    }

    void SpawnNewCube() {
        if (state != State.NoCubeSpawned) return;

        PickupObject newCube = Instantiate(cubePrefab, transform).GetComponent<PickupObject>();
        newCube.transform.SetParent(null);
        newCube.transform.localScale = cubePrefab.transform.localScale;
        newCube.transform.position = transform.position + transform.up * spawnOffset;
        newCube.transform.Rotate(Random.insideUnitSphere.normalized, Random.Range(0, 20f));
        newCube.thisGravity.gravityDirection = -transform.up;
        SceneManager.MoveGameObjectToScene(newCube.gameObject, gameObject.scene);

        // Create a parent object to place the DimensionObject script on that the cube will be a child of
        GameObject cubeParent = new GameObject("CubeDimensionObjectParent");
        DimensionObject parentDimensionObj = cubeParent.AddComponent<DimensionObject>();
        //parentDimensionObj.DEBUG = true;
        parentDimensionObj.startingVisibilityState = VisibilityState.partiallyVisible;
        parentDimensionObj.treatChildrenAsOneObjectRecursively = true;
        parentDimensionObj.ignoreChildrenWithDimensionObject = false;
        parentDimensionObj.channel = backWall.channel;
        cubeParent.name = "CubeDimensionObjectParent";
        cubeParent.transform.position = newCube.transform.position;
        cubeParent.transform.rotation = newCube.transform.rotation;
        SceneManager.MoveGameObjectToScene(cubeParent, gameObject.scene);
        
        newCube.transform.SetParent(cubeParent.transform);
        parentDimensionObj.InitializeRenderersAndLayers();
        parentDimensionObj.SwitchVisibilityState(VisibilityState.partiallyVisible, true);
        parentDimensionObj.SetCollision(VisibilityState.visible, VisibilityState.partiallyVisible, true);
        
        Physics.IgnoreCollision(roofCollider, newCube.GetComponent<Collider>(), true);
        
        PillarDimensionObject[] dimensionObjs =
            newCube.transform.GetComponentsInChildrenRecursively<PillarDimensionObject>();
        foreach (PillarDimensionObject newCubeDimensionObj in dimensionObjs) {
            newCubeDimensionObj.channel = backWall.channel;
        }

        cubeSpawned = newCube;
        state = State.CubeSpawnedButNotTaken;
    }
    
    void DestroyCubeAlreadyGrabbedFromSpawner() {
        if (state == State.CubeTaken && ReferenceIsReplaceable(cubeGrabbedFromSpawner)) {
            cubeGrabbedFromSpawner.Reference.MatchAction(
                // If the object is loaded, play the shrink animation for this cube
                pickupObject => {
                    cubeShrinking = pickupObject;
                    
                    shrinkStartSize = cubeShrinking.transform.localScale;
                    timeSinceCubeShrink = 0f;
                },
                // If the object is not loaded, just destroy it directly
                saveObject => saveObject.Destroy()
            );

            cubeGrabbedFromSpawner = null;
            state = State.NoCubeSpawned;
        }
    }
    
    void UpdateShrinkAndDestroyCube(PickupObject cube) {
        if (cube != null) {
            // Don't allow shrinking cubes to be picked up
            cube.interactable = false;
            // Trick to get the cube to not interact with the player anymore but still collide with ground
            cube.gameObject.layer = LayerMask.NameToLayer("VisibleButNoPlayerCollision");
            cube.thisRigidbody.useGravity = false;

            if (timeSinceCubeShrink < shrinkTime) {
                timeSinceCubeShrink += Time.deltaTime;
                float t = Mathf.Clamp01(timeSinceCubeShrink / shrinkTime);

                cube.transform.localScale = Vector3.LerpUnclamped(
                    shrinkStartSize,
                    Vector3.zero,
                    cubeShrinkCurve.Evaluate(t)
                );
            }
            else {
                cube.transform.localScale = Vector3.zero;
                cube.GetComponent<DynamicObject>().Destroy();
                cubeShrinking = null;
            }
        }
    }

    void OnTriggerExit(Collider other) {
        PickupObject cube = other.GetComponent<PickupObject>();
        if (cube != null && cube == cubeSpawned) {
            state = State.CubeTaken;
            
            // Restore collision with the roof of the Cube Spawner when the cube is taken from the spawner
            Physics.IgnoreCollision(roofCollider, cube.GetComponent<Collider>(), false);

            // When the cube is removed from the spawner, reset its DimensionObject channel to their default
            DimensionObject originalDimensionObj = cubePrefab.GetComponentInChildren<DimensionObject>();
            if (originalDimensionObj != null) {
                PillarDimensionObject[] dimensionObjs =
                    cube.transform.GetComponentsInChildrenRecursively<PillarDimensionObject>();
                foreach (PillarDimensionObject newCubeDimensionObj in dimensionObjs) {
                    newCubeDimensionObj.channel = originalDimensionObj.channel;
                }
            }
            
            // Also remove the dimension parent object
            DimensionObject dimensionParent = cube.transform.parent.GetComponent<DimensionObject>();
            dimensionParent.SwitchVisibilityState(VisibilityState.visible, true);
            dimensionParent.Unregister();
            Transform parent = cube.transform.parent;
            cube.transform.SetParent(null);
            Destroy(parent.gameObject);

            cubeSpawned = null;
            cubeGrabbedFromSpawner = cube;
        }
    }
}
