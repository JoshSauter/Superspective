using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum VisibilityState {
	invisible,
	partiallyVisible,
	visible
};


public class PartiallyVisibleObject : MonoBehaviour {
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
	private List<ObscurePillar> pillarsFound = new List<ObscurePillar>();
	// Whitelist/blacklist pillars
	public List<ObscurePillar> whitelist = new List<ObscurePillar>();
	public List<ObscurePillar> blacklist = new List<ObscurePillar>();
	public float pillarSearchRadius = 40;
	public Vector3 pillarSearchBoxSize = Vector3.one * 80;

	///////////////////////////////////
	// Initial values and references //
	///////////////////////////////////
	public bool setLayerRecursively = true;
	public bool setMaterialColorOnStart = true;
	public Color materialColor = Color.black;
	public VisibilityState startingVisibilityState;
	VisibilityState oppositeStartingVisibilityState;
	bool negativeRenderer;
	int initialLayer;
	int invisibleLayer;
	Material visibleMaterial;
	Material initialMaterial;
	EpitaphRenderer renderer;

	//////////////////////
	// Visibility state //
	//////////////////////
	private VisibilityState _visibilityState;
	public VisibilityState visibilityState {
		get {
			return _visibilityState;
		}
	}

	///////////////////
	// On/Off Angles //
	///////////////////
	public bool overrideOnOffAngles = false;
	public Angle onAngle;
	public Angle offAngle;

#region events
	public delegate void VisibilityStateChangeAction(VisibilityState newState);
	public event VisibilityStateChangeAction OnVisibilityStateChange;
#endregion

	private void Awake() {
		invisibleLayer = LayerMask.NameToLayer("Invisible");
		visibleMaterial = Resources.Load<Material>("Materials/Unlit/Unlit");
	}

	void OnEnable() {
		initialLayer = gameObject.layer;
		renderer = gameObject.AddComponent<EpitaphRenderer>();
		initialMaterial = renderer.GetMaterial();
		negativeRenderer = initialMaterial.name.Contains("Neg");
		oppositeStartingVisibilityState = startingVisibilityState == VisibilityState.visible ? VisibilityState.invisible : VisibilityState.visible;

		ObscurePillar.OnPlayerMoveAroundPillar += HandlePlayerMoveAroundPillar;
		ObscurePillar.OnActivePillarChanged += HandlePillarChanged;
		ObscurePillar.OnActivePillarChanged += ResetVisibilityStateIfNeeded;
	}

	private void OnDisable() {
		ObscurePillar.OnPlayerMoveAroundPillar -= HandlePlayerMoveAroundPillar;
		ObscurePillar.OnActivePillarChanged -= HandlePillarChanged;
		ObscurePillar.OnActivePillarChanged -= ResetVisibilityStateIfNeeded;
	}

	private void Start() {
		SetVisibilityState(startingVisibilityState);
		if (setMaterialColorOnStart) {
			renderer.SetMainColor(materialColor);
		}
		HandlePillarChanged();
	}

	private void HandlePillarChanged() {
		HandlePillarChanged(null);
	}

	private void HandlePillarChanged(ObscurePillar unused) {

		pillarsFound.Clear();
		switch (findPillarsTechnique) {
			case FindPillarsTechnique.whitelist:
				foreach (var pillar in whitelist) {
					pillarsFound.Add(pillar);
				}
				break;
			case FindPillarsTechnique.automaticSphere:
				SearchForPillarsInSphere(new List<ObscurePillar>());
				break;
			case FindPillarsTechnique.automaticSphereWithBlacklist:
				SearchForPillarsInSphere(blacklist);
				break;
			case FindPillarsTechnique.automaticBox:
				SearchForPillarsInBox(new List<ObscurePillar>());
				break;
			case FindPillarsTechnique.automaticBoxWithBlacklist:
				SearchForPillarsInBox(blacklist);
				break;
		}
		if (!overrideOnOffAngles && pillarsFound.Contains(ObscurePillar.activePillar)) {
			RecalculateOnOffPositions();
		}
	}

