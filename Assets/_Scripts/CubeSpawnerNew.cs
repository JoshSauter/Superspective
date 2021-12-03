using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using LevelSpecific.Fork;
using NaughtyAttributes;
using Saving;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using PickupObjectReference = SerializableClasses.SerializableReference<PickupObject, PickupObject.PickupObjectSave>;

[RequireComponent(typeof(UniqueId))]
public class CubeSpawnerNew : SaveableObject<CubeSpawnerNew, CubeSpawnerNew.CubeSpawnerNewSave> {
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
        get => _state;
        set {
            if (value != _state) {
                _state = value;
                timeSinceStateChanged = 0f;
            }
        }
    }

    PickupObject cubeDespawning;
    float timeSinceCubeDespawnStart = 0f;
    const float despawnTime = 4f;
    Vector3 despawnStartSize = Vector3.one;
    private const float despawnEndSizeMultiplier = 4;
    AnimationCurve cubeDespawnSizeCurve = AnimationCurve.EaseInOut(0,0,1,1);

    protected override void Awake() {
        base.Awake();
        button.OnButtonPressBegin += (_) => SpawnNewCube();
        button.OnButtonUnpressBegin += (_) => DestroyCubeAlreadyGrabbedFromSpawner();

        startHeight = glass.transform.localPosition.y;
    }

    bool ReferenceIsReplaceable(PickupObjectReference obj) {
        return obj?.Reference?.Match(
            pickupObject => pickupObject.isReplaceable,
            saveObject => saveObject.isReplaceable
        ) ?? false;
    }

    void UpdateButtonInteractability() {
        if (state == State.CubeTaken && !ReferenceIsReplaceable(cubeGrabbedFromSpawner)) {
            button.interactableObject.SetAsDisabled();
        }
        else if (state == State.CubeSpawnedButNotTaken) {
            button.interactableObject.SetAsHidden();
        }
        else {
            button.interactableObject.SetAsInteractable();
        }
    }

    void Update() {
        timeSinceStateChanged += Time.deltaTime;

        // Make it so the button is only clickable when it will do something (spawn a cube or destroy a spawned cube)
        UpdateButtonInteractability();
        
        switch (state) {
            case State.NoCubeSpawned:
                glass.localPosition = new Vector3(glass.localPosition.x, startHeight, glass.localPosition.z);
                break;
            case State.CubeSpawnedButNotTaken:
                DimensionObject parentDimensionObj = cubeSpawned.GetComponentInParent<DimensionObject>();
                // Disable new cube's colliders for the first half of the fall
                if (timeSinceStateChanged <= timeToFallHalfway) {
                    foreach (var newCubeColliders in parentDimensionObj.colliders) {
                        newCubeColliders.enabled = false;
                    }
                }
                else {
                    // Restore collision for the cube halfway through its fall
                    foreach (var newCubeColliders in parentDimensionObj.colliders) {
                        newCubeColliders.enabled = true;
                    }
                }

                if (timeSinceStateChanged > glassLowerDelay && timeSinceStateChanged < glassMoveTime + glassLowerDelay) {
                    float time = timeSinceStateChanged - glassLowerDelay;
                    float t = time / glassMoveTime;
                    
                    Vector3 startPos = new Vector3(glass.localPosition.x, startHeight, glass.localPosition.z);
                    Vector3 endPos = new Vector3(glass.localPosition.x, startHeight-glassOffset, glass.localPosition.z);
                    glass.localPosition = Vector3.Lerp(startPos, endPos, t*t);
                }
                else if (timeSinceStateChanged >= glassMoveTime + glassLowerDelay) {
                    glass.localPosition = new Vector3(glass.localPosition.x, startHeight-glassOffset, glass.localPosition.z);
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
        
        UpdateDematerializeCube(cubeDespawning);
    }

    void SpawnNewCube() {
        if (state != State.NoCubeSpawned) return;

        DynamicObject newCubeDynamicObj = Instantiate(cubePrefab, transform);
        newCubeDynamicObj.isGlobal = false; // Not global until retrieved from cube spawner
        PickupObject newCube = newCubeDynamicObj.GetComponent<PickupObject>();
        newCube.transform.SetParent(null);
        newCube.transform.localScale = cubePrefab.transform.localScale;
        newCube.transform.position = transform.position + transform.up * spawnOffset;
        newCube.transform.Rotate(Random.insideUnitSphere.normalized, Random.Range(0, 20f));
        newCube.thisGravity.gravityDirection = -transform.up;
        SceneManager.MoveGameObjectToScene(newCube.gameObject, gameObject.scene);

        AffixSpawnedCubeWithParentDimensionObject(newCube);
        
        PillarDimensionObject[] dimensionObjs =
            newCube.transform.GetComponentsInChildrenRecursively<PillarDimensionObject>();
        foreach (PillarDimensionObject newCubeDimensionObj in dimensionObjs) {
            newCubeDimensionObj.channel = backWall.channel;
        }

        cubeSpawned = newCube;
        AudioManager.instance.PlayAtLocation(AudioName.CubeSpawnerSpawn, ID, button.transform.position);
        state = State.CubeSpawnedButNotTaken;
    }

    void AffixSpawnedCubeWithParentDimensionObject(PickupObject newCube) {
        // Create a parent object to place the DimensionObject script on that the cube will be a child of
        GameObject cubeParent = new GameObject("CubeDimensionObjectParent");
        DimensionObject parentDimensionObj = cubeParent.AddComponent<DimensionObject>();
        //parentDimensionObj.colliders = new Collider[] {cubeParent.AddComponent<BoxCollider>()};
        //parentDimensionObj.DEBUG = DEBUG;
        parentDimensionObj.startingVisibilityState = VisibilityState.partiallyVisible;
        parentDimensionObj.treatChildrenAsOneObjectRecursively = true;
        parentDimensionObj.ignoreChildrenWithDimensionObject = false;
        parentDimensionObj.channel = backWall.channel;
        // Don't save the specifics of the DimensionObject to the scene, we'll recreate it upon loading
        parentDimensionObj.SkipSave = true;
        cubeParent.name = "CubeDimensionObjectParent";
        cubeParent.transform.position = newCube.transform.position;
        cubeParent.transform.rotation = newCube.transform.rotation;
        SceneManager.MoveGameObjectToScene(cubeParent, gameObject.scene);
        
        newCube.transform.SetParent(cubeParent.transform);
        parentDimensionObj.InitializeRenderersAndLayers();
        parentDimensionObj.SwitchVisibilityState(VisibilityState.partiallyVisible, true);
        parentDimensionObj.SetCollision(VisibilityState.visible, VisibilityState.partiallyVisible, true);
        
        SuperspectivePhysics.IgnoreCollision(roofCollider, newCube.GetComponent<Collider>());
    }
    
    void DestroyCubeAlreadyGrabbedFromSpawner() {
        if (state == State.CubeTaken && ReferenceIsReplaceable(cubeGrabbedFromSpawner)) {
            cubeGrabbedFromSpawner.Reference.MatchAction(
                // If the object is loaded, play the shrink animation for this cube
                pickupObject => {
                    AudioManager.instance.PlayAtLocation(AudioName.CubeSpawnerDespawn, ID, button.transform.position);
                    AudioManager.instance.PlayAtLocation(AudioName.RainstickFast, ID, pickupObject.transform.position);
                    cubeDespawning = pickupObject;

                    // Setup initial dissolve properties
                    foreach (var cubeRenderer in cubeDespawning.GetComponentsInChildren<Renderer>()) {
                        cubeRenderer.material.EnableKeyword("DISSOLVE_OBJECT");
                        cubeRenderer.material.SetFloat("_DissolveBurnSize", .1f);
                        if (cubeRenderer.gameObject == cubeDespawning.gameObject) {
                            cubeRenderer.material.SetTextureScale("_DissolveTex", Vector2.one * 0.02f);
                        }
                    }

                    despawnStartSize = cubeDespawning.transform.localScale;
                    timeSinceCubeDespawnStart = 0f;
                },
                // If the object is not loaded, just destroy it directly
                saveObject => saveObject.Destroy()
            );

            cubeGrabbedFromSpawner = null;
            state = State.NoCubeSpawned;
        }
    }

    void UpdateDematerializeCube(PickupObject cube) {
        if (cube != null) {
            // Don't allow shrinking cubes to be picked up
            cube.interactable = false;
            // Trick to get the cube to not interact with the player anymore but still collide with ground
            cube.gameObject.layer = LayerMask.NameToLayer("VisibleButNoPlayerCollision");
            cube.thisRigidbody.isKinematic = true;
            foreach (Collider collider in cube.GetComponentsInChildren<Collider>()) {
                if (collider.isTrigger) collider.enabled = false;
                else collider.isTrigger = true;
            }

            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            if (timeSinceCubeDespawnStart < despawnTime) {
                timeSinceCubeDespawnStart += Time.deltaTime;
                float t = Mathf.Clamp01(timeSinceCubeDespawnStart / despawnTime);

                cube.transform.localScale = Vector3.LerpUnclamped(
                    despawnStartSize,
                    despawnStartSize*despawnEndSizeMultiplier,
                    cubeDespawnSizeCurve.Evaluate(t)
                );
                propBlock.SetFloat("_DissolveValue", t);
                foreach (var cubeRenderer in cube.GetComponentsInChildren<Renderer>()) {
                    cubeRenderer.SetPropertyBlock(propBlock);
                }
            }
            else {
                propBlock.SetFloat("_DissolveValue", 1);
                foreach (var cubeRenderer in cube.GetComponentsInChildren<Renderer>()) {
                    cubeRenderer.SetPropertyBlock(propBlock);
                }
                cube.GetComponent<DynamicObject>().Destroy();
                cubeDespawning = null;
            }
        }
    }

    void OnTriggerExit(Collider other) {
        PickupObject cube = other.GetComponent<PickupObject>();
        if (cube != null && cube == cubeSpawned) {
            state = State.CubeTaken;
            AudioManager.instance.PlayAtLocation(AudioName.CubeSpawnerClose, ID, glass.transform.position);
            
            // Restore collision with the roof of the Cube Spawner when the cube is taken from the spawner
            SuperspectivePhysics.RestoreCollision(roofCollider, cube.GetComponent<Collider>());
            cube.GetComponent<DynamicObject>().isGlobal = true; // Restore isGlobal behavior when retrieved from spawner

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
            DimensionObject dimensionParent = cube.transform.parent?.GetComponent<DimensionObject>();
            dimensionParent.SwitchVisibilityState(VisibilityState.visible, true);
            dimensionParent.Unregister();
            Transform parent = cube.transform.parent;
            cube.transform.SetParent(null);
            Destroy(parent.gameObject);

            cubeSpawned = null;
            cubeGrabbedFromSpawner = cube;
        }
    }
    
    #region Saving

    [Serializable]
    public class CubeSpawnerNewSave : SerializableSaveObject<CubeSpawnerNew> {
        PickupObjectReference cubeSpawned;
        PickupObjectReference cubeGrabbedFromSpawner;
        float timeSinceStateChanged = 0f;
        State state;

        PickupObjectReference cubeDespawning;
        float timeSinceCubeDespawnStart;
        SerializableVector3 shrinkStartSize;
        SerializableAnimationCurve cubeDespawnSizeCurve;

        public CubeSpawnerNewSave(CubeSpawnerNew spawner) : base(spawner) {
            this.cubeSpawned = spawner.cubeSpawned;
            this.cubeGrabbedFromSpawner = spawner.cubeGrabbedFromSpawner;
            this.timeSinceStateChanged = spawner.timeSinceStateChanged;
            this.state = spawner.state;
            this.cubeDespawning = spawner.cubeDespawning;
            this.timeSinceCubeDespawnStart = spawner.timeSinceCubeDespawnStart;
            this.cubeDespawnSizeCurve = spawner.cubeDespawnSizeCurve;
        }

        public override void LoadSave(CubeSpawnerNew spawner) {
            spawner.cubeSpawned = this.cubeSpawned?.GetOrNull();
            if (spawner.cubeSpawned != null) {
                spawner.AffixSpawnedCubeWithParentDimensionObject(spawner.cubeSpawned);
            }
            spawner.cubeGrabbedFromSpawner = this.cubeGrabbedFromSpawner;
            spawner.timeSinceStateChanged = this.timeSinceStateChanged;
            spawner._state = this.state;
            spawner.cubeDespawning = this.cubeDespawning?.GetOrNull();
            spawner.timeSinceCubeDespawnStart = this.timeSinceCubeDespawnStart;
            spawner.cubeDespawnSizeCurve = this.cubeDespawnSizeCurve;
        }
    }
    #endregion
}
