using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using EpitaphUtils.ShaderUtils;
using System.Linq;
using System;
using SerializableClasses;

public class PillarDimensionObject : DimensionObject {
	public bool continuouslyUpdateOnOffAngles = false;

	public int objectStartDimension;
	public int objectEndDimension;

	////////////////////////////////////
	// Techniques for finding pillars //
	////////////////////////////////////
	public enum FindPillarsTechnique {
		whitelist,
		automaticSphere,
		automaticSphereWithBlacklist,
		automaticBox,
		automaticBoxWithBlacklist,
		anyPillar
	}
	public FindPillarsTechnique findPillarsTechnique;
	public List<DimensionPillar> pillarsFound = new List<DimensionPillar>();
	// Whitelist/blacklist pillars
	public List<DimensionPillar> whitelist = new List<DimensionPillar>();
	public List<DimensionPillar> blacklist = new List<DimensionPillar>();
	public float pillarSearchRadius = 40;
	public Vector3 pillarSearchBoxSize = Vector3.one * 80;

	///////////////////
	// On/Off Angles //
	///////////////////
	[Range(0, 7)]
	public int baseDimension = 1;
	public bool overrideOnOffAngles = false;
	public Angle onAngle;
	public Angle offAngle;
	private Angle centralAngle; // Used for continuous on/off angle updating for knowing when the object changes quadrants

	public delegate void DimensionObjectAction();
	public event DimensionObjectAction OnBaseDimensionChange;

	private void Awake() {
		debug = new DebugLogger(this, () => DEBUG);

		objectStartDimension = baseDimension;
		objectEndDimension = baseDimension;
		renderers = GetAllEpitaphRenderers().ToArray();
		startingMaterials = GetAllStartingMaterials(renderers);
		startingLayers = GetAllStartingLayers(renderers);
	}

	public override IEnumerator Start() {
		FindRelevantPillars();

		if (DimensionPillar.activePillar != null) {
			HandleActivePillarChanged(null);
		}

		if (GetComponent<Rigidbody>() != null && GetComponent<IgnoreCollisionsWithOtherDimensions>() == null) {
			gameObject.AddComponent<IgnoreCollisionsWithOtherDimensions>();
		}

		StartCoroutine(base.Start());

		yield break;
	}

	public virtual void SetBaseDimension(int newBaseDimension) {
		if (newBaseDimension != baseDimension && OnBaseDimensionChange != null) {
			OnBaseDimensionChange();
		}
		baseDimension = newBaseDimension;
	}

	void OnEnable() {
		DimensionPillar.OnActivePillarChanged += HandleActivePillarChanged;
		StartCoroutine(WaitOneFrameToSetup());
	}

	IEnumerator WaitOneFrameToSetup() {
		yield return null;
		HandleActivePillarChanged(DimensionPillar.activePillar);
	}

	private void OnDisable() {
		DimensionPillar.OnActivePillarChanged -= HandleActivePillarChanged;
		if (DimensionPillar.activePillar != null && PillarIsRelevant(DimensionPillar.activePillar)) {
			TeardownEventListeners(DimensionPillar.activePillar);
		}
	}

	protected virtual void HandlePillarDimensionChange(int prevDimension, int curDimension) {
		DetermineState(curDimension, DimensionPillar.activePillar.cameraAngleRelativeToPillar, true);
	}

	protected virtual void HandlePlayerMoveAroundPillar(int dimension, Angle angle) {
		VisibilityState nextState = DetermineState(dimension, angle);

		if (nextState != visibilityState) {
			// If in one fixedUpdate frame we move through both onAngle and offAngle, ignore the state change rules
			bool ignoreVisibilityStateChangeRules = IgnoreVisibilityStateChangeRules(DimensionPillar.activePillar.cameraAngleRelativeToPillar, angle);
			SwitchVisibilityState(nextState, ignoreVisibilityStateChangeRules);
		}
	}