	private void SearchForPillarsInSphere(List<ObscurePillar> blacklist) {
		foreach (var pillarMaybe in Physics.OverlapSphere(transform.position, pillarSearchRadius)) {
			ObscurePillar pillar = pillarMaybe.GetComponent<ObscurePillar>();
			if (pillar != null && !blacklist.Contains(pillar)) {
				pillarsFound.Add(pillar);
			}
		}
	}

	private void SearchForPillarsInBox(List<ObscurePillar> blacklist) {
		foreach (var pillarMaybe in Physics.OverlapBox(transform.position, pillarSearchBoxSize/2f, new Quaternion())) {
			ObscurePillar pillar = pillarMaybe.GetComponent<ObscurePillar>();
			if (pillar != null && !blacklist.Contains(pillar)) {
				pillarsFound.Add(pillar);
			}
		}
	}

	private void HandlePlayerMoveAroundPillar(Angle prevAngle, Angle newAngle) {
		if (prevAngle == newAngle || ObscurePillar.activePillar == null || !pillarsFound.Contains(ObscurePillar.activePillar)) {
			return;
		}
		Angle t = (newAngle - prevAngle).Normalize();
		MovementDirection direction = t > Angle.Radians(Mathf.PI) ? MovementDirection.clockwise : MovementDirection.counterclockwise;

		switch (direction) {
			case MovementDirection.clockwise:
				if (Angle.IsAngleBetween(onAngle, newAngle, prevAngle)) {
					HitBySweepingCollider(direction);
					print(gameObject.name + ":\tHitBySweepingColliderClockwise");
				}
				if (Angle.IsAngleBetween(offAngle, newAngle, prevAngle)) {
					SweepingColliderExit(direction);
					print(gameObject.name + ":\tSweepingColliderExitClockwise");
				}
				break;
			case MovementDirection.counterclockwise:
				if (Angle.IsAngleBetween(offAngle, prevAngle, newAngle)) {
					HitBySweepingCollider(direction);
					print(gameObject.name + ":\tHitBySweepingColliderCounterclockwise");
				}
				if (Angle.IsAngleBetween(onAngle, prevAngle, newAngle)) {
					SweepingColliderExit(direction);
					print(gameObject.name + ":\tSweepingColliderExitCounterclockwise");
				}
				break;
		}
	}

	private void RecalculateOnOffPositions() {
		RecalculateOnOffPositions(null);
	}

	private void RecalculateOnOffPositions(ObscurePillar unused) {
		if (ObscurePillar.activePillar == null) {
			onAngle = null; offAngle = null;
			return;
		}

		Vector3[] bounds = new Vector3[] { renderer.GetRendererBounds().min, renderer.GetRendererBounds().max };
		Vector3[] corners = new Vector3[] {
			new Vector3(bounds[0].x, bounds[0].y, bounds[0].z),
			new Vector3(bounds[0].x, bounds[0].y, bounds[1].z),
			new Vector3(bounds[0].x, bounds[1].y, bounds[0].z),
			new Vector3(bounds[0].x, bounds[1].y, bounds[1].z),
			new Vector3(bounds[1].x, bounds[0].y, bounds[0].z),
			new Vector3(bounds[1].x, bounds[0].y, bounds[1].z),
			new Vector3(bounds[1].x, bounds[1].y, bounds[0].z),
			new Vector3(bounds[1].x, bounds[1].y, bounds[1].z)
		};

		Vector3 centerOfObject = renderer.GetRendererBounds().center;
		Angle baseAngle = PolarCoordinate.CartesianToPolar(centerOfObject - ObscurePillar.activePillar.transform.position).angle;
		onAngle = Angle.Radians(baseAngle.radians + .001f);
		offAngle = Angle.Radians(baseAngle.radians);
		foreach (var corner in corners) {
			PolarCoordinate cornerPolar = PolarCoordinate.CartesianToPolar(corner - ObscurePillar.activePillar.transform.position);
			if (!Angle.IsAngleBetween(cornerPolar.angle, offAngle, onAngle)) {
				Angle replaceOn = Angle.AngleBetween(cornerPolar.angle, offAngle);
				Angle replaceOff = Angle.AngleBetween(cornerPolar.angle, onAngle);
				if (replaceOn.radians > replaceOff.radians) {
					onAngle = cornerPolar.angle;
				}
				else {
					offAngle = cornerPolar.angle;
				}
			}
		}
		onAngle.Reverse();
		offAngle.Reverse();
		//print(onAngle + "\n" + offAngle);
	}

