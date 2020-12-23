using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using NaughtyAttributes;
using Saving;
using System;

[RequireComponent(typeof(UniqueId))]
// NOTE: Assumes that transform.position is centered at the bottom center of the pillar
public class DimensionPillar : MonoBehaviour, SaveableObject {
	UniqueId _id;
	public UniqueId id {
		get {
			if (_id == null) {
				_id = GetComponent<UniqueId>();
			}
			return _id;
		}
	}

	public static Dictionary<string, DimensionPillar> pillars = new Dictionary<string, DimensionPillar>();

	private static DimensionPillar _activePillar;
	public static DimensionPillar activePillar {
		get { return _activePillar; }
		set {
			DimensionPillar prevActive = _activePillar;
			_activePillar = value;
			if (value != null) {
				value.Initialize();
			}
			OnActivePillarChanged?.Invoke(prevActive);
		}
	}

	public Vector3 dimensionShiftVector => transform.forward;
	public Vector3 axis => transform.up;
	public Plane dimensionShiftParallelPlane {
		get {
			Vector3 dimensionShiftPlaneNormalVector = Vector3.Cross(dimensionShiftVector.normalized, axis);
			return new Plane(dimensionShiftPlaneNormalVector, transform.position);
		}
	}
	public Plane dimensionShiftPerpindicularPlane {
		get {
			return new Plane(dimensionShiftVector.normalized, transform.position);
		}
	}
	[SerializeField]
	private Angle.Quadrant _playerQuadrant;
	public Angle.Quadrant playerQuadrant {
		get { return _playerQuadrant; }
		set {
			if (playerQuadrant == Angle.Quadrant.I && value == Angle.Quadrant.IV) {
				ShiftDimensionUp();
			}
			else if (playerQuadrant == Angle.Quadrant.IV && value == Angle.Quadrant.I) {
				ShiftDimensionDown();
			}

			_playerQuadrant = value;
		}
	}

	public bool DEBUG = false;
	DebugLogger debug;
	public bool setAsActiveOnStart = false;
	bool initialized = false;

	public string pillarKey;
	[SerializeField]
	GameObject dimensionWallPrefab;
	public DimensionWall dimensionWall;

	[Range(1, 123)]
	public int maxDimension;
	public int curDimension;

	// TO REMOVE:
	public bool overrideDimensionShiftAngle = false;
	public Angle dimensionShiftAngle;
	public Angle cameraAngleRelativeToPillar;

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

	public delegate void PlayerMoveAroundPillarEvent(int dimension, Angle angle);
	public event PlayerMoveAroundPillarEvent OnPlayerMoveAroundPillar;
#endregion

	void Awake() {
		debug = new DebugLogger(this, () => DEBUG);
		InitializeDimensionWall();
	}

	void Start() {
		Initialize();
		InitializeDictEntry();

		if (setAsActiveOnStart) {
			activePillar = this;
		}
	}
	void InitializeDictEntry() {
		if (pillarKey == "") {
			pillarKey = gameObject.scene.name + " " + gameObject.name;
		}
		pillars[pillarKey] = this;
	}

	void FixedUpdate() {
		if (activePillar != this) return;

		playerQuadrant = GetQuadrant(EpitaphScreen.instance.playerCamera.transform.position);
		//UpdateRelativeCameraAngle();
    }

	private void Initialize() {
		if (initialized) return;

		if (!overrideDimensionShiftAngle) {
			dimensionShiftAngle = DimensionShiftAngle() + Angle.Degrees(0.01f);
		}
		cameraAngleRelativeToPillar = PillarAngleOfPlayerCamera() + curDimension * Angle.D360;
		initialized = true;
	}

	Angle.Quadrant GetQuadrant(Vector3 position) {
		bool parallelPlaneTest = dimensionShiftParallelPlane.GetSide(position);
		bool perpindicularPlaneTest = dimensionShiftPerpindicularPlane.GetSide(position);

		//debug.Log($"ParallelTest: {parallelPlaneTest}\nPerpindicularTest: {perpindicularPlaneTest}");

		if (parallelPlaneTest && perpindicularPlaneTest) {
			return Angle.Quadrant.I;
		}
		else if (parallelPlaneTest && !perpindicularPlaneTest) {
			return Angle.Quadrant.II;
		}
		else if (!parallelPlaneTest && !perpindicularPlaneTest) {
			return Angle.Quadrant.III;
		}
		else /*if (!parallelPlaneTest && perpindicularPlaneTest)*/ {
			return Angle.Quadrant.IV;
		}
	}

