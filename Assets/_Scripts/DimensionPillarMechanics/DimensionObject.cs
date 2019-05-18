using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using EpitaphUtils.ShaderUtils;
using System.Linq;
using System;

public enum VisibilityState {
	invisibleHigherDimension,		// Invisible because the object exists in a higher dimension than the pillar is rendering
	partiallyVisible,
	visible,
	partiallyInvisible,
	invisibleLowerDimension			// Invisible because the object exists in a lower dimension than the pillar is rendering
};

public class DimensionObject : MonoBehaviour {
	public bool DEBUG = false;
	public bool treatChildrenAsOneObjectRecursively = false;
	DebugLogger debug;

	[Range(0, 7)]
	public int baseDimension = 1;
	public int objectStartDimension;
	public int objectEndDimension;
	public bool objectExistsAtStartOfDimension = false;
	int curDimensionSetInMaterial;
	public bool existsInNextDimension {
		get {
			if (DimensionPillar.activePillar != null) {
				return Angle.IsAngleBetween(DimensionPillar.activePillar.dimensionShiftAngle, offAngle, onAngle);
			}
			else return false;
		}
	}

	public Angle onAngle;
	public Angle offAngle;

	Angle invisibleToPartiallyVisibleAngle { get { return onAngle; } }
	Angle partiallyVisibleToVisibleAngle { get { return offAngle; } }
	Angle visibleToPartiallyInvisibleAngle { get { return Angle.D360 + onAngle; } }
	Angle partiallyInvisibleToInvisibleAngle { get { return Angle.D360 + offAngle; } }

	EpitaphRenderer[] renderers;
	Material dimensionObjectMaterial, inverseDimensionObjectMaterial, dimensionObjectSpecularMaterial, inverseDimensionObjectSpecularMaterial;
	Dictionary<EpitaphRenderer, Material[]> startingMaterials;
	Dictionary<EpitaphRenderer, Material[]> dimensionMaterials;
	Dictionary<EpitaphRenderer, Material[]> inverseDimensionMaterials;

	public VisibilityState visibilityState = VisibilityState.visible;
	static Dictionary<VisibilityState, HashSet<VisibilityState>> nextStates = new Dictionary<VisibilityState, HashSet<VisibilityState>> {
		{ VisibilityState.invisibleHigherDimension, new HashSet<VisibilityState> { VisibilityState.partiallyVisible } },
		{ VisibilityState.partiallyVisible, new HashSet<VisibilityState> { VisibilityState.invisibleHigherDimension, VisibilityState.visible } },
		{ VisibilityState.visible, new HashSet<VisibilityState> { VisibilityState.partiallyVisible, VisibilityState.partiallyInvisible } },
		{ VisibilityState.partiallyInvisible, new HashSet<VisibilityState> { VisibilityState.invisibleLowerDimension, VisibilityState.visible } },
		{ VisibilityState.invisibleLowerDimension, new HashSet<VisibilityState> { VisibilityState.partiallyInvisible } }
	};

	void Start() {
		debug = new DebugLogger(gameObject, DEBUG);

		objectStartDimension = baseDimension;
		objectEndDimension = baseDimension;
		renderers = GetAllEpitaphRenderers().ToArray();
		startingMaterials = GetAllStartingMaterials(renderers);
		dimensionMaterials = GetDimensionMaterials(startingMaterials);
		inverseDimensionMaterials = GetInverseDimensionMaterials(startingMaterials);

		DimensionPillar.OnActivePillarChanged += HandleActivePillarChanged;
		SwitchVisibilityState(visibilityState, true);
    }

	void HandlePillarDimensionChange(int prevDimension, int curDimension) {
		if (IsRelevantDimension(curDimension)) {
			SetDimensionValueInMaterials(curDimension);
		}
	}

	private bool IsRelevantDimension(int dimension) {
		return (dimension == objectStartDimension) || (dimension == objectEndDimension);
	}

