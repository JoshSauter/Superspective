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
	public static bool DEBUG = false;
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

    /////////////////////
    // Toggle Settings //
    /////////////////////
    public bool setLayerRecursively = true;
    public bool setMaterialColorOnStart = true;
    public bool swapRevealDirection = false;

    ///////////////////////////////////
    // Initial values and references //
    ///////////////////////////////////
    public VisibilityState startingVisibilityState;     // invisible, partiallyVisible, or visible
    VisibilityState oppositeStartingVisibilityState;
    public Color materialColor = Color.black;           // Only used if setMaterialColorOnStart is true
    [ColorUsageAttribute(true, true)]
    public Color emissiveColor = Color.black;            // Only used if setMaterialColorOnStart is true, emissive color
    const string emissiveColorKey = "_EmissionColor";
    int initialLayer;                                   // Set automatically from the gameObject's layer
	int invisibleLayer;                                 // Set automatically from LayerMask.NameToLayer("Invisible")
	public Material visibleMaterial;                    // Defaults to Materials/Unlit/Unlit, can be set in inspector
    public Material partiallyVisibleMaterial;           // Needs to be set in inspector
	// Material initialMaterial;
	EpitaphRenderer renderer;                           // EpitaphRenderer used to easily swap materials and colors

	//////////////////////
	// Visibility state //
	//////////////////////
	public VisibilityState visibilityState { get; private set; }

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

        if (partiallyVisibleMaterial == null) {
            Debug.LogError("PartiallyVisibleMaterial for object: " + gameObject.name + " is not set.");
            enabled = false;
        }
        if (visibleMaterial == null) {
            visibleMaterial = Resources.Load<Material>("Materials/Unlit/Unlit");
        }
	}

	void OnEnable() {
		initialLayer = gameObject.layer;
		renderer = gameObject.GetComponent<EpitaphRenderer>();
		if (renderer == null) {
			renderer = gameObject.AddComponent<EpitaphRenderer>();
		}
		oppositeStartingVisibilityState = startingVisibilityState == VisibilityState.visible ? VisibilityState.invisible : VisibilityState.visible;

        if (!setMaterialColorOnStart) {
            Material startingMaterial = renderer.GetMaterial();
            materialColor = startingMaterial.GetColor(EpitaphRenderer.mainColor);
            emissiveColor = startingMaterial.GetColor(emissiveColorKey);
        }

		ObscurePillar.OnPlayerMoveAroundPillar += HandlePlayerMoveAroundPillar;
		ObscurePillar.OnActivePillarChanged += HandlePillarChanged;
		ObscurePillar.OnActivePillarChanged += ResetVisibilityStateIfPartiallyVisible;
	}

	private void OnDisable() {
		ObscurePillar.OnPlayerMoveAroundPillar -= HandlePlayerMoveAroundPillar;
		ObscurePillar.OnActivePillarChanged -= HandlePillarChanged;
		ObscurePillar.OnActivePillarChanged -= ResetVisibilityStateIfPartiallyVisible;
	}

	private void Start() {
		SetVisibilityState(startingVisibilityState);
		if (setMaterialColorOnStart) {
            SetColors(materialColor, emissiveColor);
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
					if (DEBUG) {
						print(gameObject.name + ":\tHitBySweepingColliderClockwise");
					}
				}
				if (Angle.IsAngleBetween(offAngle, newAngle, prevAngle)) {
					SweepingColliderExit(direction);
					if (DEBUG) {
						print(gameObject.name + ":\tSweepingColliderExitClockwise");
					}
				}
				break;
			case MovementDirection.counterclockwise:
				if (Angle.IsAngleBetween(offAngle, prevAngle, newAngle)) {
					HitBySweepingCollider(direction);
					if (DEBUG) {
						print(gameObject.name + ":\tHitBySweepingColliderCounterclockwise");
					}
				}
				if (Angle.IsAngleBetween(onAngle, prevAngle, newAngle)) {
					SweepingColliderExit(direction);
					if (DEBUG) {
						print(gameObject.name + ":\tSweepingColliderExitCounterclockwise");
					}
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
	}

	/// <summary>
	/// Handles the VisibilityState switching logic when a sweeping collider (now just a metaphorical object) hits this object
	/// </summary>
	/// <param name="direction">Direction the sweeping collider is moving</param>
	/// <returns>true if VisibilityState changes, false otherwise</returns>
	public bool HitBySweepingCollider(MovementDirection direction) {
		if (swapRevealDirection) {
			direction = OppositeMovementDirection(direction);
		}
		switch (direction) {
			case MovementDirection.clockwise:
				if (visibilityState == startingVisibilityState) {
					if (DEBUG) {
						print("Enter Clockwise, setting visibility state to PartiallyVisible from " + startingVisibilityState);
					}
					SetVisibilityState(VisibilityState.partiallyVisible);
					return true;
				}
				break;
			case MovementDirection.counterclockwise:
				if (visibilityState == oppositeStartingVisibilityState) {
					if (DEBUG) {
						print("Enter Counterclockwise, setting visibility state to PartiallyVisible from " + oppositeStartingVisibilityState);
					}
					SetVisibilityState(VisibilityState.partiallyVisible);
					return true;
				}
				break;
		}
		return false;
	}

	/// <summary>
	/// Handles the VisibilityState switching logic when a sweeping collider (now just a metaphorical object) exits this object
	/// </summary>
	/// <param name="direction">Direction the sweeping collider is moving</param>
	/// <returns>true if VisibilityState changes, false otherwise</returns>
	public bool SweepingColliderExit(MovementDirection direction) {
		if (swapRevealDirection) {
			direction = OppositeMovementDirection(direction);
		}
		if (visibilityState == VisibilityState.partiallyVisible) {
			switch (direction) {
				case MovementDirection.clockwise:
					if (DEBUG) {
						print("Exit Clockwise, setting visibility state to " + oppositeStartingVisibilityState + " from PartiallyVisible");
					}
					SetVisibilityState(oppositeStartingVisibilityState);
					return true;
				case MovementDirection.counterclockwise:
					if (DEBUG) {
						print("Exit Counterclockwise, setting visibility state to " + startingVisibilityState + " from PartiallyVisible");
					}
					SetVisibilityState(startingVisibilityState);
					return true;
			}
		}
		return false;
	}

	private MovementDirection OppositeMovementDirection(MovementDirection m) {
		if (m == MovementDirection.clockwise) return MovementDirection.counterclockwise;
		else return MovementDirection.clockwise;
	}

	private void ResetVisibilityStateIfPartiallyVisible(ObscurePillar unused) {
		if (visibilityState == VisibilityState.partiallyVisible) {
			ResetVisibilityState();
		}
	}

	public void ResetVisibilityState() {
		SetVisibilityState(startingVisibilityState);
	}

	public void SetVisibilityState(VisibilityState newState) {
		visibilityState = newState;
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
				renderer.SetMaterial(partiallyVisibleMaterial);
                SetColors(materialColor, emissiveColor);
				break;
			case VisibilityState.visible:
				SetLayer(initialLayer);
				renderer.SetMaterial(visibleMaterial);
                SetColors(materialColor, emissiveColor);
                break;
		}
	}

    private void SetColors(Color mainColor, Color emissiveColor) {
        renderer.SetMainColor(materialColor);
        renderer.SetColor(emissiveColorKey, emissiveColor);
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
			EditorGUI.BeginChangeCheck();
			script.materialColor = EditorGUILayout.ColorField("Material color: ", script.materialColor);
			if (EditorGUI.EndChangeCheck()) {
				foreach (Object obj in targets) {
					((PartiallyVisibleObject)obj).materialColor = script.materialColor;
				}
			}
            EditorGUI.BeginChangeCheck();
            script.emissiveColor = EditorGUILayout.ColorField("Material emission color: ", script.emissiveColor);
            if (EditorGUI.EndChangeCheck()) {
                foreach (Object obj in targets) {
                    ((PartiallyVisibleObject)obj).emissiveColor = script.emissiveColor;
                }
            }
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

        ///////////////
        // Materials //
        ///////////////
        EditorGUI.BeginChangeCheck();
        bool allowSceneObjects = !EditorUtility.IsPersistent(target);
        script.visibleMaterial = (Material)EditorGUILayout.ObjectField("Fully Visible Material: ", script.visibleMaterial, typeof(Material), allowSceneObjects);
        if (EditorGUI.EndChangeCheck()) {
            foreach (Object obj in targets) {
                ((PartiallyVisibleObject)obj).visibleMaterial = script.visibleMaterial;
            }
        }
        EditorGUI.BeginChangeCheck();
        script.partiallyVisibleMaterial = (Material)EditorGUILayout.ObjectField("Partially Visible Material: ", script.partiallyVisibleMaterial, typeof(Material), allowSceneObjects);
        if (EditorGUI.EndChangeCheck()) {
            foreach (Object obj in targets) {
                ((PartiallyVisibleObject)obj).partiallyVisibleMaterial = script.partiallyVisibleMaterial;
            }
        }

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
		script.swapRevealDirection = EditorGUILayout.Toggle("Reverse reveal direction? ", script.swapRevealDirection);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PartiallyVisibleObject)obj).swapRevealDirection = script.swapRevealDirection;
			}
		}

		EditorGUILayout.Space();

		EditorGUI.BeginChangeCheck();
		script.overrideOnOffAngles = EditorGUILayout.Toggle("Override On/Off Angles?", script.overrideOnOffAngles);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PartiallyVisibleObject)obj).overrideOnOffAngles = script.overrideOnOffAngles;
			}
		}

		if (script.overrideOnOffAngles) {

			EditorGUI.BeginChangeCheck();
			script.onAngle = Angle.Degrees(EditorGUILayout.FloatField("On Angle Degrees: ", script.onAngle.degrees));
			script.offAngle = Angle.Degrees(EditorGUILayout.FloatField("Off Angle Degrees: ", script.offAngle.degrees));
			if (EditorGUI.EndChangeCheck()) {
				foreach (Object obj in targets) {
					((PartiallyVisibleObject)obj).onAngle = Angle.Degrees(EditorGUILayout.FloatField("On Angle Degrees: ", script.onAngle.degrees));
					((PartiallyVisibleObject)obj).offAngle = Angle.Degrees(EditorGUILayout.FloatField("Off Angle Degrees: ", script.offAngle.degrees));
				}
			}
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