	/// <summary>
	/// Handles the VisibilityState switching logic when a sweeping collider hits this object
	/// </summary>
	/// <param name="direction">Direction the sweeping collider is moving</param>
	/// <returns>true if VisibilityState changes, false otherwise</returns>
	public bool HitBySweepingCollider(MovementDirection direction) {
		switch (direction) {
			case MovementDirection.clockwise:
				if (visibilityState == startingVisibilityState) {
					print("Enter Clockwise, setting visibility state to PartiallyVisible from " + startingVisibilityState);
					SetVisibilityState(VisibilityState.partiallyVisible);
					return true;
				}
				break;
			case MovementDirection.counterclockwise:
				if (visibilityState == oppositeStartingVisibilityState) {
					print("Enter Counterclockwise, setting visibility state to PartiallyVisible from " + oppositeStartingVisibilityState);
					SetVisibilityState(VisibilityState.partiallyVisible);
					return true;
				}
				break;
		}
		return false;
	}

	/// <summary>
	/// Handles the VisibilityState switching logic when a sweeping collider exits this object
	/// </summary>
	/// <param name="direction">Direction the sweeping collider is moving</param>
	/// <returns>true if VisibilityState changes, false otherwise</returns>
	public bool SweepingColliderExit(MovementDirection direction) {
		if (visibilityState == VisibilityState.partiallyVisible) {
			switch (direction) {
				case MovementDirection.clockwise:
					print("Exit Clockwise, setting visibility state to " + oppositeStartingVisibilityState + " from PartiallyVisible");
					SetVisibilityState(oppositeStartingVisibilityState);
					return true;
				case MovementDirection.counterclockwise:
					print("Exit Counterclockwise, setting visibility state to " + startingVisibilityState + " from PartiallyVisible");
					SetVisibilityState(startingVisibilityState);
					return true;
			}
		}
		return false;
	}

	private void ResetVisibilityStateIfNeeded(ObscurePillar unused) {
		if (visibilityState == VisibilityState.partiallyVisible) {
			ResetVisibilityState();
		}
	}

	public void ResetVisibilityState() {
		SetVisibilityState(startingVisibilityState);
	}

	public void SetVisibilityState(VisibilityState newState) {
		_visibilityState = newState;
		UpdateVisibilitySettings();

		if (OnVisibilityStateChange != null) {
			OnVisibilityStateChange(newState);
		}
	}

	private void UpdateVisibilitySettings() {
		switch (visibilityState) {
			case VisibilityState.invisible:
				SetLayer(invisibleLayer);
				break;
			case VisibilityState.partiallyVisible:
				SetLayer(initialLayer);
				renderer.SetMaterial(initialMaterial);
				break;
			case VisibilityState.visible:
				SetLayer(initialLayer);
				renderer.SetMaterial(visibleMaterial);
				break;
		}
	}

	private void SetLayer(int layer) {
		if (setLayerRecursively) {
			Utils.SetLayerRecursively(gameObject, layer);
		}
		else {
			gameObject.layer = layer;
		}
	}

}

#if UNITY_EDITOR

