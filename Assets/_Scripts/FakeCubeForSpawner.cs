using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class FakeCubeForSpawner : MonoBehaviour {
	public PickupObject realCubePrefab;
	PickupObject thisCube;
	CubeSpawner thisSpawner;

	private void Awake() {
		thisCube = GetComponent<PickupObject>();
	}

	void Start() {
		thisSpawner = GetComponentInParent<CubeSpawner>();

		CreateAndCopyDimensionObjectFromSpawner();
	}

	void CreateAndCopyDimensionObjectFromSpawner() {
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
}