	// Returns true if both onAngle and offAngle were covered between prevAngleOfPlayer and angleOfPlayer
	private bool IgnoreVisibilityStateChangeRules(Angle prevAngleOfPlayer, Angle angleOfPlayer) {
		// If in one fixedUpdate frame we move through both onAngle and offAngle, ignore the state change rules
		Angle startAngleForIngoreRulesTest = Angle.IsClockwise(prevAngleOfPlayer, angleOfPlayer) ? prevAngleOfPlayer : angleOfPlayer;
		Angle endAngleForIngoreRulesTest = Angle.IsClockwise(prevAngleOfPlayer, angleOfPlayer) ? angleOfPlayer : prevAngleOfPlayer;

		return Angle.IsAngleBetween(onAngle, startAngleForIngoreRulesTest, endAngleForIngoreRulesTest) &&
			Angle.IsAngleBetween(offAngle, startAngleForIngoreRulesTest, endAngleForIngoreRulesTest);
	}
	
	private void FixedUpdate() {
		if (continuouslyUpdateOnOffAngles && PillarIsRelevant(DimensionPillar.activePillar)) {
			HandleThisObjectMoving();
		}
	}

	private bool PillarIsRelevant(DimensionPillar pillar) {
		return (findPillarsTechnique == FindPillarsTechnique.anyPillar && pillar != null) || (pillarsFound.Contains(pillar));
	}

	// Updates on/off positions for this object, then
	// Updates baseDimension if this object passed the dimensionShift angle, then
	// Updates state based on current cameraAngleRelativeToPillar
	void HandleThisObjectMoving() {
		Angle prevAvgAngleOfObject = centralAngle;
		Angle.Quadrant prevQuadrantOfObject = prevAvgAngleOfObject.quadrant;
		RecalculateOnOffPositions();
		Angle avgAngleOfObject = centralAngle;
		Angle.Quadrant quadrantOfObject = avgAngleOfObject.quadrant;

		//debug.Log(prevAvgAngleOfObject + ", " + avgAngleOfObject);// + "\nOn: " + onAngle + ", Off: " + offAngle);
		if (prevQuadrantOfObject != quadrantOfObject) {
			debug.Log("Prev Quadrant: " + prevQuadrantOfObject + ", Next Quadrant: " + quadrantOfObject);
		}

		bool clockwise = Angle.IsClockwise(prevAvgAngleOfObject, avgAngleOfObject);
		if (prevAvgAngleOfObject != avgAngleOfObject) {
			if (clockwise && prevQuadrantOfObject == Angle.Quadrant.IV && quadrantOfObject == Angle.Quadrant.I) {
				// Bump baseDimension up
				debug.Log("Bumping baseDimension up");
				SetBaseDimension((baseDimension + 1) % (DimensionPillar.activePillar.maxDimension + 1));
			}
			else if (!clockwise && prevQuadrantOfObject == Angle.Quadrant.I && quadrantOfObject == Angle.Quadrant.IV) {
				// Bump baseDimension down
				debug.Log("Bumping baseDimension down");
				SetBaseDimension((baseDimension == 0) ? DimensionPillar.activePillar.maxDimension : baseDimension - 1);
			}

			VisibilityState nextState = DetermineState(DimensionPillar.activePillar.curDimension, DimensionPillar.activePillar.cameraAngleRelativeToPillar, forceCalculations: true);

			if (nextState != visibilityState) {
				SwitchVisibilityState(nextState);
			}
		}
	}

	protected virtual void HandleActivePillarChanged(DimensionPillar prevPillar) {
		if (prevPillar != null && PillarIsRelevant(prevPillar)) {
			TeardownEventListeners(prevPillar);
		}
		FindRelevantPillars();

		if (DimensionPillar.activePillar != null && PillarIsRelevant(DimensionPillar.activePillar)) {
			RecalculateOnOffPositions();
			SetupEventListeners(DimensionPillar.activePillar);
			HandlePillarDimensionChange(-1, DimensionPillar.activePillar.curDimension);
		}
	}

