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
	// TODO: Find a replacement for this dimension cube logic below
	// Spawns a "fake" cube prefab that looks like a normal cube but disappears when in other dimensions
	// The fake cube is replaced by a real one when it is moved out of the spawner
	public PickupObject cubePrefab;
	public AnimationCurve cubeGrowCurve;
	public AnimationCurve cubeShrinkCurve;
	[ReadOnly]
	public Collider waterCollider;
	Collider thisCollider;
	[ReadOnly]
	public PickupObject objectBeingSuspended;
	PickupObject objectGrabbedFromSpawner = null;
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

		if (objectGrabbedFromSpawner != null && objectBeingSuspended != null) {
			objectBeingSuspended.gameObject.SetActive(objectGrabbedFromSpawner.isReplaceable);
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
		SceneManager.MoveGameObjectToScene(newCube.gameObject, gameObject.scene);
		newCube.GetComponent<GravityObject>().useGravity = false;

		//newCube.Drop();
		Physics.IgnoreCollision(waterCollider, newCube.GetComponent<Collider>(), true);
		objectBeingSuspended = newCube;
		rigidbodyOfObjectBeingSuspended = newCube.thisRigidbody;
		rigidbodyOfObjectBeingSuspended.useGravity = false;

		objectBeingSuspended.OnPickupSimple += DestroyCubeAlreadyGrabbedFromSpawner;

		FakeCubeForSpawner fakeCube = newCube.GetComponent<FakeCubeForSpawner>();
		if (fakeCube != null) {
			fakeCube.thisSpawner = this;
		}

		StartCoroutine(GrowCube(newCube));
	}

	public void DestroyCubeAlreadyGrabbedFromSpawner() {
		if (objectGrabbedFromSpawner != null) {
			StartCoroutine(ShrinkAndDestroyCube(objectGrabbedFromSpawner));
			objectGrabbedFromSpawner = null;
		}
	}

	IEnumerator GrowCube(PickupObject cube) {
		// Don't allow growing cubes to be picked up (don't want to break some logic and end up with a mis-sized cube)
		cube.interactable = false;
		const float growTime = 0.75f;
		float timeElapsed = 0;

		Vector3 endSize = cube.transform.localScale;
		cube.transform.localScale = Vector3.zero;

		while (timeElapsed < growTime) {
			timeElapsed += Time.deltaTime;
			float t = Mathf.Clamp01(timeElapsed / growTime);

			cube.transform.localScale = endSize * cubeGrowCurve.Evaluate(t);

			yield return null;
		}

		cube.transform.localScale = endSize;
		cube.interactable = true;
	}

	IEnumerator ShrinkAndDestroyCube(PickupObject cube) {
		// Don't allow shrinking cubes to be picked up
		cube.interactable = false;
		// Trick to get the cube to not interact with the player anymore but still collide with ground
		cube.gameObject.layer = LayerMask.NameToLayer("VisibleButNoPlayerCollision");
		cube.thisRigidbody.useGravity = false;
		const float shrinkTime = 0.4f;
		float timeElapsed = 0;

		Vector3 startSize = cube.transform.localScale;
		Vector3 endSize = Vector3.zero;

		while (timeElapsed < shrinkTime) {
			timeElapsed += Time.deltaTime;
			float t = Mathf.Clamp01(timeElapsed / shrinkTime);

			cube.transform.localScale = Vector3.LerpUnclamped(startSize, endSize, cubeShrinkCurve.Evaluate(t));

			yield return null;
		}

		cube.transform.localScale = endSize;
		Destroy(cube.gameObject);
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
