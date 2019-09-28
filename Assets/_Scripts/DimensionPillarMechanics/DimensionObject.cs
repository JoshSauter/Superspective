using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using EpitaphUtils.ShaderUtils;
using System.Linq;

public enum VisibilityState {
	invisible,
	partiallyVisible,
	visible,
	partiallyInvisible,
};

public class DimensionObject : MonoBehaviour {
	public bool DEBUG = false;
	public bool treatChildrenAsOneObjectRecursively = false;
	public bool continuouslyUpdateOnOffAngles = false;
	DebugLogger debug;

	[Range(0, 7)]
	public int baseDimension = 1;
	public int objectStartDimension;
	public int objectEndDimension;
	public bool reverseVisibilityStates = false;
	int curDimensionSetInMaterial;

	////////////////////////////////////
	// Techniques for finding pillars //
	////////////////////////////////////
	public enum FindPillarsTechnique {
		whitelist,
		automaticSphere,
		automaticSphereWithBlacklist,
		automaticBox,
		automaticBoxWithBlacklist
	}
	public FindPillarsTechnique findPillarsTechnique;
	private List<DimensionPillar> pillarsFound = new List<DimensionPillar>();
	// Whitelist/blacklist pillars
	public List<DimensionPillar> whitelist = new List<DimensionPillar>();
	public List<DimensionPillar> blacklist = new List<DimensionPillar>();
	public float pillarSearchRadius = 40;
	public Vector3 pillarSearchBoxSize = Vector3.one * 80;

	///////////////////
	// On/Off Angles //
	///////////////////
	public bool overrideOnOffAngles = false;
	public Angle onAngle;
	public Angle offAngle;
	private Angle centralAngle;	// Used for continuous on/off angle updating for knowing when the object changes quadrants

	EpitaphRenderer[] renderers;
	Material dimensionObjectMaterial, inverseDimensionObjectMaterial, dimensionObjectSpecularMaterial, inverseDimensionObjectSpecularMaterial;
	Dictionary<EpitaphRenderer, Material[]> startingMaterials;

	public VisibilityState startingVisibilityState = VisibilityState.visible;
	public VisibilityState visibilityState = VisibilityState.visible;
	static Dictionary<VisibilityState, HashSet<VisibilityState>> nextStates = new Dictionary<VisibilityState, HashSet<VisibilityState>> {
		{ VisibilityState.invisible, new HashSet<VisibilityState> { VisibilityState.partiallyVisible, VisibilityState.partiallyInvisible } },
		{ VisibilityState.partiallyVisible, new HashSet<VisibilityState> { VisibilityState.invisible, VisibilityState.visible } },
		{ VisibilityState.visible, new HashSet<VisibilityState> { VisibilityState.partiallyVisible, VisibilityState.partiallyInvisible } },
		{ VisibilityState.partiallyInvisible, new HashSet<VisibilityState> { VisibilityState.invisible, VisibilityState.visible } }
	};

	#region events
	public delegate void DimensionObjectAction();
	public event DimensionObjectAction OnBaseDimensionChange;
	public delegate void DimensionObjectStateChangeAction(VisibilityState visibilityState);
	public event DimensionObjectStateChangeAction OnStateChange;
	#endregion

	void Start() {
		debug = new DebugLogger(gameObject, DEBUG);

		objectStartDimension = baseDimension;
		objectEndDimension = baseDimension;
		renderers = GetAllEpitaphRenderers().ToArray();
		startingMaterials = GetAllStartingMaterials(renderers);

		FindRelevantPillars();
		DimensionPillar.OnActivePillarChanged += HandleActivePillarChanged;
		SwitchVisibilityState(visibilityState, true);

		if (GetComponent<Rigidbody>() != null && GetComponent<IgnoreCollisionsWithOtherDimensions>() == null) {
			gameObject.AddComponent<IgnoreCollisionsWithOtherDimensions>();
		}
    }

	private void SetBaseDimension(int newBaseDimension) {
		if (newBaseDimension != baseDimension && OnBaseDimensionChange != null) {
			OnBaseDimensionChange();
		}
		baseDimension = newBaseDimension;
	}

	void HandlePillarDimensionChange(int prevDimension, int curDimension) {
		if (IsRelevantDimension(curDimension)) {
			SetDimensionValueInMaterials(curDimension);
		}
	}

	private bool IsRelevantDimension(int dimension) {
		return (dimension == objectStartDimension) || (dimension == objectEndDimension);
	}