	void SetupEventListeners(DimensionPillar pillar) {
		pillar.OnDimensionChange += HandlePillarDimensionChange;
		pillar.OnPlayerMoveAroundPillar += HandlePlayerMoveAroundPillar;
	}

	void TeardownEventListeners(DimensionPillar pillar) {
		pillar.OnDimensionChange -= HandlePillarDimensionChange;
		pillar.OnPlayerMoveAroundPillar -= HandlePlayerMoveAroundPillar;
	}

	////////////////////////////
	// Administrative & Setup //
	////////////////////////////
	#region adminAndSetup
	void FindRelevantPillars() {
		pillarsFound.Clear();
		switch (findPillarsTechnique) {
			case FindPillarsTechnique.whitelist:
				foreach (var pillar in whitelist) {
					pillarsFound.Add(pillar);
				}
				break;
			case FindPillarsTechnique.automaticSphere:
				SearchForPillarsInSphere(new List<DimensionPillar>());
				break;
			case FindPillarsTechnique.automaticSphereWithBlacklist:
				SearchForPillarsInSphere(blacklist);
				break;
			case FindPillarsTechnique.automaticBox:
				SearchForPillarsInBox(new List<DimensionPillar>());
				break;
			case FindPillarsTechnique.automaticBoxWithBlacklist:
				SearchForPillarsInBox(blacklist);
				break;
			case FindPillarsTechnique.anyPillar:
				break;
		}
	}
	private void SearchForPillarsInSphere(List<DimensionPillar> blacklist) {
		foreach (var pillarMaybe in Physics.OverlapSphere(transform.position, pillarSearchRadius)) {
			DimensionPillar pillar = pillarMaybe.GetComponent<DimensionPillar>();
			if (pillar != null && !blacklist.Contains(pillar)) {
				pillarsFound.Add(pillar);
			}
		}
	}

	private void SearchForPillarsInBox(List<DimensionPillar> blacklist) {
		foreach (var pillarMaybe in Physics.OverlapBox(transform.position, pillarSearchBoxSize / 2f, new Quaternion())) {
			DimensionPillar pillar = pillarMaybe.GetComponent<DimensionPillar>();
			if (pillar != null && !blacklist.Contains(pillar)) {
				pillarsFound.Add(pillar);
			}
		}
	}

	private void RecalculateOnOffPositions() {
		if (overrideOnOffAngles) return;

		if (renderers.Length == 0) {
			Debug.LogError("No renderers found for: " + gameObject.name, gameObject);
			enabled = false;
			return;
		}

		if (DimensionPillar.activePillar == null) {
			onAngle = null; offAngle = null;
			return;
		}

		List<Bounds> allRendererBounds = new List<Bounds>(); // new Vector3[] { renderers[0].GetRendererBounds().min, renderers[0].GetRendererBounds().max };
		foreach (var r in renderers) {
			allRendererBounds.Add(r.GetRendererBounds());
		}
		Vector3 min = new Vector3(allRendererBounds.Min(b => b.min.x), 0, allRendererBounds.Min(b => b.min.z));
		Vector3 max = new Vector3(allRendererBounds.Max(b => b.max.x), 0, allRendererBounds.Max(b => b.max.z));
		Vector3[] corners = new Vector3[] {
			new Vector3(min.x, min.y, min.z),
			new Vector3(min.x, min.y, max.z),
			new Vector3(max.x, min.y, min.z),
			new Vector3(max.x, min.y, max.z),
		};

		Vector3 centerOfObject = (min + max) / 2f;
		centralAngle = DimensionPillar.activePillar.PillarAngleOfPoint(centerOfObject);
		onAngle = Angle.Radians(centralAngle.radians - .001f);
		offAngle = Angle.Radians(centralAngle.radians);
		foreach (var corner in corners) {
			Angle cornerAngle = DimensionPillar.activePillar.PillarAngleOfPoint(corner);
			if (!Angle.IsAngleBetween(cornerAngle, onAngle, offAngle)) {
				Angle replaceOn = Angle.AngleBetween(cornerAngle, offAngle);
				Angle replaceOff = Angle.AngleBetween(cornerAngle, onAngle);
				if (replaceOn.radians > replaceOff.radians) {
					onAngle = cornerAngle;
				}
				else {
					offAngle = cornerAngle;
				}
			}
		}
		onAngle.Reverse();
		offAngle.Reverse();
	}
	#endregion

