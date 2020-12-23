using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using EpitaphUtils;
using Saving;
using System;
using SerializableClasses;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UniqueId))]
public class CubeSpawner : MonoBehaviour, SaveableObject {
	UniqueId _id;
	UniqueId id {
		get {
			if (_id == null) {
				_id = GetComponent<UniqueId>();
			}
			return _id;
		}
	}

	public enum GrowCubeState {
		Idle,
		Growing,
		Grown
	}
	GrowCubeState _growCubeState;
	public GrowCubeState growCubeState {
		get { return _growCubeState; }
		set {
			if (value != _growCubeState) {
				timeSinceGrowCubeStateChanged = 0f;
				_growCubeState = value;
			}
		}
	}
	float timeSinceGrowCubeStateChanged = 0f;
	const float growTime = 0.75f;
	Vector3 growEndSize;

	public enum ShrinkCubeState {
		Idle,
		Shrinking
	}
	ShrinkCubeState _shrinkCubeState;
	public ShrinkCubeState shrinkCubeState {
		get { return _shrinkCubeState; }
		set {
			if (value != _shrinkCubeState) {
				timeSinceShrinkCubeStateChanged = 0f;
				_shrinkCubeState = value;
			}
		}
	}
	float timeSinceShrinkCubeStateChanged = 0f;
	const float shrinkTime = 0.4f;
	Vector3 shrinkStartSize;

	public PickupObject cubePrefab;
	public AnimationCurve cubeGrowCurve;
	public AnimationCurve cubeShrinkCurve;
	[ReadOnly]
	public Collider waterCollider;
	Collider thisCollider;
	[ReadOnly]
	public PickupObject objectBeingSuspended;
	PickupObject objectGrabbedFromSpawner = null;
	PickupObject objectShrinking;
	[ReadOnly]
	public Rigidbody rigidbodyOfObjectBeingSuspended;

	public int baseDimensionForCubes;

	IEnumerator Start() {
		thisCollider = GetComponent<Collider>();
		yield return new WaitForSeconds(0.5f);
		if (objectBeingSuspended == null) {
			SpawnNewCube();
		}
    }

	void Update() {
		if (objectBeingSuspended == null && (objectGrabbedFromSpawner == null || objectGrabbedFromSpawner.isReplaceable)) {
			SpawnNewCube();
		}

		if (objectGrabbedFromSpawner != null && objectBeingSuspended != null) {
			objectBeingSuspended.gameObject.SetActive(objectGrabbedFromSpawner.isReplaceable);
		}

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
				rigidbodyOfObjectBeingSuspended.AddTorque(10*Vector3.up, ForceMode.Force);
			}
		}
    }

	private void OnTriggerExit(Collider other) {
		PickupObject movableObject = other.gameObject.GetComponent<PickupObject>();
		if (movableObject == objectBeingSuspended) {
			if (movableObject.isHeld) {
				objectGrabbedFromSpawner = movableObject;
				objectGrabbedFromSpawner.OnPickupSimple -= DestroyCubeAlreadyGrabbedFromSpawner;
				SpawnNewCube();

				Physics.IgnoreCollision(waterCollider, movableObject.GetComponent<Collider>(), false);
			}
		}
	}

	void SpawnNewCube() {
		const float randomizeOffset = 1.0f;
		PickupObject newCube = Instantiate(cubePrefab, thisCollider.bounds.center + Random.insideUnitSphere * Random.Range(0, randomizeOffset), new Quaternion());
		objectBeingSuspended = newCube;
		SceneManager.MoveGameObjectToScene(newCube.gameObject, gameObject.scene);
		newCube.GetComponent<GravityObject>().useGravity = false;

		Physics.IgnoreCollision(waterCollider, newCube.GetComponent<Collider>(), true);
		rigidbodyOfObjectBeingSuspended = newCube.thisRigidbody;
		rigidbodyOfObjectBeingSuspended.useGravity = false;

		// TODO: Make this happen when the cube is pulled out rather than when it is clicked on
		objectBeingSuspended.OnPickupSimple += DestroyCubeAlreadyGrabbedFromSpawner;

		growEndSize = newCube.transform.localScale;
		growCubeState = GrowCubeState.Growing;
	}

	public void DestroyCubeAlreadyGrabbedFromSpawner() {
		if (objectGrabbedFromSpawner != null && objectGrabbedFromSpawner.isReplaceable) {
			objectShrinking = objectGrabbedFromSpawner;
			objectGrabbedFromSpawner = null;

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
				if (timeSinceGrowCubeStateChanged == 0) {
					cube.transform.localScale = Vector3.zero;
				}
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
			default:
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

				cube.transform.localScale = Vector3.LerpUnclamped(shrinkStartSize, Vector3.zero, cubeShrinkCurve.Evaluate(t));
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
		SerializableAnimationCurve cubeGrowCurve;
		SerializableAnimationCurve cubeShrinkCurve;
		SerializableReference<PickupObject> objectBeingSuspended;
		SerializableReference<PickupObject> objectGrabbedFromSpawner;

		int baseDimensionForCubes;

		public CubeSpawnerSave(CubeSpawner spawner) {
			this.cubeGrowCurve = spawner.cubeGrowCurve;
			this.cubeShrinkCurve = spawner.cubeShrinkCurve;
			this.objectBeingSuspended = spawner.objectBeingSuspended;
			this.objectGrabbedFromSpawner = spawner.objectGrabbedFromSpawner;
			this.baseDimensionForCubes = spawner.baseDimensionForCubes;
		}

		public void LoadSave(CubeSpawner spawner) {
			spawner.cubeGrowCurve = this.cubeGrowCurve;
			spawner.cubeShrinkCurve = this.cubeShrinkCurve;
			spawner.objectBeingSuspended = this.objectBeingSuspended;
			spawner.objectGrabbedFromSpawner = this.objectGrabbedFromSpawner;
			if (spawner.objectBeingSuspended != null) {
				spawner.rigidbodyOfObjectBeingSuspended = spawner.objectBeingSuspended.GetComponent<Rigidbody>();
			}
			spawner.baseDimensionForCubes = this.baseDimensionForCubes;
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
