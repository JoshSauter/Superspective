using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class DimensionPillar : MonoBehaviour {
	public bool DEBUG = false;
	DebugLogger debug;
	public static Dictionary<string, DimensionPillar> pillars;

	private static DimensionPillar _activePillar;
	public static DimensionPillar activePillar {
		get { return _activePillar; }
		set {
			DimensionPillar prevActive = _activePillar;
			_activePillar = value;
			value.Initialize();
			if (OnActivePillarChanged != null) {
				OnActivePillarChanged(prevActive);
			}
		}
	}
	[SerializeField]
	GameObject dimensionWallPrefab;
	[SerializeField]
	GameObject debugWallPrefab;

	public bool staticDimensionShiftAngle = false;
	[Range(0, 123)]
	public int minDimension;
	[Range(0, 123)]
	public int maxDimension;
	public int curDimension;

	Camera playerCamera;

	public Angle dimensionShiftAngle;
	public Angle cameraAngleRelativeToPillar;
	bool aboveMaxDimension = false;
	bool belowMinDimension = false;

#region events
	public delegate void DimensionChangeEvent(int prevDimension, int curDimension);
	public event DimensionChangeEvent OnDimensionChange;

	public delegate void DimensionShiftAngleChangeEvent();
	public event DimensionShiftAngleChangeEvent OnDimensionShiftAngleChange;

	public delegate void ActivePillarChangedEvent(DimensionPillar previousActivePillar);
	public static event ActivePillarChangedEvent OnActivePillarChanged;

	public delegate void PlayerMoveAroundPillarEvent(Angle previousAngle, Angle newAngle, bool clockwise);
	public event PlayerMoveAroundPillarEvent OnPlayerMoveAroundPillar;
#endregion

	void Start() {
		Initialize();
		InitializeDimensionWall();
	}

    void Update() {
		UpdateRelativeCameraAngle();
    }

	private void Initialize() {
		debug = new DebugLogger(gameObject, DEBUG);

		playerCamera = EpitaphScreen.instance.playerCamera;

		dimensionShiftAngle = DimensionShiftAngle() + Angle.Degrees(0.01f);
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
			OnPlayerMoveAroundPillar(cameraAngleRelativeToPillar, newCameraAngleRelativeToPillar, clockwise);
		}

		debug.Log("Prev: " + cameraAngleRelativeToPillar + "\nNew: " + newCameraAngleRelativeToPillar);

		cameraAngleRelativeToPillar = newCameraAngleRelativeToPillar;
	}

	void UpdateDimensionShifting(Angle prevAngle, Angle newAngle, bool clockwise) {
		bool shiftDimensionUp = clockwise && Angle.IsAngleBetween((curDimension+1) * Angle.D360, prevAngle, newAngle);
		bool shiftDimensionDown = !clockwise && Angle.IsAngleBetween(curDimension * Angle.D360, newAngle, prevAngle);
		if (shiftDimensionUp) {
			if (curDimension < maxDimension) {
				curDimension++;
				//debug.Log("PrevAngle: " + prevAngle + "\nNewAngle: " + newAngle + "\nDimensionShiftAngle: " + dimensionShiftAngle);
				if (OnDimensionChange != null) {
					OnDimensionChange(curDimension - 1, curDimension);
				}
				print("Shift to dimension " + curDimension);
			}
			else {
				aboveMaxDimension = true;
			}
		}
		if (shiftDimensionDown) {
			if (curDimension > minDimension) {
				curDimension--;
				//debug.Log("PrevAngle: " + prevAngle + "\nNewAngle: " + newAngle + "\nDimensionShiftAngle: " + dimensionShiftAngle);
				if (OnDimensionChange != null) {
					OnDimensionChange(curDimension + 1, curDimension);
				}
				print("Shift to dimension " + curDimension);
			}
			else {
				belowMinDimension = true;
			}
		}

		// Handle updating dimension shift angle when pushing against the max dimension or min dimension
		bool updateDimensionShiftAngle = false;
		if (!staticDimensionShiftAngle) {
			if (aboveMaxDimension && !clockwise) {
				aboveMaxDimension = false;
				dimensionShiftAngle = newAngle - Angle.Degrees(0.01f);
				updateDimensionShiftAngle = true;
			}
			if (belowMinDimension && clockwise) {
				belowMinDimension = false;
				dimensionShiftAngle = newAngle + Angle.Degrees(0.01f);
				updateDimensionShiftAngle = true;
			}
			if (updateDimensionShiftAngle && OnDimensionShiftAngleChange != null) {
				debug.Log("PrevAngle: " + prevAngle + "\nNewAngle: " + newAngle + "\nNewDimensionShiftAngle: " + dimensionShiftAngle);
				OnDimensionShiftAngleChange();
			}
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