	// Note: Unlike the Angle method of the same name, this one just compares straight radians values
	private bool IsAngleBetween(Angle test, Angle a, Angle b) {
		return (test > a && test < b);
	}
	void HandlePlayerMoveAroundPillar(Angle prevAngle, Angle newAngle, bool clockwise) {
		int curPillarDimension = DimensionPillar.activePillar.curDimension;
		// Going clockwise
		if (clockwise) {
			// InvisibleHigherDimension -> Partially Visible
			if (IsAngleBetween(invisibleToPartiallyVisibleAngle, prevAngle, newAngle) && visibilityState == VisibilityState.invisibleHigherDimension) {
				SwitchVisibilityState(VisibilityState.partiallyVisible);
			}
			// Visible -> Partially Invisible
			else if (IsAngleBetween(visibleToPartiallyInvisibleAngle, prevAngle, newAngle) && visibilityState == VisibilityState.visible) {
				SwitchVisibilityState(VisibilityState.partiallyInvisible);
			}
			// Partially Visible -> Visible
			else if (IsAngleBetween(partiallyVisibleToVisibleAngle, prevAngle, newAngle) && visibilityState == VisibilityState.partiallyVisible) {
				SwitchVisibilityState(VisibilityState.visible);
			}
			// Partially Invisible -> InvisibleLowerDimension
			else if (IsAngleBetween(partiallyInvisibleToInvisibleAngle, prevAngle, newAngle) && visibilityState == VisibilityState.partiallyInvisible) {
				SwitchVisibilityState(VisibilityState.invisibleLowerDimension);
			}
		}
		// Going counter-clockwise
		else {
			// Visible -> Partially Visible
			if (IsAngleBetween(partiallyVisibleToVisibleAngle, newAngle, prevAngle) && visibilityState == VisibilityState.visible) {
				SwitchVisibilityState(VisibilityState.partiallyVisible);
			}
			// InvisibleLowerDimension -> Partially Invisible
			else if (IsAngleBetween(partiallyInvisibleToInvisibleAngle, newAngle, prevAngle) && visibilityState == VisibilityState.invisibleLowerDimension) {
				SwitchVisibilityState(VisibilityState.partiallyInvisible);
			}
			// Partially Visible -> InvisibleHigherDimension
			else if (IsAngleBetween(invisibleToPartiallyVisibleAngle, newAngle, prevAngle) && visibilityState == VisibilityState.partiallyVisible) {
				SwitchVisibilityState(VisibilityState.invisibleHigherDimension);
			}
			// Partially Invisible -> Visible
			else if (IsAngleBetween(visibleToPartiallyInvisibleAngle, newAngle, prevAngle) && visibilityState == VisibilityState.partiallyInvisible) {
				SwitchVisibilityState(VisibilityState.visible);
			}
		}

		SetDimensionValueInMaterials(curPillarDimension);
	}

	void HandleActivePillarChanged(DimensionPillar prevPillar) {
		if (prevPillar != null) {
			prevPillar.OnDimensionChange -= HandlePillarDimensionChange;
			prevPillar.OnPlayerMoveAroundPillar -= HandlePlayerMoveAroundPillar;
			prevPillar.OnDimensionShiftAngleChange -= DetermineInitialState;
		}
		RecalculateOnOffPositions();
		DimensionPillar.activePillar.OnDimensionChange += HandlePillarDimensionChange;
		DimensionPillar.activePillar.OnPlayerMoveAroundPillar += HandlePlayerMoveAroundPillar;
		DimensionPillar.activePillar.OnDimensionShiftAngleChange += DetermineInitialState;
		HandlePillarDimensionChange(-1, DimensionPillar.activePillar.curDimension);

		DetermineInitialState();
	}