	////////////////////////
	// State Change Logic //
	////////////////////////
	#region stateChange
	VisibilityState DetermineState(int dimension, Angle angle, bool forceCalculations = false) {
		Angle prevAngleOfPlayer = DimensionPillar.activePillar.cameraAngleRelativeToPillar.normalized;
		Angle angleOfPlayer = angle.normalized;
		if (prevAngleOfPlayer == angleOfPlayer && !forceCalculations) return visibilityState;

		Angle angleOppositeToPlayer = (angleOfPlayer + Angle.D180).normalized;

		bool playerIsOnFirstHalf = angleOfPlayer < Angle.D180;

		bool leftSideOfObjectAppearsLeftOfPillar = Angle.IsAngleBetween(onAngle, angleOppositeToPlayer, angleOfPlayer);
		bool rightSideOfObjectAppearsLeftOfPillar = Angle.IsAngleBetween(offAngle, angleOppositeToPlayer, angleOfPlayer);

		bool objectIsVisuallySplitByPillar = leftSideOfObjectAppearsLeftOfPillar && !rightSideOfObjectAppearsLeftOfPillar;

		// These are relative to the dimension shift angle of the pillar itself
		bool onAngleExistsInFirstHalf = onAngle < Angle.D180;
		bool offAngleExistsInFirstHalf = offAngle < Angle.D180;

		// object fully < 180 degrees
		bool objectExistsWholelyInFirstHalf = onAngleExistsInFirstHalf && offAngleExistsInFirstHalf;
		// object starts > 180 degrees and ends < 180 degrees
		bool objectIsSplitWithinThisDimension = !onAngleExistsInFirstHalf && offAngleExistsInFirstHalf;
		// object fully > 180 degrees
		bool objectExistsWholelyInSecondHalf = !onAngleExistsInFirstHalf && !offAngleExistsInFirstHalf;
		// object starts < 180 degrees and ends > 180 degrees (into next dimension)
		bool objectIsSplitIntoNextDimension = onAngleExistsInFirstHalf && !offAngleExistsInFirstHalf;

		//objectStartDimension = baseDimension - (onAngleIsLeftOfPillar || (objectExistsAtStartOfDimension && objectExistsAtEndOfDimension) ? 1 : 0);
		//objectEndDimension = baseDimension - (!offAngleIsLeftOfPillar || (!objectExistsAtStartOfDimension && objectExistsAtEndOfDimension) ? 0 : 1);



		int maxDimension = DimensionPillar.activePillar.maxDimension;
		VisibilityState nextState = visibilityState;
		// Determine relevant dimensions for this object
		if (objectExistsWholelyInFirstHalf) {
			objectStartDimension = baseDimension;
			objectEndDimension = baseDimension;
		}
		else if (objectIsSplitWithinThisDimension) {
			objectStartDimension = DimensionPillar.activePillar.PrevDimension(baseDimension);
			objectEndDimension = baseDimension;
		}
		else if (objectExistsWholelyInSecondHalf) {
			objectStartDimension = DimensionPillar.activePillar.PrevDimension(baseDimension);
			objectEndDimension = DimensionPillar.activePillar.PrevDimension(baseDimension);
		}
		else if (objectIsSplitIntoNextDimension) {
			objectStartDimension = DimensionPillar.activePillar.PrevDimension(baseDimension);
			objectEndDimension = DimensionPillar.activePillar.PrevDimension(baseDimension);
		}

		int objectStartDimensionPlusOne = (objectStartDimension == maxDimension) ? 0 : objectStartDimension + 1;
		int objectEndDimensionPlusOne = (objectEndDimension == maxDimension) ? 0 : objectEndDimension + 1;
		//debug.Log("Object name: " + gameObject.name + "\nDimension:\t\t\t\t" + dimension + "\nAngle:\t\t\t\t" + angle +
		//	"\n\nAngleOfPlayer:\t\t\t" + angleOfPlayer + "\nAngleOppositeToPlayer:\t\t" + angleOppositeToPlayer +
		//	"\n\nLeftSideOfObjectAppearsLeftOfPillar:\t" + leftSideOfObjectAppearsLeftOfPillar + "\nRightSideOfObjectAppearsLeftOfPillar:\t" + rightSideOfObjectAppearsLeftOfPillar +
		//	"\n\nObjectIsVisuallySplitByPillar:\t\t" + objectIsVisuallySplitByPillar +
		//	"\n\nOnAngleExistsInFirstHalf:\t\t" + onAngleExistsInFirstHalf + "\nOffAngleExistsInFirstHalf:\t\t" + offAngleExistsInFirstHalf +
		//	"\n\nObjectFullyInFirstHalf?:\t\t" + objectExistsWholelyInFirstHalf +
		//	"\nObjectSplitWithinThisDimension?:\t\t" + objectIsSplitWithinThisDimension +
		//	"\nObjectFullyInSecondHalf?:\t\t" + objectExistsWholelyInSecondHalf +
		//	"\nObjectSplitIntoNextDimension?:\t\t" + objectIsSplitIntoNextDimension +
		//	"\n\nObjectStartDimension:\t\t" + objectStartDimension +
		//	"\nObjectEndDimension:\t\t\t" + objectEndDimension
		//);

		if (objectIsVisuallySplitByPillar) {
			if ((dimension == objectStartDimension && !playerIsOnFirstHalf) || (dimension == objectEndDimension && playerIsOnFirstHalf)) {
				nextState = VisibilityState.partiallyVisible;
			}
			else if ((dimension == objectStartDimensionPlusOne && !playerIsOnFirstHalf) || (dimension == objectEndDimensionPlusOne && playerIsOnFirstHalf)) {
				nextState = VisibilityState.partiallyInvisible;
			}
			else {
				nextState = VisibilityState.invisible;
			}
		}
		else {
			if (objectIsSplitWithinThisDimension) {
				if (dimension == objectEndDimension && (angleOfPlayer > offAngle && angleOfPlayer < onAngle)) {
					nextState = VisibilityState.visible;
				}
				else {
					nextState = VisibilityState.invisible;
				}
			}
			else {
				if ((dimension == objectEndDimension && angleOfPlayer > offAngle) || (dimension == objectStartDimensionPlusOne && angleOfPlayer < onAngle)) {
					nextState = VisibilityState.visible;
				}
				else {
					nextState = VisibilityState.invisible;
				}
			}
		}

		//debug.Log(nextState);
		return nextState;
	}
	#endregion

