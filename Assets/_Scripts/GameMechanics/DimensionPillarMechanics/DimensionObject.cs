using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using System.Linq;
using Saving;
using System;
using DimensionObjectMechanics;
using Sirenix.OdinInspector;
using SuperspectiveAttributes;

public enum VisibilityState {
	Invisible,
	PartiallyVisible,
	Visible,
	PartiallyInvisible,
}

public static class VisibilityStateExt {
	public static VisibilityState Opposite(this VisibilityState visibilityState) {
		switch (visibilityState) {
			case VisibilityState.Invisible:
				return VisibilityState.Visible;
			case VisibilityState.PartiallyVisible:
				return VisibilityState.PartiallyInvisible;
			case VisibilityState.Visible:
				return VisibilityState.Invisible;
			case VisibilityState.PartiallyInvisible:
				return VisibilityState.PartiallyVisible;
			default:
				throw new ArgumentOutOfRangeException(nameof(visibilityState), visibilityState, null);
		}
	}
}

[RequireComponent(typeof(UniqueId))]
public class DimensionObject : SuperspectiveObject<DimensionObject, DimensionObject.DimensionObjectSave> {
	protected static Color _GUI_RENDERING = new Color(0.35f, 0.75f, .9f);
	protected static Color _GUI_PHYSICS = new Color(0.95f, 0.55f, .55f);
	protected static Color _GUI_GENERAL = new Color(.65f, 1f, .65f);
	
	public const int NUM_CHANNELS = 8;
	private const string IGNORE_COLLISIONS_TRIGGER_ZONE_NAME = "IgnoreCollisionsTriggerZone";
	
#region events
	public delegate void DimensionObjectStateChangeAction(DimensionObject context);
	public delegate void DimensionObjectStateChangeActionSimple();

	// Immediate will fire immediately after any state change happens
	public event DimensionObjectStateChangeAction OnStateChangeImmediate;
	// Non-immediate events wait until end of frame to see if the net visibility state has changed
	public event DimensionObjectStateChangeAction OnStateChange;
	public event DimensionObjectStateChangeActionSimple OnStateChangeSimple;
#endregion
	
	public override string ID => $"{gameObject.name}_{base.ID}";

	[TabGroup("General"), GUIColor(nameof(_GUI_GENERAL))]
	public bool treatChildrenAsOneObjectRecursively = false;
	[ShowIf(nameof(treatChildrenAsOneObjectRecursively)), TabGroup("General"), GUIColor(nameof(_GUI_GENERAL))]
	public bool ignoreChildrenWithDimensionObject = true; // Only skips children with DimensionObject scripts that MATCH THE CHANNEL of this one
	[ShowIf(nameof(treatChildrenAsOneObjectRecursively)), TabGroup("General"), GUIColor(nameof(_GUI_GENERAL))]
	public bool includeInactiveGameObjects = false;

	[DoNotSave]
	protected bool initialized = false;
	
	// Channel logic config
	[TabGroup("General"), GUIColor(nameof(_GUI_GENERAL))]
	public bool useAdvancedChannelLogic = false;
	[Range(0, NUM_CHANNELS-1), TabGroup("General"), GUIColor(nameof(_GUI_GENERAL)), HideIf(nameof(useAdvancedChannelLogic))]
	public int channel;
	[TabGroup("General"), GUIColor(nameof(_GUI_GENERAL)), ShowIf(nameof(useAdvancedChannelLogic))]
	public string channelLogic = "";
	private DimensionObjectBitmask maskSolution;
	private DimensionObjectBitmask inverseMaskSolution;
	[TabGroup("General"), GUIColor(nameof(_GUI_GENERAL)), ShowInInspector]
	public DimensionObjectBitmask EffectiveMaskSolution => InverseShouldBeEnabled ? inverseMaskSolution : maskSolution;
	[TabGroup("General"), GUIColor(nameof(_GUI_GENERAL))]
	public bool reverseVisibilityStates = false;

	// If true, this object will not check the visibility mask when checking if a raycast hit this object
	[TabGroup("General"), GUIColor(nameof(_GUI_GENERAL))]
	public bool bypassRaycastCheck = false;

	[TabGroup("Rendering"), GUIColor(nameof(_GUI_RENDERING))]
	public bool automaticallySetRenderersIfEmpty = true;
	[TabGroup("Rendering"), GUIColor(nameof(_GUI_RENDERING))]
	public SuperspectiveRenderer[] renderers;
	[TabGroup("Rendering"), GUIColor(nameof(_GUI_RENDERING)), ShowIf(nameof(automaticallySetRenderersIfEmpty))]
	public SuperspectiveRenderer[] renderersToSkip;
	
