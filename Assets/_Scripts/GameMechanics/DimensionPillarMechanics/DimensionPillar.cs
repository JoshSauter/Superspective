using UnityEngine;
using SuperspectiveUtils;
using Saving;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
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
	public int maxBaseDimension;
	public int curBaseDimension;

	public float curDimension;

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
		playerQuadrant = GetQuadrant(Cam.Player.CamPos());
		curDimension = GetDimension(curBaseDimension, Cam.Player.CamPos());
		
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

	/// <summary>
	/// Returns the exact dimension value at a given position, which is a float between 0 and 1 + the base dimension value
	/// </summary>
	/// <param name="baseDimension">Base dimension to add to the [0,1) dimension value</param>
	/// <param name="position">Position to get the dimension value for</param>
	/// <returns>Dimension value at the given position</returns>
	public float GetDimension(int baseDimension, Vector3 position) {
		Vector3 projectedPillarCenter = Vector3.ProjectOnPlane(transform.position, Axis);
		Vector3 projectedVerticalPillarOffset = transform.position - projectedPillarCenter;
		
		Vector3 projectedPos = Vector3.ProjectOnPlane(position, Axis) + projectedVerticalPillarOffset;
		float signedAngle = Vector3.SignedAngle(DimensionShiftVector, projectedPos - transform.position, Axis);
		// Evil divide by zero nonsense check to differentiate the situation where signedAngle is -0.0f instead of 0.0f, because they need to be handled in different ways
		// For -0.0f, the equality to 0.0f will be true, but the division will be 1.0f / -0.0f = float.NegativeInfinity
		float angle360 = signedAngle < 0 || (signedAngle == 0.0f && 1.0f / signedAngle == float.NegativeInfinity) ? signedAngle + 360 : signedAngle;

		return baseDimension + (angle360 / 360f);
	}

	public float WrappedValue(float value) {
		if (value < 0) {
			return maxBaseDimension + 1 + value;
		}
		else if (value >= (maxBaseDimension + 1)) {
			return value - (maxBaseDimension + 1);
		}
		else {
			return value;
		}
	}

	public void ShiftDimensionUp() {
		curBaseDimension = NextDimension(curBaseDimension);

		debug.Log($"Shift to dimension {curBaseDimension}");
	}

	public void ShiftDimensionDown() {
		curBaseDimension = PrevDimension(curBaseDimension);

		debug.Log($"Shift to dimension {curBaseDimension}");
	}

	// Wrap-around logic for incrementing dimension values
	public int NextDimension(int fromDimension) {
		return fromDimension < maxBaseDimension ? fromDimension + 1 : 0;
	}

	// Wrap-around logic for decrementing dimension values
	public int PrevDimension(int fromDimension) {
		return fromDimension > 0 ? fromDimension - 1 : maxBaseDimension;
	}

	void InitializeDimensionWall() {
		GameObject dimensionWallGO = Instantiate(dimensionWallPrefab, transform);
		dimensionWallGO.name = "Dimension Wall";
		dimensionWall = dimensionWallGO.GetComponent<DimensionWall>();
	}

	private void OnDrawGizmosSelected() {
		if (DEBUG) {
			// Draw the dimension shift vector
			float width = 30;
			float height = dimensionWall ? dimensionWall.PillarHeight : 10;
			ExtDebug.DrawPlane(transform.position + transform.forward * width * .5f, -transform.right, height, width, Color.green);
		}
	}

#region Saving
	[Serializable]
	public class DimensionPillarSave : SerializableSaveObject<DimensionPillar> {
		bool initialized;
		int maxDimension;
		int curDimension;

		public DimensionPillarSave(DimensionPillar dimensionPillar) : base(dimensionPillar) {
			this.initialized = dimensionPillar.initialized;
			this.maxDimension = dimensionPillar.maxBaseDimension;
			this.curDimension = dimensionPillar.curBaseDimension;
		}

		public override void LoadSave(DimensionPillar dimensionPillar) {
			dimensionPillar.initialized = this.initialized;
			dimensionPillar.maxBaseDimension = this.maxDimension;
			dimensionPillar.curBaseDimension = this.curDimension;
		}
	}
	#endregion
}