	private void UpdateRelativeCameraAngle(bool forceUpdate = false) {
		Angle newCameraAngleRelativeToPillar = PillarAngleOfPlayerCamera() + curDimension * Angle.D360;
		Angle angleDiff = newCameraAngleRelativeToPillar.WrappedAngleDiff(cameraAngleRelativeToPillar);
		if (angleDiff.degrees == 0 && !forceUpdate) return;

		bool clockwise = angleDiff.radians >= 0;

		UpdateDimensionShifting(cameraAngleRelativeToPillar, newCameraAngleRelativeToPillar, clockwise);
		newCameraAngleRelativeToPillar = PillarAngleOfPlayerCamera() + curDimension * Angle.D360;

		if (OnPlayerMoveAroundPillar != null) {
			OnPlayerMoveAroundPillar(curDimension, newCameraAngleRelativeToPillar);
			debug.Log("Prev: " + cameraAngleRelativeToPillar + "\nNew: " + newCameraAngleRelativeToPillar);
		}

		cameraAngleRelativeToPillar = newCameraAngleRelativeToPillar;
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

	void UpdateDimensionShifting(Angle prevAngle, Angle newAngle, bool clockwise) {
		bool shiftDimensionUp = clockwise && Angle.IsAngleBetween((curDimension+1) * Angle.D360, prevAngle, newAngle);
		bool shiftDimensionDown = !clockwise && Angle.IsAngleBetween(curDimension * Angle.D360, newAngle, prevAngle);

		if (shiftDimensionUp) {
			ShiftDimensionUp();
		}
		if (shiftDimensionDown) {
			ShiftDimensionDown();
		}
	}

	Angle DimensionShiftAngle() {
		//Vector3 pillarToCamera = EpitaphScreen.instance.playerCamera.transform.position - transform.position;
		//PolarCoordinate polar = PolarCoordinate.CartesianToPolar(pillarToCamera);
		PolarCoordinate polar = PolarCoordinate.CartesianToPolar(dimensionShiftVector);
		return polar.angle;
	}

	/// <summary>
	/// Calculates the Angle of the point relative to this Pillar, where 0 degrees is the dimensionShiftAngle and the value increases clockwise
	/// </summary>
	/// <param name="p">Point in space to find the relative Angle of</param>
	/// <returns></returns>
	public Angle PillarAngleOfPoint(Vector3 p) {
		Vector3 pillarToPoint = p - transform.position;
		PolarCoordinate polar = PolarCoordinate.CartesianToPolar(pillarToPoint);
		return (dimensionShiftAngle - polar.angle).normalized;
	}

	public Angle PillarAngleOfPlayerCamera() {
		return PillarAngleOfPoint(EpitaphScreen.instance.playerCamera.transform.position);
	}

	// Wrap-around logic for incrementing dimension values
	public int NextDimension(int fromDimension) {
		return fromDimension < maxDimension ? fromDimension + 1 : 0;
	}

	// Wrap-around logic for decrementing dimension values
	public int PrevDimension(int fromDimension) {
		return fromDimension > 0 ? fromDimension - 1 : maxDimension;
	}

	private void InitializeDimensionWall() {
		GameObject dimensionWallGO = Instantiate(dimensionWallPrefab, transform);
		dimensionWallGO.name = "Dimension Wall";
		dimensionWall = dimensionWallGO.GetComponent<DimensionWall>();
	}

	[ContextMenu("Print active pillar details")]
	void PrintActivePillarName() {
		if (DimensionPillar.activePillar == null) {
			Debug.Log("Active pillar is null");
		}
		else {
			Debug.Log("Active pillar is: " + activePillar.gameObject.name + " in Scene: " + activePillar.gameObject.scene.name +
				"\nThis pillar is active? " + (activePillar == this), activePillar.gameObject);
		}
	}

	#region Saving
	public bool SkipSave { get; set; }

	public string ID {
		get {
			if (id == null || id.uniqueId == null) {
				throw new Exception($"{gameObject.name} in {gameObject.scene.name} doesn't have a uniqueId set");
			}
			return $"DimensionPillar_{id.uniqueId}";
		}
	}
	//public string ID => $"DimensionPillar_{id.uniqueId}";

	[Serializable]
	class DimensionPillarSave {
		bool setAsActiveOnStart;

		bool initialized;
		int maxDimension;
		int curDimension;

		bool overrideDimensionShiftAngle;
		Angle dimensionShiftAngle;
		Angle cameraAngleRelativeToPillar;

		public DimensionPillarSave(DimensionPillar dimensionPillar) {
			this.setAsActiveOnStart = DimensionPillar.activePillar == dimensionPillar;
			this.initialized = dimensionPillar.initialized;
			this.maxDimension = dimensionPillar.maxDimension;
			this.curDimension = dimensionPillar.curDimension;
			this.overrideDimensionShiftAngle = dimensionPillar.overrideDimensionShiftAngle;
			this.dimensionShiftAngle = dimensionPillar.dimensionShiftAngle;
			this.cameraAngleRelativeToPillar = dimensionPillar.cameraAngleRelativeToPillar;
		}

		public void LoadSave(DimensionPillar dimensionPillar) {
			dimensionPillar.initialized = this.initialized;
			dimensionPillar.maxDimension = this.maxDimension;
			if (dimensionPillar.curDimension != this.curDimension) {
				dimensionPillar.curDimension = this.curDimension;
				dimensionPillar.OnDimensionChange?.Invoke(dimensionPillar.curDimension, this.curDimension);
			}
			dimensionPillar.curDimension = this.curDimension;
			dimensionPillar.overrideDimensionShiftAngle = this.overrideDimensionShiftAngle;
			dimensionPillar.dimensionShiftAngle = this.dimensionShiftAngle;
			dimensionPillar.cameraAngleRelativeToPillar = this.cameraAngleRelativeToPillar - Angle.Degrees(0.001f);

			if (setAsActiveOnStart) {
				DimensionPillar.activePillar = dimensionPillar;
				dimensionPillar.UpdateRelativeCameraAngle(true);
			}
		}
	}

	public object GetSaveObject() {
		return new DimensionPillarSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		DimensionPillarSave save = savedObject as DimensionPillarSave;

		save.LoadSave(this);
	}
	#endregion
}