	[TabGroup("Physics"), GUIColor(nameof(_GUI_PHYSICS))]
	public bool automaticallySetCollidersIfEmpty = true;
	[TabGroup("Physics"), GUIColor(nameof(_GUI_PHYSICS))]
	public Collider[] colliders;
	[TabGroup("Physics"), GUIColor(nameof(_GUI_PHYSICS)), ShowIf(nameof(automaticallySetRenderersIfEmpty))]
	public Collider[] collidersToSkip;
	[TabGroup("Physics"), GUIColor(nameof(_GUI_PHYSICS))]
	public SphereCollider ignoreCollisionsTriggerZone;

	private Color GUIVisibilityStateColor(VisibilityState forState) {
		switch (GetEffectiveVisibilityState(forState)) {
			case VisibilityState.Invisible:
				return new Color(0.95f, 0.55f, .55f);
			case VisibilityState.PartiallyVisible:
				return new Color(1, 1, 0.5f);
			case VisibilityState.Visible:
				return new Color(.65f, 1f, .65f);
			case VisibilityState.PartiallyInvisible:
				return new Color(0.95f, 0.75f, .45f);
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private Color StartingVisibilityStateColor => GUIVisibilityStateColor(startingVisibilityState);
	private Color VisibilityStateColor => GUIVisibilityStateColor(visibilityState);
	[BoxGroup("Visibility State"), GUIColor(nameof(StartingVisibilityStateColor))]
	public VisibilityState startingVisibilityState = VisibilityState.Visible;
	[BoxGroup("Visibility State"), GUIColor(nameof(VisibilityStateColor))]
	public VisibilityState visibilityState = VisibilityState.Visible;

	/// <summary>
	/// Returns true if the inverse mask should be applied for the shader
	/// </summary>
	private bool InverseShouldBeEnabled {
		get {
			switch (visibilityState) {
				case VisibilityState.Visible:
				case VisibilityState.Invisible:
					return false;
				case VisibilityState.PartiallyVisible:
					return reverseVisibilityStates;
				case VisibilityState.PartiallyInvisible:
					return !reverseVisibilityStates;
				default:
					throw new ArgumentOutOfRangeException();
					return false;
			}
		}
	}

	protected VisibilityState GetEffectiveVisibilityState(VisibilityState visibilityState) {
		return reverseVisibilityStates ? visibilityState.Opposite() : visibilityState;
	}
	public VisibilityState EffectiveVisibilityState {
		get => GetEffectiveVisibilityState(visibilityState);
		set => visibilityState = reverseVisibilityStates ? value.Opposite() : value;
	} 
	private static readonly Dictionary<VisibilityState, HashSet<VisibilityState>> nextStates = new Dictionary<VisibilityState, HashSet<VisibilityState>> {
		{ VisibilityState.Invisible, new HashSet<VisibilityState> { VisibilityState.PartiallyVisible, VisibilityState.PartiallyInvisible } },
		{ VisibilityState.PartiallyVisible, new HashSet<VisibilityState> { VisibilityState.Invisible, VisibilityState.Visible } },
		{ VisibilityState.Visible, new HashSet<VisibilityState> { VisibilityState.PartiallyVisible, VisibilityState.PartiallyInvisible } },
		{ VisibilityState.PartiallyInvisible, new HashSet<VisibilityState> { VisibilityState.Invisible, VisibilityState.Visible } }
	};

	[HideInInspector]
	public bool[] collisionMatrix = new bool[COLLISION_MATRIX_COLS * COLLISION_MATRIX_ROWS] {
		 true, false, false, false, false, false,
		false,  true, false, false, false, false,
		false, false,  true, false,  true,  true,
		false, false, false,  true,  true,  true
	};

	[TabGroup("Physics"), GUIColor(nameof(_GUI_PHYSICS))]
	public DimensionObjectCollisionMatrix collisionMatrixNew = new DimensionObjectCollisionMatrix();

	public const int COLLISION_MATRIX_ROWS = 4;
	// First 4 columns are VisibilityStates, 5 is Player and 6 is Other Non-DimensionObjects
	public const int COLLISION_MATRIX_COLS = 6;
	[TabGroup("Physics"), GUIColor(nameof(_GUI_PHYSICS))]
	public DimensionObjectCollisions collisionLogic;
	
	protected override void OnValidate() {
		base.OnValidate();
		if (collisionMatrix == null || collisionMatrix.Length != COLLISION_MATRIX_ROWS * COLLISION_MATRIX_COLS) {
			collisionMatrix = new bool[COLLISION_MATRIX_COLS * COLLISION_MATRIX_ROWS] {
				true, false, false, false, false, false,
				false, true, false, false, false, false,
				false, false, true, false, true, true,
				false, false, false, true, true, true
			};
		}
	}

	protected override void Init() {
		base.Init();
		
		SetupDimensionCollisionLogic();
		InitializeMaskSolution();

		if (!initialized && gameObject.activeInHierarchy) {
			StartCoroutine(Initialize());
		}
	}

	IEnumerator Initialize() {
		yield return new WaitUntil(() => gameObject.IsInLoadedScene());
		yield return null;
		
		SwitchVisibilityState(startingVisibilityState, true);
		initialized = true;
	}

	protected override void OnEnable() {
		base.OnEnable();
		InitializeRenderersAndColliders();
		
		InitializeMaskSolution();
	}

	protected override void OnDisable() {
		base.OnDisable();
		collisionLogic?.Destroy();
		UninitializeRenderersAndColliders();
		
		visibilityState = VisibilityState.Visible;
		OnStateChangeImmediate?.Invoke(this);
		OnStateChange?.Invoke(this);
		OnStateChangeSimple?.Invoke();
	}
	
	public bool IsVisibleFrom(Camera cam) {
		return renderers.Any(r => r.r.IsVisibleFrom(cam));
	}

	///////////////////
	// Physics Logic //
	///////////////////
#region Physics
	void SetupDimensionCollisionLogic() {
		if (colliders != null && colliders.Length > 0) {
			if (ignoreCollisionsTriggerZone != null) {
				Debug.LogError(ignoreCollisionsTriggerZone.FullPath());
			}
			ignoreCollisionsTriggerZone = CreateTriggerZone();
		}
	}
	
	public SphereCollider CreateTriggerZone() {
		Vector3 MinOfTwoVectors(Vector3 a, Vector3 b) {
			return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z));
		}

		Vector3 MaxOfTwoVectors(Vector3 a, Vector3 b) {
			return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));
		}
		
		Vector3 min = float.MaxValue * Vector3.one;
		Vector3 max = float.MinValue * Vector3.one;
		foreach (var c in colliders) {
			bool wasEnabled = c.enabled;
			c.enabled = true; // Bounds.min/max are zero if the collider is disabled :(
			Vector3 thisMin = c.bounds.min;
			Vector3 thisMax = c.bounds.max;
			c.enabled = wasEnabled;

			min = MinOfTwoVectors(min, thisMin);
			max = MaxOfTwoVectors(max, thisMax);
		}

		Vector3 size = (max - min);
		size.x /= transform.lossyScale.x;
		size.y /= transform.lossyScale.y;
		size.z /= transform.lossyScale.z;
		Vector3 center = (max + min) / 2f;

		GameObject triggerGO = new GameObject(IGNORE_COLLISIONS_TRIGGER_ZONE_NAME) {
			layer = SuperspectivePhysics.IgnoreRaycastLayer
		};
		triggerGO.transform.SetParent(transform, false);
		SphereCollider trigger = triggerGO.AddComponent<SphereCollider>();
		trigger.radius = Mathf.Max(size.x, size.y, size.z);
		trigger.center = transform.InverseTransformPoint(center);
		trigger.isTrigger = true;
		collisionLogic = triggerGO.AddComponent<DimensionObjectCollisions>();
		collisionLogic.id = id;
		collisionLogic.dimensionObject = this;

		return trigger;
	}

	public bool HasChannelOverlapWith(DimensionObject other) {
		// If we're dealing with advanced channel logic, compare the resulting bitmasks
		if (useAdvancedChannelLogic || other.useAdvancedChannelLogic) {
			return HasMaskOverlapWith(other);
		}
		// Otherwise, just compare the channels directly
		else {
			return channel == other.channel;
		}
	}
	
	private bool HasMaskOverlapWith(DimensionObject other) {
		bool MasksHaveOverlap(DimensionObjectBitmask maskA, DimensionObjectBitmask maskB) {
			return !(maskA & maskB).IsEmpty;
		}
		
		void CheckMaskIsSet(DimensionObject dimObj) {
			if (!dimObj.EffectiveMaskSolution.HasBitmaskSet) {
				Debug.LogWarning("Mask render solution is null for " + dimObj.gameObject.name, dimObj.gameObject);
				dimObj.ValidateAndApplyChannelLogic();
				if (!dimObj.EffectiveMaskSolution.HasBitmaskSet) {
					Debug.LogError("Mask render solution is still null for " + dimObj.gameObject.name, dimObj.gameObject);
				}
			}
		}
		
		CheckMaskIsSet(this);
		CheckMaskIsSet(other);

		return MasksHaveOverlap(EffectiveMaskSolution, other.EffectiveMaskSolution);
	}

	/// <summary>
	/// Checks if this object should collide with the other non-dimension object/Player
	/// </summary>
	/// <param name="other">Non-DimensionObject Collider or Player Collider</param>
	/// <returns>True if this DimensionObject should collide with the provided Collider</returns>
	public bool ShouldCollideWithNonDimensionCollider(Collider other) {
		if (other.TaggedAsPlayer()) {
			return ShouldCollideWithPlayer();
		}
		
		return ShouldCollideWithNonDimensionObjects();
	}

	public virtual bool ShouldCollideWithNonDimensionObjects() {
		return collisionMatrixNew.ShouldCollideWithNonDimensionObjects(EffectiveVisibilityState);
		return collisionMatrix[(int)visibilityState * COLLISION_MATRIX_COLS + COLLISION_MATRIX_COLS - 1];
	}

	public virtual bool ShouldCollideWithPlayer() {
		return collisionMatrixNew.ShouldCollideWithPlayer(EffectiveVisibilityState);
		return collisionMatrix[(int)visibilityState * COLLISION_MATRIX_COLS + COLLISION_MATRIX_COLS - 2];
	}
	
	public virtual bool ShouldCollideWithDimensionObject(DimensionObject other) {
		bool ShouldCollideInSameChannel() {
			bool thisCollidesWithOther = collisionMatrixNew.ShouldCollide(EffectiveVisibilityState, other.EffectiveVisibilityState);
			bool otherCollidesWithThis = other.collisionMatrixNew.ShouldCollide(other.EffectiveVisibilityState, EffectiveVisibilityState);
			return thisCollidesWithOther && otherCollidesWithThis;
			
			int thisVisibility = (int)EffectiveVisibilityState;
			int otherVisibility = (int)other.EffectiveVisibilityState;
			return collisionMatrix[thisVisibility * COLLISION_MATRIX_COLS + otherVisibility] && other.collisionMatrix[otherVisibility * COLLISION_MATRIX_COLS + thisVisibility];
		}
		
		bool bothFullyVisible = EffectiveVisibilityState == VisibilityState.Visible && other.EffectiveVisibilityState == VisibilityState.Visible;
		
		return (HasMaskOverlapWith(other) || bothFullyVisible) && ShouldCollideInSameChannel();
	}

	public void SetCollision(VisibilityState thisVisibility, VisibilityState otherVisibility, bool shouldCollide) {
		collisionMatrixNew.SetCollision(thisVisibility, otherVisibility, shouldCollide);
		collisionMatrix[(int)thisVisibility * COLLISION_MATRIX_COLS + (int)otherVisibility] = shouldCollide;
		collisionMatrix[(int)otherVisibility * COLLISION_MATRIX_COLS + (int)thisVisibility] = shouldCollide;
	}

	public void SetCollisionForNonDimensionObject(VisibilityState thisVisibility, bool shouldCollide) {
		collisionMatrix[(int)thisVisibility * COLLISION_MATRIX_COLS + COLLISION_MATRIX_COLS - 1] = shouldCollide;
	}

	public void SetCollisionForPlayer(VisibilityState thisVisibility, bool shouldCollide) {
		collisionMatrix[(int)thisVisibility * COLLISION_MATRIX_COLS + COLLISION_MATRIX_COLS - 2] = shouldCollide;
	}