	void HandlePlayerMoveAroundPillar(int dimension, Angle angle) {
		VisibilityState nextState = DetermineState(dimension, angle);

		if (nextState != visibilityState) {
			// If in one fixedUpdate frame we move through both onAngle and offAngle, ignore the state change rules
			bool ignoreVisibilityStateChangeRules = IgnoreVisibilityStateChangeRules(DimensionPillar.activePillar.cameraAngleRelativeToPillar, angle);
			SwitchVisibilityState(nextState, ignoreVisibilityStateChangeRules);
			SetDimensionValueInMaterials(dimension);
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
		if (continuouslyUpdateOnOffAngles && pillarsFound.Contains(DimensionPillar.activePillar)) {
			HandleThisObjectMoving();
		}
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

		//print(prevAvgAngleOfObject + ", " + avgAngleOfObject);// + "\nOn: " + onAngle + ", Off: " + offAngle);
		if (prevQuadrantOfObject != quadrantOfObject) {
			print("Prev Quadrant: " + prevQuadrantOfObject + ", Next Quadrant: " + quadrantOfObject);
		}

		bool clockwise = Angle.IsClockwise(prevAvgAngleOfObject, avgAngleOfObject);
		if (prevAvgAngleOfObject != avgAngleOfObject) {
			if (clockwise && prevQuadrantOfObject == Angle.Quadrant.IV && quadrantOfObject == Angle.Quadrant.I) {
				// Bump baseDimension up
				print("Bumping baseDimension up");
				SetBaseDimension((baseDimension + 1) % (DimensionPillar.activePillar.maxDimension + 1));
			}
			else if (!clockwise && prevQuadrantOfObject == Angle.Quadrant.I && quadrantOfObject == Angle.Quadrant.IV) {
				// Bump baseDimension down
				print("Bumping baseDimension down");
				SetBaseDimension((baseDimension == 0) ? DimensionPillar.activePillar.maxDimension : baseDimension - 1);
			}

			VisibilityState nextState = DetermineState(DimensionPillar.activePillar.curDimension, DimensionPillar.activePillar.cameraAngleRelativeToPillar, forceCalculations: true);

			if (nextState != visibilityState) {
				SwitchVisibilityState(nextState);
				SetDimensionValueInMaterials(DimensionPillar.activePillar.curDimension);
			}
		}
	}

	void HandleActivePillarChanged(DimensionPillar prevPillar) {
		if (prevPillar != null && pillarsFound.Contains(prevPillar)) {
			prevPillar.OnDimensionChange -= HandlePillarDimensionChange;
			prevPillar.OnPlayerMoveAroundPillar -= HandlePlayerMoveAroundPillar;
		}
		FindRelevantPillars();

		if (DimensionPillar.activePillar != null && pillarsFound.Contains(DimensionPillar.activePillar)) {
			RecalculateOnOffPositions();
			DimensionPillar.activePillar.OnDimensionChange += HandlePillarDimensionChange;
			DimensionPillar.activePillar.OnPlayerMoveAroundPillar += HandlePlayerMoveAroundPillar;
			HandlePillarDimensionChange(-1, DimensionPillar.activePillar.curDimension);
		}
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
	public void SwitchVisibilityState(VisibilityState nextState, bool ignoreTransitionRules = false) {
		if (!ignoreTransitionRules && !IsValidNextState(nextState)) return;

		debug.Log("State transition: " + visibilityState + " --> " + nextState);

		int setDimension = -1;
		switch (nextState) {
			case VisibilityState.invisible:
				visibilityState = VisibilityState.invisible;
				break;
			case VisibilityState.partiallyVisible:
				visibilityState = VisibilityState.partiallyVisible;
				setDimension = (DimensionPillar.activePillar != null) ? DimensionPillar.activePillar.curDimension : baseDimension;
				break;
			case VisibilityState.visible:
				visibilityState = VisibilityState.visible;
				break;
			case VisibilityState.partiallyInvisible:
				visibilityState = VisibilityState.partiallyInvisible;
				setDimension = (DimensionPillar.activePillar != null) ? DimensionPillar.activePillar.curDimension : baseDimension;
				break;
		}

		foreach (var r in renderers) {
			SetMaterials(r);

		}
		if (setDimension > 0) {
			SetDimensionValueInMaterials(setDimension);
		}

		if (OnStateChange != null) {
			OnStateChange(nextState);
		}
	}

	private bool IsValidNextState(VisibilityState nextState) {
		return nextStates[visibilityState].Contains(nextState);
	}

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
			objectStartDimension = (baseDimension == 0) ? maxDimension : baseDimension - 1;
			objectEndDimension = baseDimension;
		}
		else if (objectExistsWholelyInSecondHalf) {
			objectStartDimension = (baseDimension == 0) ? maxDimension : baseDimension - 1;
			objectEndDimension = (baseDimension == 0) ? maxDimension : baseDimension - 1;
		}
		else if (objectIsSplitIntoNextDimension) {
			objectStartDimension = (baseDimension == 0) ? maxDimension : baseDimension - 1;
			objectEndDimension = (baseDimension == 0) ? maxDimension : baseDimension - 1;
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

	///////////////////////////
	// Material Change Logic //
	///////////////////////////
	#region materials
	void SetMaterials(EpitaphRenderer renderer) {
		Material[] normalMaterials = startingMaterials[renderer];
		Material[] newMaterials;
		if (!reverseVisibilityStates) {
			if (visibilityState == VisibilityState.partiallyVisible) {
				newMaterials = normalMaterials.Select(m => GetDimensionObjectMaterial(m)).ToArray();
			}
			else if (visibilityState == VisibilityState.partiallyInvisible) {
				newMaterials = normalMaterials.Select(m => GetInverseDimensionObjectMaterial(m)).ToArray();
			}
			else {
				newMaterials = normalMaterials;
			}
		}
		else {
			if (visibilityState == VisibilityState.partiallyVisible) {
				newMaterials = normalMaterials.Select(m => GetInverseDimensionObjectMaterial(m)).ToArray();
			}
			else if (visibilityState == VisibilityState.partiallyInvisible) {
				newMaterials = normalMaterials.Select(m => GetDimensionObjectMaterial(m)).ToArray();
			}
			else {
				newMaterials = normalMaterials;
			}
		}

		bool invisibleLayer = visibilityState == VisibilityState.invisible;
		if (reverseVisibilityStates) invisibleLayer = visibilityState == VisibilityState.visible;
		renderer.gameObject.layer = LayerMask.NameToLayer(invisibleLayer ? "Invisible" : "Default");

		renderer.SetMaterials(newMaterials);
	}

	void SetDimensionValueInMaterials(int newDimensionValue) {
		if (curDimensionSetInMaterial != newDimensionValue && (visibilityState == VisibilityState.partiallyVisible || visibilityState == VisibilityState.partiallyInvisible)) {
			foreach (var r in renderers) {
				r.SetInt("_Dimension", newDimensionValue);
			}
			curDimensionSetInMaterial = newDimensionValue;
		}
	}

	List<EpitaphRenderer> GetAllEpitaphRenderers() {
		List<EpitaphRenderer> allRenderers = new List<EpitaphRenderer>();
		if (!treatChildrenAsOneObjectRecursively) {
			EpitaphRenderer thisRenderer = GetComponent<EpitaphRenderer>();
			if (thisRenderer == null && GetComponent<Renderer>() != null) {
				thisRenderer = gameObject.AddComponent<EpitaphRenderer>();
			}
			if (thisRenderer != null) {
				allRenderers.Add(thisRenderer);
			}
		}
		else {
			SetEpitaphRenderersRecursively(transform, ref allRenderers);
		}
		return allRenderers;
	}

	void SetEpitaphRenderersRecursively(Transform parent, ref List<EpitaphRenderer> renderersSoFar) {
		// Children who have DimensionObject scripts are treated on only by their own settings
		if (parent != transform && parent.GetComponent<DimensionObject>() != null) return;

		EpitaphRenderer thisRenderer = parent.GetComponent<EpitaphRenderer>();
		if (thisRenderer == null && parent.GetComponent<Renderer>() != null) {
			thisRenderer = parent.gameObject.AddComponent<EpitaphRenderer>();
		}

		if (thisRenderer != null) {
			renderersSoFar.Add(thisRenderer);
		}

		if (parent.childCount > 0) {
			foreach (Transform child in parent) {
				SetEpitaphRenderersRecursively(child, ref renderersSoFar);
			}
		}
	}

	Dictionary<EpitaphRenderer, Material[]> GetAllStartingMaterials(EpitaphRenderer[] renderers) {
		Dictionary<EpitaphRenderer, Material[]> dict = new Dictionary<EpitaphRenderer, Material[]>();
		foreach (var r in renderers) {
			dict.Add(r, r.GetMaterials());
		}

		return dict;
	}

	private Material GetDimensionObjectMaterial(Material normalMaterial) {
		Material newMaterial = null;
		switch (normalMaterial.shader.name) {
			case "Custom/Unlit":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionObject"));
				break;
			case "Standard (Specular setup)":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionObjectSpecular"));
				break;
			case "TextMeshPro/Distance Field":
				if (normalMaterial.name.Contains("Signika-Regular SDF Material")) {
					newMaterial = Resources.Load<Material>("Fonts/Signika-Regular SDF DimensionObject");
				}
				else {
					Debug.LogWarning("No DimensionObject font material for " + normalMaterial.name);
				}
				break;
			default:
				Debug.LogWarning("No matching dimensionObjectShader for shader " + normalMaterial.shader.name);
				break;
		}

		newMaterial.CopyMatchingPropertiesFromMaterial(normalMaterial);
		return newMaterial;
	}

	private Material GetInverseDimensionObjectMaterial(Material normalMaterial) {
		Material newMaterial = null;
		switch (normalMaterial.shader.name) {
			case "Custom/Unlit":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/InverseDimensionObject"));
				break;
			case "Standard (Specular setup)":
				newMaterial = new Material(Shader.Find("Custom/DimensionShaders/InverseDimensionObjectSpecular"));
				break;
			case "TextMeshPro/Distance Field":
				if (normalMaterial.name.Contains("Signika-Regular SDF Material")) {
					newMaterial = Resources.Load<Material>("Fonts/Signika-Regular SDF InverseDimensionObject");
				}
				else {
					Debug.LogWarning("No DimensionObject font material for " + normalMaterial.name);
				}
				break;
			default:
				Debug.LogWarning("No matching dimensionObjectShader for shader " + normalMaterial.shader.name);
				break;
		}

		newMaterial.CopyMatchingPropertiesFromMaterial(normalMaterial);
		return newMaterial;
	}
	#endregion
}
