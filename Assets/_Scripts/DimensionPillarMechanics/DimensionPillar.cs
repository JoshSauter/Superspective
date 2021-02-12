using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using NaughtyAttributes;
using Saving;
using System;
using UnityEngine.Serialization;

[RequireComponent(typeof(UniqueId))]
// NOTE: Assumes that transform.position is centered at the bottom center of the pillar
public class DimensionPillar : SaveableObject<DimensionPillar, DimensionPillar.DimensionPillarSave> {
	UniqueId id;

	UniqueId uniqueId {
		get {
			if (id == null) {
				id = GetComponent<UniqueId>();
			}
			return id;
		}
	}

	public static Dictionary<string, DimensionPillar> pillars = new Dictionary<string, DimensionPillar>();

	static DimensionPillar _activePillar;
	public static DimensionPillar ActivePillar {
		get { return _activePillar; }
		set {
			DimensionPillar prevActive = _activePillar;
			_activePillar = value;
			OnActivePillarChanged?.Invoke(prevActive);
		}
	}

	public Vector3 DimensionShiftVector => transform.forward;
	public Vector3 Axis => transform.up;
	public Plane DimensionShiftParallelPlane {
		get {
			Vector3 dimensionShiftPlaneNormalVector = Vector3.Cross(DimensionShiftVector.normalized, Axis);
			return new Plane(dimensionShiftPlaneNormalVector, transform.position);
		}
	}
	public Plane DimensionShiftPerpendicularPlane {
		get {
			return new Plane(DimensionShiftVector.normalized, transform.position);
		}
	}
	[SerializeField]
	Angle.Quadrant _playerQuadrant;
	public Angle.Quadrant PlayerQuadrant {
		get { return _playerQuadrant; }
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

	public bool setAsActiveOnStart = false;
	bool initialized = false;

	public string pillarKey;
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

	public delegate void ActivePillarChangedEvent(DimensionPillar previousActivePillar);
	public static event ActivePillarChangedEvent OnActivePillarChanged;
#endregion

	protected override void Awake() {
		base.Awake();
		InitializeDimensionWall();
	}

	protected override void Start() {
		base.Start();
		InitializeDictEntry();

		if (setAsActiveOnStart) {
			ActivePillar = this;
		}
	}
	void InitializeDictEntry() {
		if (pillarKey == "") {
			pillarKey = gameObject.scene.name + " " + gameObject.name;
		}
		pillars[pillarKey] = this;
	}

	void FixedUpdate() {
		if (ActivePillar != this) return;

		PlayerQuadrant = GetQuadrant(EpitaphScreen.instance.playerCamera.transform.position);
		//UpdateRelativeCameraAngle();
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

	void ShiftDimensionUp() {
		int prevDimension = curDimension;
		curDimension = NextDimension(curDimension);

		OnDimensionChange?.Invoke(prevDimension, curDimension);
		OnDimensionChangeWithDirection?.Invoke(prevDimension, curDimension, DimensionSwitch.Up);

		debug.Log("Shift to dimension " + curDimension);
	}

	void ShiftDimensionDown() {
		int prevDimension = curDimension;
		curDimension = PrevDimension(curDimension);

		OnDimensionChange?.Invoke(prevDimension, curDimension);
		OnDimensionChangeWithDirection?.Invoke(prevDimension, curDimension, DimensionSwitch.Down);

		debug.Log("Shift to dimension " + curDimension);
	}

	Angle DimensionShiftAngle() {
		//Vector3 pillarToCamera = EpitaphScreen.instance.playerCamera.transform.position - transform.position;
		//PolarCoordinate polar = PolarCoordinate.CartesianToPolar(pillarToCamera);
		PolarCoordinate polar = PolarCoordinate.CartesianToPolar(DimensionShiftVector);
		return polar.angle;
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

	[ContextMenu("Print active pillar details")]
	void PrintActivePillarName() {
		if (DimensionPillar.ActivePillar == null) {
			Debug.Log("Active pillar is null");
		}
		else {
			Debug.Log("Active pillar is: " + ActivePillar.gameObject.name + " in Scene: " + ActivePillar.gameObject.scene.name +
				"\nThis pillar is active? " + (ActivePillar == this), ActivePillar.gameObject);
		}
	}

	#region Saving
	public override string ID {
		get {
			if (uniqueId == null || uniqueId.uniqueId == null) {
				throw new Exception($"{gameObject.name} in {gameObject.scene.name} doesn't have a uniqueId set");
			}
			return $"DimensionPillar_{uniqueId.uniqueId}";
		}
	}
	//public string ID => $"DimensionPillar_{id.uniqueId}";

	[Serializable]
	public class DimensionPillarSave : SerializableSaveObject<DimensionPillar> {
		bool setAsActiveOnStart;

		bool initialized;
		int maxDimension;
		int curDimension;

		bool overrideDimensionShiftAngle;
		Angle dimensionShiftAngle;
		Angle cameraAngleRelativeToPillar;

		public DimensionPillarSave(DimensionPillar dimensionPillar) {
			this.setAsActiveOnStart = DimensionPillar.ActivePillar == dimensionPillar;
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

			if (setAsActiveOnStart) {
				DimensionPillar.ActivePillar = dimensionPillar;
			}
		}
	}
	#endregion
}