#endregion

	////////////////////////
	// State Change Logic //
	////////////////////////
#region State Change
	public void SwitchEffectiveVisibilityState(VisibilityState nextState, bool ignoreTransitionRules = false, bool sendEvents = true, bool suppressLogs = false) {
		SwitchVisibilityState(reverseVisibilityStates ? nextState.Opposite() : nextState, ignoreTransitionRules, sendEvents, suppressLogs);
	}

	public void SwitchVisibilityState(VisibilityState nextState, bool ignoreTransitionRules = false, bool sendEvents = true, bool suppressLogs = false) {
		if (!(ignoreTransitionRules || IsValidNextState(nextState))) return;

		if (!suppressLogs) {
			debug.Log("State transition: " + visibilityState + " --> " + nextState);
		}

		visibilityState = nextState;
		
		DimensionObjectManager.instance.RefreshDimensionObject(this);

		if (sendEvents) {
			OnStateChangeImmediate?.Invoke(this);
		
			if (nextState == visibilityState) {
				OnStateChangeSimple?.Invoke();
				OnStateChange?.Invoke(this);
			}
		}
	}

	bool IsValidNextState(VisibilityState nextState) {
		return nextStates[visibilityState].Contains(nextState);
	}
	#endregion
	
	///////////////////////////
	// Material Change Logic //
	///////////////////////////
