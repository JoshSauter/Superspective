using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using Saving;
using System;
using SerializableClasses;

[RequireComponent(typeof(UniqueId))]
public class FakeCubeForSpawner : MonoBehaviour, SaveableObject {
	UniqueId _id;
	UniqueId id {
		get {
			if (_id == null) {
				_id = GetComponent<UniqueId>();
			}
			return _id;
		}
	}

	public PickupObject realCubePrefab;
	PickupObject thisCube;
	public CubeSpawner thisSpawner;

	private void Awake() {
		thisCube = GetComponent<PickupObject>();
	}

	void Start() {
		CreateAndCopyDimensionObjectFromSpawner();
	}

	void CreateAndCopyDimensionObjectFromSpawner() {
		if (thisSpawner == null) {
			Debug.LogError($"No cube spawner set for {gameObject.name}", gameObject);
			return;
		}
		PillarDimensionObject spawnerDimensionObj = Utils.FindDimensionObjectRecursively(thisSpawner.transform);
		if (spawnerDimensionObj != null) {
			PillarDimensionObject thisDimensionObj = gameObject.AddComponent<PillarDimensionObject>();
			thisDimensionObj.baseDimension = spawnerDimensionObj.baseDimension;
			thisDimensionObj.findPillarsTechnique = spawnerDimensionObj.findPillarsTechnique;
			thisDimensionObj.whitelist = spawnerDimensionObj.whitelist;
			thisDimensionObj.blacklist = spawnerDimensionObj.blacklist;
			thisDimensionObj.pillarSearchBoxSize = spawnerDimensionObj.pillarSearchBoxSize;
			thisDimensionObj.pillarSearchRadius = spawnerDimensionObj.pillarSearchRadius;
			thisDimensionObj.reverseVisibilityStates = spawnerDimensionObj.reverseVisibilityStates;
			thisDimensionObj.ignoreMaterialChanges = spawnerDimensionObj.ignoreMaterialChanges;
			thisDimensionObj.continuouslyUpdateOnOffAngles = spawnerDimensionObj.continuouslyUpdateOnOffAngles;
			thisDimensionObj.startingVisibilityState = spawnerDimensionObj.visibilityState;
			thisDimensionObj.treatChildrenAsOneObjectRecursively = true;
		}
	}

	public PickupObject SpawnRealCube(ColorCoded colorCoded) {
		GetComponent<Collider>().enabled = false;
		PickupObject newCube = Instantiate(realCubePrefab, transform.position, transform.rotation);
		newCube.transform.SetParent(thisSpawner.transform, true);
		newCube.isHeld = thisCube.isHeld;
		thisCube.isHeld = false;

		Physics.IgnoreCollision(thisSpawner.waterCollider, newCube.GetComponent<Collider>(), true);

		thisSpawner.objectBeingSuspended = newCube;
		thisSpawner.rigidbodyOfObjectBeingSuspended = newCube.thisRigidbody;
		newCube.thisRigidbody.useGravity = false;

		if (colorCoded != null) {
			newCube.gameObject.PasteComponent(colorCoded);
		}

		DimensionObjectBase cubeDimensionObj = newCube.GetComponent<DimensionObjectBase>();
		if (cubeDimensionObj != null) {
			cubeDimensionObj.baseDimension = thisSpawner.baseDimensionForCubes;
			foreach (var dimensionObj in Utils.GetComponentsInChildrenRecursively<DimensionObjectBase>(newCube.transform)) {
				dimensionObj.baseDimension = thisSpawner.baseDimensionForCubes;
			}
		}

		thisSpawner.DestroyCubeAlreadyGrabbedFromSpawner();
		Destroy(gameObject);
		return newCube;
	}

	#region Saving
	public bool SkipSave { get; set; }
	// All components on PickupCubes share the same uniqueId so we need to qualify with component name
	public string ID => $"FakeCubeForSpawner_{id.uniqueId}";

	[Serializable]
	class FakeCubeForSpawnerSave {
		SerializableReference<CubeSpawner> thisSpawner;

		public FakeCubeForSpawnerSave(FakeCubeForSpawner obj) {
			this.thisSpawner = obj.thisSpawner;
		}

		public void LoadSave(FakeCubeForSpawner obj) {
			obj.thisSpawner = this.thisSpawner;
		}
	}

	public object GetSaveObject() {
		return new FakeCubeForSpawnerSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		FakeCubeForSpawnerSave save = savedObject as FakeCubeForSpawnerSave;

		save.LoadSave(this);
	}
	#endregion
}
