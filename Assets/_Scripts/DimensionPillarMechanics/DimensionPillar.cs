﻿using UnityEngine;
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
	Angle.Quadrant _playerQuadrant;
	Angle.Quadrant PlayerQuadrant {
		get => _playerQuadrant;
		set {
			if (PlayerQuadrant == Angle.Quadrant.I && value == Angle.Quadrant.IV) {
				ShiftDimensionUp();
			}
			else if (PlayerQuadrant == Angle.Quadrant.IV && value == Angle.Quadrant.I) {
				ShiftDimensionDown();
			}

			_playerQuadrant = value;
		}
	}

	bool initialized = false;
	
	[SerializeField]
	GameObject dimensionWallPrefab;
	public DimensionWall dimensionWall;

	[Range(1, 123)]
	public int maxDimension;
	public int curDimension;

	public int heightOverride = -1;
	public bool HeightOverridden => heightOverride != -1;
	
	public enum DimensionSwitch {
		Up,
		Down
	}

#region events
	public delegate void DimensionChangeEvent(int prevDimension, int curDimension);
	public event DimensionChangeEvent OnDimensionChange;

	public delegate void DimensionChangeWithDirectionEvent(int prevDimension, int curDimension, DimensionSwitch direction);
	public event DimensionChangeWithDirectionEvent OnDimensionChangeWithDirection;
#endregion

	protected override void Awake() {
		base.Awake();
		InitializeDimensionWall();
		if (!allPillars.ContainsKey(ID)) {
			allPillars.Add(ID, this);
		}
	}

	void FixedUpdate() {
		PlayerQuadrant = GetQuadrant(SuperspectiveScreen.instance.playerCamera.transform.position);
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
		int prevDimension = curDimension;
		curDimension = NextDimension(curDimension);

		OnDimensionChange?.Invoke(prevDimension, curDimension);
		OnDimensionChangeWithDirection?.Invoke(prevDimension, curDimension, DimensionSwitch.Up);

		debug.Log("Shift to dimension " + curDimension);
	}

	public void ShiftDimensionDown() {
		int prevDimension = curDimension;
		curDimension = PrevDimension(curDimension);

		OnDimensionChange?.Invoke(prevDimension, curDimension);
		OnDimensionChangeWithDirection?.Invoke(prevDimension, curDimension, DimensionSwitch.Down);

		debug.Log("Shift to dimension " + curDimension);
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
			if (dimensionPillar.curDimension != this.curDimension) {
				dimensionPillar.curDimension = this.curDimension;
				dimensionPillar.OnDimensionChange?.Invoke(dimensionPillar.curDimension, this.curDimension);
			}
			dimensionPillar.curDimension = this.curDimension;
		}
	}
	#endregion
}