#region Materials
	public void InitializeRenderersAndColliders(bool force = false) {
		// Unregister existing renderers and colliders if we're forcing a re-initialization
		if (force) {
			UninitializeRenderersAndColliders();
		}
		
		// Get all renderers if we haven't already, or if we're forcing a re-initialization
		if (force || (automaticallySetRenderersIfEmpty && (renderers == null || renderers.Length == 0))) {
			renderers = GetAllSuperspectiveRenderers().ToArray();
		}
		foreach (SuperspectiveRenderer sRenderer in renderers) {
			DimensionObjectManager.RegisterRenderer(sRenderer, this);
		}
		
		// Get all colliders if we haven't already, or if we're forcing a re-initialization
		if (force || (automaticallySetCollidersIfEmpty && (colliders == null || colliders.Length == 0))) {
			colliders = GetAllColliders().ToArray();
		}
		foreach (Collider collider in colliders) {
			DimensionObjectManager.RegisterCollider(collider, this);
		}

		if (renderers.Length == 0) {
			debug.LogError("No renderers found for: " + gameObject.name);
		}
	}

	public void UninitializeRenderersAndColliders() {
		if (renderers != null) {
			foreach (SuperspectiveRenderer sRenderer in renderers) {
				DimensionObjectManager.UnregisterRenderer(sRenderer, this);
			}
		}
		if (colliders != null) {
			foreach (Collider collider in colliders) {
				DimensionObjectManager.UnregisterCollider(collider, this);
			}
		}
	}

	protected List<Collider> GetAllColliders() {
		List<Collider> result = new List<Collider>();

		void GetCollidersRecursively(Transform parent) {
			// Don't set renderers for inactive objects unless we really want to
			if (!includeInactiveGameObjects && !parent.gameObject.activeInHierarchy) return;
			
			// Children who have DimensionObject scripts are treated on only by their own settings
			if (parent != transform && ignoreChildrenWithDimensionObject && parent.TryGetComponent(out DimensionObject dimObj) && HasMaskOverlapWith(dimObj)) return;

			Collider[] collidersOnThisTransform = parent.GetComponents<Collider>();
			if (collidersToSkip != null && collidersOnThisTransform.Any(collidersToSkip.Contains)) return;

			result.AddRange(collidersOnThisTransform);

			foreach (Transform child in parent) {
				GetCollidersRecursively(child);
			}
		}

		if (!treatChildrenAsOneObjectRecursively) {
			result.AddRange(transform.GetComponents<Collider>());
		}
		else {
			GetCollidersRecursively(transform);
		}

		return result;
	}

	protected List<SuperspectiveRenderer> GetAllSuperspectiveRenderers() {
		List<SuperspectiveRenderer> allRenderers = new List<SuperspectiveRenderer>();
		if (!treatChildrenAsOneObjectRecursively) {
			SuperspectiveRenderer thisRenderer = GetComponent<SuperspectiveRenderer>();
			if (thisRenderer == null && GetComponent<Renderer>() != null) {
				thisRenderer = gameObject.AddComponent<SuperspectiveRenderer>();
			}
			if (thisRenderer != null && !renderersToSkip.Contains(thisRenderer)) {
				allRenderers.Add(thisRenderer);
			}
		}
		else {
			SetSuperspectiveRenderersRecursively(transform, ref allRenderers);
		}
		return allRenderers;
	}

	void SetSuperspectiveRenderersRecursively(Transform parent, ref List<SuperspectiveRenderer> renderersSoFar) {
		// Don't set renderers for inactive objects unless we really want to
		if (!includeInactiveGameObjects && !parent.gameObject.activeInHierarchy) return;
		
		// Children who have DimensionObject scripts are treated on only by their own settings
		if (parent != transform && ignoreChildrenWithDimensionObject && parent.TryGetComponent(out DimensionObject dimObj) && HasMaskOverlapWith(dimObj)) return;

		SuperspectiveRenderer thisRenderer = parent.GetComponent<SuperspectiveRenderer>();
		if (thisRenderer == null && parent.GetComponent<Renderer>() != null) {
			thisRenderer = parent.gameObject.AddComponent<SuperspectiveRenderer>();
		}

		if (renderersToSkip != null && renderersToSkip.Contains(thisRenderer)) return;

		if (thisRenderer != null) {
			renderersSoFar.Add(thisRenderer);
		}

		if (parent.childCount > 0) {
			foreach (Transform child in parent) {
				SetSuperspectiveRenderersRecursively(child, ref renderersSoFar);
			}
		}
	}

	Dictionary<SuperspectiveRenderer, int> GetAllStartingLayers(SuperspectiveRenderer[] renderers) {
		return renderers.ToDictionary(r => r, r => r.gameObject.layer);
	}
	#endregion
	
	///////////////////
	// Channel Logic //
	///////////////////
