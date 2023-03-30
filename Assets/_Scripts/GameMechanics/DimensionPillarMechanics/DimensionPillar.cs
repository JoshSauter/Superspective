using UnityEngine;
using SuperspectiveUtils;
using Saving;
using System;
using System.Collections.Generic;
using PillarReference = SerializableClasses.SerializableReference<DimensionPillar, DimensionPillar.DimensionPillarSave>;

// TODO: ActivePillar logic needs some work
[RequireComponent(typeof(UniqueId))]
// NOTE: Assumes that transform.position is centered at the bottom center of the pillar
public class DimensionPillar : SaveableObject<DimensionPillar, DimensionPillar.DimensionPillarSave> {
	public static Dictionary<string, PillarReference> allPillars = new Dictionary<string, PillarReference>();

	public Vector3 DimensionShiftVector => transform.forward;
	public Vector3 Axis => transform.up;
	Plane DimensionShiftParallelPlane {
		get {
			Vector3 dimensionShiftPlaneNormalVector = Vector3.Cross(DimensionShiftVector.normalized, Axis);
			return new Plane(dimensionShiftPlaneNormalVector, transform.position);
		}
	}
	Plane DimensionShiftPerpendicularPlane => new Plane(DimensionShiftVector.normalized, transform.position);

	[SerializeField]
	public Angle.Quadrant playerQuadrant;

	bool initialized = false;
	
	[SerializeField]
	GameObject dimensionWallPrefab;
	public DimensionWall dimensionWall;

	[Range(1, 123)]
	public int maxDimension;
	public int curDimension;

	public int heightOverride = -1;
	public bool HeightOverridden => heightOverride != -1;

	protected override void Awake() {
		base.Awake();
		InitializeDimensionWall();
		if (!allPillars.ContainsKey(ID)) {
			allPillars.Add(ID, this);
		}
	}

	void Update() {
		Camera playerCam = SuperspectiveScreen.instance.playerCamera;
		Angle.Quadrant prevQuadrant = playerQuadrant;
		playerQuadrant = GetQuadrant(playerCam.transform.position);
		
		if (prevQuadrant == Angle.Quadrant.I && playerQuadrant == Angle.Quadrant.IV) {
			ShiftDimensionUp();
		}
		else if (prevQuadrant == Angle.Quadrant.IV && playerQuadrant == Angle.Quadrant.I) {
			ShiftDimensionDown();
		}
		else if (prevQuadrant != playerQuadrant) {
			debug.Log($"Prev Quadrant: {prevQuadrant}\nNew Quadrant: {playerQuadrant}");
		}
    }

	Angle.Quadrant GetQuadrant(Vector3 position) {
		bool parallelPlaneTest = DimensionShiftParallelPlane.GetSide(position);
		bool perpendicularPlaneTest = DimensionShiftPerpendicularPlane.GetSide(position);

		//debug.Log($"ParallelTest: {parallelPlaneTest}\nPerpindicularTest: {perpindicularPlaneTest}");

		if (parallelPlaneTest && perpendicularPlaneTest) {
			return Angle.Quadrant.I;
		}
		else if (parallelPlaneTest && !perpendicularPlaneTest) {
			return Angle.Quadrant.II;
		}
		else if (!parallelPlaneTest && !perpendicularPlaneTest) {
			return Angle.Quadrant.III;
		}
		else /*if (!parallelPlaneTest && perpendicularPlaneTest)*/ {
			return Angle.Quadrant.IV;
		}
	}

	public void ShiftDimensionUp() {
		curDimension = NextDimension(curDimension);

		debug.Log($"Shift to dimension {curDimension}");
	}

	public void ShiftDimensionDown() {
		curDimension = PrevDimension(curDimension);

		debug.Log($"Shift to dimension {curDimension}");
	}

	// Wrap-around logic for incrementing dimension values
	public int NextDimension(int fromDimension) {
		return fromDimension < maxDimension ? fromDimension + 1 : 0;
	}

	// Wrap-around logic for decrementing dimension values
	public int PrevDimension(int fromDimension) {
		return fromDimension > 0 ? fromDimension - 1 : maxDimension;
	}

	void InitializeDimensionWall() {
		GameObject dimensionWallGO = Instantiate(dimensionWallPrefab, transform);
		dimensionWallGO.name = "Dimension Wall";
		dimensionWall = dimensionWallGO.GetComponent<DimensionWall>();
	}

	#region Saving
	[Serializable]
	public class DimensionPillarSave : SerializableSaveObject<DimensionPillar> {
		bool initialized;
		int maxDimension;
		int curDimension;

		public DimensionPillarSave(DimensionPillar dimensionPillar) : base(dimensionPillar) {
			this.initialized = dimensionPillar.initialized;
			this.maxDimension = dimensionPillar.maxDimension;
			this.curDimension = dimensionPillar.curDimension;
		}

		public override void LoadSave(DimensionPillar dimensionPillar) {
			dimensionPillar.initialized = this.initialized;
			dimensionPillar.maxDimension = this.maxDimension;
			dimensionPillar.curDimension = this.curDimension;
		}
	}
	#endregion
}
