using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class DimensionPillar : MonoBehaviour {
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
			if (OnActivePillarChanged != null) {
				OnActivePillarChanged(prevActive);
			}
		}
	}

	public bool DEBUG = false;
	DebugLogger debug;

	public string pillarKey;
	[SerializeField]
	GameObject dimensionWallPrefab;
	[SerializeField]
	GameObject debugWallPrefab;

	[Range(1, 123)]
	public int maxDimension;
	public int curDimension;

	Camera playerCamera;

	public bool overrideDimensionShiftAngle = false;
	public Angle dimensionShiftAngle;
	public Angle cameraAngleRelativeToPillar;

#region events
	public delegate void DimensionChangeEvent(int prevDimension, int curDimension);
	public event DimensionChangeEvent OnDimensionChange;

	public delegate void ActivePillarChangedEvent(DimensionPillar previousActivePillar);
	public static event ActivePillarChangedEvent OnActivePillarChanged;

	public delegate void PlayerMoveAroundPillarEvent(int dimension, Angle angle);
	public event PlayerMoveAroundPillarEvent OnPlayerMoveAroundPillar;
#endregion

	void Start() {
		Initialize();
		InitializeDimensionWall();
		InitializeDictEntry();
	}
	void InitializeDictEntry() {
		if (pillarKey == "") {
			pillarKey = gameObject.scene.name + " " + gameObject.name;
		}
		if (!pillars.ContainsKey(pillarKey)) {
			pillars[pillarKey] = this;
		}
	}

	void FixedUpdate() {
		if (activePillar != this) return;

		UpdateRelativeCameraAngle();
    }

	private void Initialize() {
		debug = new DebugLogger(gameObject, DEBUG);

		playerCamera = EpitaphScreen.instance.playerCamera;
		if (!overrideDimensionShiftAngle) {
			dimensionShiftAngle = DimensionShiftAngle() + Angle.Degrees(0.01f);
		}
		cameraAngleRelativeToPillar = PillarAngleOfPlayerCamera() + curDimension * Angle.D360;
	}

	private void UpdateRelativeCameraAngle() {
		Angle newCameraAngleRelativeToPillar = PillarAngleOfPlayerCamera() + curDimension * Angle.D360;
		Angle angleDiff = newCameraAngleRelativeToPillar.WrappedAngleDiff(cameraAngleRelativeToPillar);
		if (angleDiff.degrees == 0) return;

		bool clockwise = angleDiff.radians >= 0;

		UpdateDimensionShifting(cameraAngleRelativeToPillar, newCameraAngleRelativeToPillar, clockwise);
		newCameraAngleRelativeToPillar = PillarAngleOfPlayerCamera() + curDimension * Angle.D360;

		if (OnPlayerMoveAroundPillar != null) {
			OnPlayerMoveAroundPillar(curDimension, newCameraAngleRelativeToPillar);
			debug.Log("Prev: " + cameraAngleRelativeToPillar + "\nNew: " + newCameraAngleRelativeToPillar);
		}


		cameraAngleRelativeToPillar = newCameraAngleRelativeToPillar;
	}

	void UpdateDimensionShifting(Angle prevAngle, Angle newAngle, bool clockwise) {
		bool shiftDimensionUp = clockwise && Angle.IsAngleBetween((curDimension+1) * Angle.D360, prevAngle, newAngle);
		bool shiftDimensionDown = !clockwise && Angle.IsAngleBetween(curDimension * Angle.D360, newAngle, prevAngle);
		if (shiftDimensionUp) {
			int prevDimension = curDimension;
			curDimension = (curDimension < maxDimension ? curDimension + 1 : 0);

			if (OnDimensionChange != null) {
				OnDimensionChange(prevDimension, curDimension);
			}

			print("Shift to dimension " + curDimension);
		}
		if (shiftDimensionDown) {
			int prevDimension = curDimension;
			curDimension = (curDimension > 0 ? curDimension - 1 : maxDimension);

			if (OnDimensionChange != null) {
				OnDimensionChange(prevDimension, curDimension);
			}

			print("Shift to dimension " + curDimension);
		}
	}

	Angle DimensionShiftAngle() {
		Vector3 pillarToCamera = playerCamera.transform.position - transform.position;
		PolarCoordinate polar = PolarCoordinate.CartesianToPolar(pillarToCamera);
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
		return PillarAngleOfPoint(playerCamera.transform.position);
	}

	private void InitializeDimensionWall() {
		GameObject dimensionWallGO = Instantiate(dimensionWallPrefab, transform);
		dimensionWallGO.name = "Dimension Wall";

		if (DEBUG) {
			Instantiate(debugWallPrefab, transform);
		}
	}
}