	private void RecalculateOnOffPositions() {
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
		Angle baseAngle = DimensionPillar.activePillar.PillarAngleOfPoint(centerOfObject);
		onAngle = Angle.Radians(baseAngle.radians - .001f);
		offAngle = Angle.Radians(baseAngle.radians);
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

	public void SwitchVisibilityState(VisibilityState nextState, bool ignoreTransitionRules = false) {
		if (!ignoreTransitionRules && !IsValidNextState(nextState)) return;

		int setDimension = -1;
		switch (nextState) {
			case VisibilityState.invisibleHigherDimension:
				debug.Log("Becoming invisible (object now exists in a higher dimension than what the pillar is rendering)");
				visibilityState = VisibilityState.invisibleHigherDimension;

				break;
			case VisibilityState.partiallyVisible:
				debug.Log("Becoming partially visible");
				visibilityState = VisibilityState.partiallyVisible;

				setDimension = (DimensionPillar.activePillar != null) ? DimensionPillar.activePillar.curDimension : baseDimension;
				break;
			case VisibilityState.visible:
				debug.Log("Becoming visible");
				visibilityState = VisibilityState.visible;

				break;
			case VisibilityState.partiallyInvisible:
				debug.Log("Becoming partially invisible");
				visibilityState = VisibilityState.partiallyInvisible;

				setDimension = (DimensionPillar.activePillar != null) ? DimensionPillar.activePillar.curDimension : baseDimension;
				break;
			case VisibilityState.invisibleLowerDimension:
				debug.Log("Becoming invisible (object now exists in a lower dimension than what the pillar is rendering)");
				visibilityState = VisibilityState.invisibleLowerDimension;

				break;
		}

		foreach (var r in renderers) {
			SetMaterials(r);

		}
		if (setDimension > 0) {
			SetDimensionValueInMaterials(setDimension);
		}
	}

	private bool IsValidNextState(VisibilityState nextState) {
		return nextStates[visibilityState].Contains(nextState);
	}

	void DetermineInitialState() {
		int activeDimension = DimensionPillar.activePillar.curDimension;
		Angle pillarDimensionChangeAngle = Angle.D0;
		Angle oppositeDimensionChangeAngle = Angle.D180;

		bool onAngleIsLeftOfPillar = Angle.IsAngleBetween(onAngle, oppositeDimensionChangeAngle, pillarDimensionChangeAngle);
		bool offAngleIsLeftOfPillar = Angle.IsAngleBetween(offAngle, oppositeDimensionChangeAngle, pillarDimensionChangeAngle);

		bool objectExistsAtEndOfDimension = !onAngleIsLeftOfPillar && offAngleIsLeftOfPillar;
		bool objectExistsBetweenDimensions = onAngleIsLeftOfPillar && !offAngleIsLeftOfPillar;
		bool objectExistsLeftOfPillar = (onAngleIsLeftOfPillar && offAngleIsLeftOfPillar) || (objectExistsAtEndOfDimension && objectExistsAtStartOfDimension);
		bool objectExistsRightOfPillar = (!onAngleIsLeftOfPillar && !offAngleIsLeftOfPillar);

		objectStartDimension = baseDimension - (onAngleIsLeftOfPillar || (objectExistsAtStartOfDimension && objectExistsAtEndOfDimension) ? 1 : 0);
		objectEndDimension = baseDimension - (!offAngleIsLeftOfPillar || (!objectExistsAtStartOfDimension && objectExistsAtEndOfDimension) ? 0 : 1);

		if (objectExistsBetweenDimensions) {
			if (activeDimension == objectEndDimension) {
				visibilityState = VisibilityState.partiallyVisible;
			}
			else if (activeDimension == objectEndDimension+1) {
				visibilityState = VisibilityState.partiallyInvisible;
			}
			else if (activeDimension < objectEndDimension) {
				visibilityState = VisibilityState.invisibleHigherDimension;
			}
			else if (activeDimension > objectEndDimension+1) {
				visibilityState = VisibilityState.invisibleLowerDimension;
			}
		}
		else {
			if (activeDimension == objectStartDimension+1) {
				visibilityState = VisibilityState.visible;
			}
			else if (activeDimension < objectStartDimension+1) {
				visibilityState = VisibilityState.invisibleHigherDimension;
			}
			else if (activeDimension > objectStartDimension+1) {
				visibilityState = VisibilityState.invisibleLowerDimension;
			}
		}

		onAngle += objectStartDimension * Angle.D360;
		offAngle += objectEndDimension * Angle.D360;

		SwitchVisibilityState(visibilityState, true);
	}

	void SetMaterials(EpitaphRenderer renderer) {
		Material[] normalMaterials = startingMaterials[renderer];
		Material[] newMaterials;

		if (visibilityState == VisibilityState.partiallyVisible) {
			newMaterials = dimensionMaterials[renderer];
		}
		else if (visibilityState == VisibilityState.partiallyInvisible) {
			newMaterials = inverseDimensionMaterials[renderer];
		}
		else {
			newMaterials = normalMaterials;
		}

		bool invisibleLayer = visibilityState == VisibilityState.invisibleLowerDimension || visibilityState == VisibilityState.invisibleHigherDimension;
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

	Dictionary<EpitaphRenderer, Material[]> GetDimensionMaterials(Dictionary<EpitaphRenderer, Material[]> startingMaterials) {
		Dictionary<EpitaphRenderer, Material[]> dimensionMaterials = new Dictionary<EpitaphRenderer, Material[]>();
		foreach (var key in startingMaterials.Keys) {
			Material[] startingMaterialsForThisRenderer = startingMaterials[key];
			Material[] dimensionMaterialsForThisRenderer = startingMaterialsForThisRenderer.ToList().Select(m => GetDimensionObjectMaterial(m)).ToArray();

			dimensionMaterials.Add(key, dimensionMaterialsForThisRenderer);
		}
		return dimensionMaterials;
	}

	private Material GetDimensionObjectMaterial(Material normalMaterial) {
		Material newMaterial = null;
		if (normalMaterial.shader.name == "Custom/Unlit") {
			newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionObject"));
		}
		else if (normalMaterial.shader.name == "Standard (Specular setup)") {
			newMaterial = new Material(Shader.Find("Custom/DimensionShaders/DimensionObjectSpecular"));
		}
		else {
			Debug.LogWarning("No matching dimensionObjectShader for shader " + normalMaterial.shader.name);
		}

		newMaterial.CopyMatchingPropertiesFromMaterial(normalMaterial);
		return newMaterial;
	}

	Dictionary<EpitaphRenderer, Material[]> GetInverseDimensionMaterials(Dictionary<EpitaphRenderer, Material[]> startingMaterials) {
		Dictionary<EpitaphRenderer, Material[]> inverseDimensionMaterials = new Dictionary<EpitaphRenderer, Material[]>();
		foreach (var key in startingMaterials.Keys) {
			Material[] startingMaterialsForThisRenderer = startingMaterials[key];
			Material[] inverseDimensionMaterialsForThisRenderer = startingMaterialsForThisRenderer.ToList().Select(m => GetInverseDimensionObjectMaterial(m)).ToArray();

			inverseDimensionMaterials.Add(key, inverseDimensionMaterialsForThisRenderer);
		}
		return inverseDimensionMaterials;
	}

	private Material GetInverseDimensionObjectMaterial(Material normalMaterial) {
		Material newMaterial = null;
		if (normalMaterial.shader.name == "Custom/Unlit") {
			newMaterial = new Material(Shader.Find("Custom/DimensionShaders/InverseDimensionObject"));
		}
		else if (normalMaterial.shader.name == "Standard (Specular setup)") {
			newMaterial = new Material(Shader.Find("Custom/DimensionShaders/InverseDimensionObjectSpecular"));
		}
		else {
			Debug.LogWarning("No matching dimensionObjectShader for shader " + normalMaterial.shader.name);
		}

		newMaterial.CopyMatchingPropertiesFromMaterial(normalMaterial);
		return newMaterial;
	}
}