#region Channel Logic
	
	void InitializeMaskSolution() {
		if (!EffectiveMaskSolution.HasBitmaskSet) {
			ValidateAndApplyChannelLogic();
		}
	}

	class BooleanExpressionStream {
		readonly string s;
		int pos = 0;
		string lastValidSymbol = "";
		
		public BooleanExpressionStream(string input) {
			s = input;
		}

		// Returns the next character, if it exists, else '$' to symbol end of string
		char NextChar() {
			return pos < s.Length ? s[pos++] : '$';
		}

		char PeekNextChar() {
			return pos < s.Length ? s[pos] : '$';
		}

		bool IsEndOfString(char c) {
			return c == '$';
		}

		/// <summary>
		/// Returns the next valid boolean expression symbol, or, if there is none left, an empty string
		/// </summary>
		/// <returns>The next valid boolean expression (number, or part of validNonNumericSymbols), or empty string if there are none left</returns>
		/// <exception cref="Exception">Throws an exception if an invalid symbol is found</exception>
		public string Next() {
			char next = NextChar();
			string nextValidSymbol = "";

			while (char.IsWhiteSpace(next)) {
				next = NextChar();
			}

			// Popping next character when we've already hit end of string returns an empty string
			if (IsEndOfString(next)) {
				return "";
			}
			
			if (char.IsDigit(next)) {
				// Don't allow two digits in a row (NUM_CHANNELS is < 10 so only single-digit numbers are allowed)
				if (int.TryParse(lastValidSymbol, out int _)) {
					throw new Exception($"Found two digits in a row: {lastValidSymbol}, {next}");
				}
				nextValidSymbol = next.ToString();
			}
			else if (next == '|' || next == '&') {
				// || and && are valid, but | or & is not
				if (PeekNextChar() == next) {
					char secondChar = NextChar();
					nextValidSymbol = string.Concat(next, secondChar);
				}
				else {
					throw new Exception($"Invalid symbol {next} found at pos {pos}.");
				}
			}
			else if (next == '(' || next == ')') {
				nextValidSymbol = next.ToString();
			}
			else {
				throw new Exception($"Invalid symbol {next} found at pos {pos}.");
			}

			lastValidSymbol = nextValidSymbol;
			return lastValidSymbol;
		}
	}
	
	[NaughtyAttributes.Button("Apply boolean expression")]
	public void ValidateAndApplyChannelLogic() {
		if (useAdvancedChannelLogic) {
			ValidateAndApplyAdvancedChannelLogic();
		}
		else {
			maskSolution = new DimensionObjectBitmask(channel);
			inverseMaskSolution = ~maskSolution;
		}
	}

	private void ValidateAndApplyAdvancedChannelLogic() {
		BooleanExpressionStream validationStream = new BooleanExpressionStream(channelLogic);
		List<string> booleanExpressionSymbols = new List<string>();
		List<string> postfixBooleanExpression = new List<string>();
		try {
			string nextSymbol = validationStream.Next();
			while (nextSymbol != "") {
				booleanExpressionSymbols.Add(nextSymbol);
				nextSymbol = validationStream.Next();
			}

			postfixBooleanExpression = InfixToPostfix(booleanExpressionSymbols);
		}
		catch (Exception e) {
			Debug.LogError($"Invalid boolean expression string: {e}");
		}

		ApplyBooleanExpression(postfixBooleanExpression);
	}

	void ApplyBooleanExpression(List<string> postfixExpression) {
		maskSolution = new DimensionObjectBitmask(SolutionArrayFromPostfixExpression(postfixExpression));
		inverseMaskSolution = ~maskSolution;
		
		debug.Log($"Boolean channel expression applied to {gameObject.name}.\n{maskSolution.DebugPrettyPrint()}");
	}

	public static List<string> InfixToPostfix(List<string> infix) {
		List<string> postfix = new List<string>();
		Stack<string> symbolStack = new Stack<string>();
		
		foreach (var symbol in infix) {
			// Channel
			if (IsOperand(symbol)) {
				postfix.Add(symbol);
			}
			// Operator
			else if (IsOperator(symbol)) {
				while (symbolStack.Count > 0 && IsOperator(symbolStack.Peek())) {
					postfix.Add(symbolStack.Pop());
				}

				symbolStack.Push(symbol);
			}
			// Left parenthesis
			else if (symbol == "(") {
				symbolStack.Push(symbol);
			}
			// Right parenthesis
			else if (symbol == ")") {
				while (symbolStack.Count > 0 && symbolStack.Peek() != "(") {
					postfix.Add(symbolStack.Pop());
				}

				if (symbolStack.Peek() == "(") {
					symbolStack.Pop();
				}
			}
		}

		while (symbolStack.Count > 0) {
			postfix.Add(symbolStack.Pop());
		}

		return postfix;
	}
	
	static bool ChannelIsOn(int maskValue, int channel) => (maskValue & (1 << channel)) == (1 << channel);
	static bool IsOperator(string symbol) => symbol == "&&" || symbol == "||";
	static bool IsOperand(string symbol) => int.TryParse(symbol, out int _);

	/// <summary>
	/// Takes in a boolean expression in postfix form, and returns an array of size NUM_CHANNELS^2, where
	/// each entry is a 1 if the mask value would pass the boolean expression, and 0 otherwise
	/// </summary>
	/// <param name="postfixExpression">Boolean expression in postfix form</param>
	/// <returns>Int array of size 2^NUM_CHANNELS with 1 or 0 in each cell</returns>
	public static int[] SolutionArrayFromPostfixExpression(List<string> postfixExpression) {
		int[] solution = new int[1 << NUM_CHANNELS];
		// Run the boolean expression for every possible mask value
		for (int i = 0; i < solution.Length; i++) {
			Stack<bool> evaluationStack = new Stack<bool>();

			foreach (var symbol in postfixExpression) {
				if (IsOperand(symbol)) {
					evaluationStack.Push(ChannelIsOn(i, int.Parse(symbol)));
				}
				else if (IsOperator(symbol)) {
					try {
						bool eval1 = evaluationStack.Pop();
						bool eval2 = evaluationStack.Pop();

						switch (symbol) {
							case "&&":
								evaluationStack.Push(eval1 && eval2);
								break;
							case "||":
								evaluationStack.Push(eval1 || eval2);
								break;
							default:
								throw new Exception($"Unrecognized operator: {symbol}");
						}
					}
					catch (Exception) {
						throw new Exception($"Couldn't pop two operands from the stack for operator {symbol}");
					}
				}
				else {
					throw new Exception($"Unrecognized symbol: {symbol}");
				}
			}

			solution[i] = evaluationStack.Pop() ? 1 : 0;
		}

		return solution;
	}
#endregion

#region Saving

	public override void LoadSave(DimensionObjectSave save) {
		startingVisibilityState = save.startingVisibilityState;
		visibilityState = save.visibilityState;
		
		SwitchVisibilityState(visibilityState, true);
	}

	[Serializable]
	public class DimensionObjectSave : SaveObject<DimensionObject> {
		public VisibilityState startingVisibilityState;
		public VisibilityState visibilityState;

		public DimensionObjectSave(DimensionObject dimensionObj) : base(dimensionObj) {
			this.startingVisibilityState = dimensionObj.startingVisibilityState;
			this.visibilityState = dimensionObj.visibilityState;
		}
	}
#endregion
}