	#region Saving
	public override string ID => $"PillarDimensionObject_{id.uniqueId}";

	[Serializable]
	class PillarDimensionObjectSave {
		bool treatChildrenAsOneObjectRecursively;

		int channel;
		int baseDimension;
		bool reverseVisibilityStates;
		bool ignoreMaterialChanges;
		int curDimensionSetInMaterial;

		int visibilityState;

		bool continuouslyUpdateOnOffAngles;

		int objectStartDimension;
		int objectEndDimension;
		int findPillarsTechnique;

		List<SerializableReference<DimensionPillar>> whitelist = new List<SerializableReference<DimensionPillar>>();
		List<SerializableReference<DimensionPillar>> blacklist = new List<SerializableReference<DimensionPillar>>();
		float pillarSearchRadius;
		SerializableVector3 pillarSearchBoxSize;

		bool overrideOnOffAngles;
		Angle onAngle;
		Angle offAngle;
		Angle centralAngle;

		public PillarDimensionObjectSave(PillarDimensionObject dimensionObj) {
			this.treatChildrenAsOneObjectRecursively = dimensionObj.treatChildrenAsOneObjectRecursively;
			this.channel = dimensionObj.channel;
			this.baseDimension = dimensionObj.baseDimension;
			this.reverseVisibilityStates = dimensionObj.reverseVisibilityStates;
			this.ignoreMaterialChanges = dimensionObj.ignoreMaterialChanges;
			this.curDimensionSetInMaterial = dimensionObj.curDimensionSetInMaterial;
			this.visibilityState = (int)dimensionObj.visibilityState;
			this.continuouslyUpdateOnOffAngles = dimensionObj.continuouslyUpdateOnOffAngles;
			this.objectStartDimension = dimensionObj.objectStartDimension;
			this.objectEndDimension = dimensionObj.objectEndDimension;
			this.findPillarsTechnique = (int)dimensionObj.findPillarsTechnique;
			this.whitelist = dimensionObj.whitelist.Select<DimensionPillar, SerializableReference<DimensionPillar>>(p => p).ToList();
			this.blacklist = dimensionObj.blacklist.Select<DimensionPillar, SerializableReference<DimensionPillar>>(p => p).ToList();
			this.pillarSearchRadius = dimensionObj.pillarSearchRadius;
			this.pillarSearchBoxSize = dimensionObj.pillarSearchBoxSize;
			this.overrideOnOffAngles = dimensionObj.overrideOnOffAngles;
			this.onAngle = dimensionObj.onAngle;
			this.offAngle = dimensionObj.offAngle;
			this.centralAngle = dimensionObj.centralAngle;
		}