[CustomEditor(typeof(PartiallyVisibleObject))]
[CanEditMultipleObjects]
public class PartiallyVisibleObjectEditor : Editor {
	public override void OnInspectorGUI() {
		PartiallyVisibleObject script = target as PartiallyVisibleObject;
		float defaultWidth = EditorGUIUtility.labelWidth;

		EditorGUILayout.Space();

		EditorGUI.BeginChangeCheck();
		script.findPillarsTechnique = (PartiallyVisibleObject.FindPillarsTechnique)EditorGUILayout.EnumPopup("Technique for finding pillars", script.findPillarsTechnique);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PartiallyVisibleObject)obj).findPillarsTechnique = script.findPillarsTechnique;
			}
		}

		EditorGUILayout.Space();

		switch (script.findPillarsTechnique) {
			case PartiallyVisibleObject.FindPillarsTechnique.whitelist:
				SerializedProperty whitelist = serializedObject.FindProperty("whitelist");
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(whitelist, new GUIContent("Whitelist: "), true);
				if (EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();
				break;
			case PartiallyVisibleObject.FindPillarsTechnique.automaticSphere:
				UpdateSearchRadiusForAll(script);
				break;
			case PartiallyVisibleObject.FindPillarsTechnique.automaticSphereWithBlacklist: {
				UpdateSearchRadiusForAll(script);
				SerializedProperty blacklist = serializedObject.FindProperty("blacklist");
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(blacklist, new GUIContent("Blacklist: "), true);
				if (EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();
				}
				break;
			case PartiallyVisibleObject.FindPillarsTechnique.automaticBox:
				UpdateSearchBoxSizeForAll(script);
				break;
			case PartiallyVisibleObject.FindPillarsTechnique.automaticBoxWithBlacklist: {
				UpdateSearchBoxSizeForAll(script);
				SerializedProperty blacklist = serializedObject.FindProperty("blacklist");
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(blacklist, new GUIContent("Blacklist: "), true);
				if (EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();
				}
				break;
		}

		EditorGUILayout.Space();

		EditorGUI.BeginChangeCheck();
		script.setLayerRecursively = EditorGUILayout.Toggle("Set layer recursively? ", script.setLayerRecursively);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PartiallyVisibleObject)obj).setLayerRecursively = script.setLayerRecursively;
			}
		}

		EditorGUILayout.Space();

		EditorGUI.BeginChangeCheck();
		script.setMaterialColorOnStart = EditorGUILayout.Toggle("Set material color on start? ", script.setMaterialColorOnStart);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PartiallyVisibleObject)obj).setMaterialColorOnStart = script.setMaterialColorOnStart;
			}
		}
		if (script.setMaterialColorOnStart) {
			script.materialColor = EditorGUILayout.ColorField("Material color: ", script.materialColor);
		}

		EditorGUILayout.Space();

		EditorGUI.BeginChangeCheck();
		script.startingVisibilityState = (VisibilityState)EditorGUILayout.EnumPopup("Starting visibility state: ", script.startingVisibilityState);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PartiallyVisibleObject)obj).startingVisibilityState = script.startingVisibilityState;
			}
		}
		string s = script.visibilityState.ToString();
		EditorGUILayout.LabelField("Current visibility state: ", char.ToUpper(s[0]) + s.Substring(1));

		EditorGUILayout.Space();

		EditorGUI.BeginChangeCheck();
		script.overrideOnOffAngles = EditorGUILayout.Toggle("Override On/Off Angles?", script.overrideOnOffAngles);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PartiallyVisibleObject)obj).overrideOnOffAngles = script.overrideOnOffAngles;
			}
		}

		if (script.overrideOnOffAngles) {
			script.onAngle = Angle.Degrees(EditorGUILayout.FloatField("On Angle Degrees: ", script.onAngle.degrees));
			script.offAngle = Angle.Degrees(EditorGUILayout.FloatField("Off Angle Degrees: ", script.offAngle.degrees));
		}
		else {
			EditorGUILayout.LabelField("On Angle: ", script.onAngle.ToString());
			EditorGUILayout.LabelField("Off Angle: ", script.offAngle.ToString());
		}
	}

	private void UpdateSearchRadiusForAll(PartiallyVisibleObject script) {
		EditorGUI.BeginChangeCheck();
		script.pillarSearchRadius = EditorGUILayout.FloatField("Search radius: ", script.pillarSearchRadius);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PartiallyVisibleObject)obj).pillarSearchRadius = script.pillarSearchRadius;
			}
		}
	}

	private void UpdateSearchBoxSizeForAll(PartiallyVisibleObject script) {
		EditorGUI.BeginChangeCheck();
		script.pillarSearchBoxSize = EditorGUILayout.Vector3Field("Search box size: ", script.pillarSearchBoxSize);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PartiallyVisibleObject)obj).pillarSearchBoxSize = script.pillarSearchBoxSize;
			}
		}
	}
}

#endif