		public void LoadSave(PillarDimensionObject dimensionObj) {
			dimensionObj.treatChildrenAsOneObjectRecursively = this.treatChildrenAsOneObjectRecursively;
			dimensionObj.channel = this.channel;
			dimensionObj.baseDimension = this.baseDimension;
			dimensionObj.reverseVisibilityStates = this.reverseVisibilityStates;
			dimensionObj.ignoreMaterialChanges = this.ignoreMaterialChanges;
			dimensionObj.curDimensionSetInMaterial = this.curDimensionSetInMaterial;
			dimensionObj.startingVisibilityState = (VisibilityState)this.visibilityState;
			dimensionObj.visibilityState = (VisibilityState)this.visibilityState;

			dimensionObj.continuouslyUpdateOnOffAngles = this.continuouslyUpdateOnOffAngles;
			dimensionObj.objectStartDimension = this.objectStartDimension;
			dimensionObj.objectEndDimension = this.objectEndDimension;
			dimensionObj.findPillarsTechnique = (FindPillarsTechnique)this.findPillarsTechnique;
			dimensionObj.whitelist = this.whitelist.Select(p => p.Reference).ToList();
			dimensionObj.blacklist = this.blacklist.Select(p => p.Reference).ToList();
			dimensionObj.pillarSearchRadius = this.pillarSearchRadius;
			dimensionObj.pillarSearchBoxSize = this.pillarSearchBoxSize;
			dimensionObj.overrideOnOffAngles = this.overrideOnOffAngles;
			dimensionObj.onAngle = this.onAngle;
			dimensionObj.offAngle = this.offAngle;
			dimensionObj.centralAngle = this.centralAngle;

			if (dimensionObj.gameObject.activeInHierarchy) {
				dimensionObj.SwitchVisibilityState(dimensionObj.visibilityState, true);
			}
		}
	}

	public override object GetSaveObject() {
		return new PillarDimensionObjectSave(this);
	}

	public override void LoadFromSavedObject(object savedObject) {
		PillarDimensionObjectSave save = savedObject as PillarDimensionObjectSave;

		save.LoadSave(this);
	}
	#endregion
}
