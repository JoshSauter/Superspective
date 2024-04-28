using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using SuperspectiveUtils.ShaderUtils;
using System.Linq;
using Saving;
using System;
using NaughtyAttributes;
using PortalMechanics;

public enum VisibilityState {
	invisible,
	partiallyVisible,
	visible,
	partiallyInvisible,
};

public static class VisibilityStateExt {
	public static VisibilityState Opposite(this VisibilityState visibilityState) {
		switch (visibilityState) {
			case VisibilityState.invisible:
				return VisibilityState.visible;
			case VisibilityState.partiallyVisible:
				return VisibilityState.partiallyInvisible;
			case VisibilityState.visible:
				return VisibilityState.invisible;
			case VisibilityState.partiallyInvisible:
				return VisibilityState.partiallyVisible;
			default:
				throw new ArgumentOutOfRangeException(nameof(visibilityState), visibilityState, null);
		}
	}
}

[RequireComponent(typeof(UniqueId))]
public class DimensionObject : SaveableObject<DimensionObject, DimensionObject.DimensionObjectSave> {
	public override string ID => $"{gameObject.name}_{base.ID}";

	public static HashSet<DimensionObject> allDimensionObjects = new HashSet<DimensionObject>();
	public const int NUM_CHANNELS = 8;
	public bool treatChildrenAsOneObjectRecursively = false;
	public bool ignoreChildrenWithDimensionObject = true;

	protected bool initialized = false;
	[Range(0, NUM_CHANNELS-1)]
	public int channel;
	public bool reverseVisibilityStates = false;
	// Set to true by Portals to keep them on only Portal or Invisible layers since VisibleButNoPlayerCollision layer
	// prevents them from being rendered to PortalMask camera
	public bool ignorePartiallyVisibleLayerChanges = false;
	public bool disableColliderWhileInvisible = true;
	protected int curDimensionSetInMaterial;

	public SuperspectiveRenderer[] renderers;
	public Collider[] colliders;
	Dictionary<SuperspectiveRenderer, int> startingLayers;
	public SphereCollider ignoreCollisionsTriggerZone;

	public VisibilityState startingVisibilityState = VisibilityState.visible;
	public VisibilityState visibilityState = VisibilityState.visible;
	public VisibilityState effectiveVisibilityState => reverseVisibilityStates
		? visibilityState.Opposite()
		: visibilityState;
	static Dictionary<VisibilityState, HashSet<VisibilityState>> nextStates = new Dictionary<VisibilityState, HashSet<VisibilityState>> {
		{ VisibilityState.invisible, new HashSet<VisibilityState> { VisibilityState.partiallyVisible, VisibilityState.partiallyInvisible } },
		{ VisibilityState.partiallyVisible, new HashSet<VisibilityState> { VisibilityState.invisible, VisibilityState.visible } },
		{ VisibilityState.visible, new HashSet<VisibilityState> { VisibilityState.partiallyVisible, VisibilityState.partiallyInvisible } },
		{ VisibilityState.partiallyInvisible, new HashSet<VisibilityState> { VisibilityState.invisible, VisibilityState.visible } }
	};

	public bool[] collisionMatrix = new bool[COLLISION_MATRIX_COLS * COLLISION_MATRIX_ROWS] {
		 true, false, false, false, false, false,
		false,  true, false, false, false, false,
		false, false,  true, false,  true,  true,
		false, false, false,  true,  true,  true
	};
	public const int COLLISION_MATRIX_ROWS = 4;
	// First 4 columns are VisibilityStates, 5 is Player and 6 is Other Non-DimensionObjects
	public const int COLLISION_MATRIX_COLS = 6;
	public DimensionObjectCollisions collisionLogic;

	public const string IgnoreCollisionsTriggerZone = "IgnoreCollisionsTriggerZone";
	public bool isBeingDestroyed = false;

#region events
	public delegate void DimensionObjectStateChangeAction(DimensionObject context);
	public delegate void DimensionObjectStateChangeActionSimple();

	// Immediate will fire immediately after any state change happens
	public event DimensionObjectStateChangeAction OnStateChangeImmediate;
	// Non-immediate events wait until end of frame to see if the net visibility state has changed
	public event DimensionObjectStateChangeAction OnStateChange;
	public event DimensionObjectStateChangeActionSimple OnStateChangeSimple;
	#endregion

	protected override void OnValidate() {
		base.OnValidate();
		if (collisionMatrix == null || collisionMatrix.Length < COLLISION_MATRIX_ROWS * COLLISION_MATRIX_COLS) {
			collisionMatrix = new bool[COLLISION_MATRIX_COLS * COLLISION_MATRIX_ROWS] {
				true, false, false, false, false, false,
				false, true, false, false, false, false,
				false, false, true, false, true, true,
				false, false, false, true, true, true
			};
		}
	}

	protected override void Awake() {
		base.Awake();

		InitializeRenderersAndLayers();
	}

	protected override void Init() {
		foreach (var r in renderers) {
			SetShaderProperties(r);
		}
		SetupDimensionCollisionLogic();
		SetChannelValuesInMaterials();

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
	
	void OnDisable() {
		allDimensionObjects.Remove(this);
		isBeingDestroyed = true;
		visibilityState = VisibilityState.visible;
		OnStateChangeImmediate?.Invoke(this);
		OnStateChange?.Invoke(this);
		OnStateChangeSimple?.Invoke();
		try {
			SetChannelValuesInMaterials(false);
		} catch {}
	}

	void OnEnable() {
		allDimensionObjects.Add(this);
		SetChannelValuesInMaterials();
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
			Vector3 thisMin = c.bounds.min;
			Vector3 thisMax = c.bounds.max;

			min = MinOfTwoVectors(min, thisMin);
			max = MaxOfTwoVectors(max, thisMax);
		}

		Vector3 size = (max - min);
		size.x /= transform.lossyScale.x;
		size.y /= transform.lossyScale.y;
		size.z /= transform.lossyScale.z;
		Vector3 center = (max + min) / 2f;

		GameObject triggerGO = new GameObject(IgnoreCollisionsTriggerZone) {
			layer = LayerMask.NameToLayer("Ignore Raycast")
		};
		triggerGO.transform.SetParent(transform, false);
		collisionLogic = triggerGO.AddComponent<DimensionObjectCollisions>();
		collisionLogic.dimensionObject = this;
		SphereCollider trigger = triggerGO.AddComponent<SphereCollider>();
		trigger.radius = Mathf.Max(size.x, size.y, size.z);
		trigger.center = transform.InverseTransformPoint(center);
		trigger.isTrigger = true;

		return trigger;
	}

	public bool IsVisibleFromMask(int maskValue) {
		bool ChannelOverlap() {
			if (useAdvancedChannelLogic) {
				return maskRenderSolution[maskValue] > 0;
			}
			else {
				return DimensionShaderUtils.ChannelIsOnForMaskValue(channel, maskValue);
			}
		}

		switch (effectiveVisibilityState) {
			case VisibilityState.invisible:
				return false;
			case VisibilityState.partiallyVisible:
				return ChannelOverlap();
			case VisibilityState.partiallyInvisible:
				return !ChannelOverlap();
			case VisibilityState.visible:
				return true;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
	
	public static bool ShouldCollide(DimensionObject a, DimensionObject b) {
		bool HaveChannelOverlap() {
			bool MasksHaveOverlap(float[] maskSolutionsA, float[] maskSolutionsB) {
				for (int i = 0; i < maskSolutionsA.Length; i++) {
					if (maskSolutionsA[i] > 0 && maskSolutionsB[i] > 0) {
						return true;
					}
				}

				return false;
			}

			bool ChannelIsOnForMask(int channel, float[] maskSolution) {
				return maskSolution[channel] > 0;
			}

			return a.useAdvancedChannelLogic switch {
				true when b.useAdvancedChannelLogic => MasksHaveOverlap(a.maskRenderSolution, b.maskRenderSolution),
				true when !b.useAdvancedChannelLogic => ChannelIsOnForMask(b.channel, a.maskRenderSolution),
				false when b.useAdvancedChannelLogic => ChannelIsOnForMask(a.channel, b.maskRenderSolution),
				_ => a.channel == b.channel
			};
		}
		
		return HaveChannelOverlap() && ShouldCollideInSameChannel(a, b);
	}

	public virtual bool ShouldCollideWithNonDimensionObject() {
		return collisionMatrix[(int)visibilityState * COLLISION_MATRIX_COLS + COLLISION_MATRIX_COLS - 1];
	}

	public virtual bool ShouldCollideWithPlayer() {
		return collisionMatrix[(int)visibilityState * COLLISION_MATRIX_COLS + COLLISION_MATRIX_COLS - 2];
	}

	static bool ShouldCollideInSameChannel(DimensionObject a, DimensionObject b) {
		int aVisibility = (int)(a.reverseVisibilityStates ? a.visibilityState.Opposite() : a.visibilityState);
		int bVisibility = (int)(b.reverseVisibilityStates ? b.visibilityState.Opposite() : b.visibilityState);
		return a.collisionMatrix[aVisibility * COLLISION_MATRIX_COLS + bVisibility] && b.collisionMatrix[bVisibility * COLLISION_MATRIX_COLS + aVisibility];
	}
	
	public virtual bool ShouldCollideWith(DimensionObject other) {
		return ShouldCollide(this, other);
	}

	public void SetCollision(VisibilityState thisVisibility, VisibilityState otherVisibility, bool shouldCollide) {
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

	public void SwitchVisibilityState(VisibilityState nextState, bool ignoreTransitionRules = false, bool sendEvents = true) {
		if (!(ignoreTransitionRules || IsValidNextState(nextState))) return;

		debug.Log("State transition: " + visibilityState + " --> " + nextState);

		switch (nextState) {
			case VisibilityState.invisible:
				visibilityState = VisibilityState.invisible;
				break;
			case VisibilityState.partiallyVisible:
				visibilityState = VisibilityState.partiallyVisible;
				break;
			case VisibilityState.visible:
				visibilityState = VisibilityState.visible;
				break;
			case VisibilityState.partiallyInvisible:
				visibilityState = VisibilityState.partiallyInvisible;
				break;
		}
		
		foreach (var r in renderers) {
			SetShaderProperties(r);
		}
		SetChannelValuesInMaterials();

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
	public void InitializeRenderersAndLayers() {
		renderers = GetAllSuperspectiveRenderers().ToArray();
		if (colliders == null || colliders.Length == 0) {
			colliders = GetAllColliders().ToArray();
		}

		if (renderers.Length == 0) {
			debug.LogError("No renderers found for: " + gameObject.name);
		}

		SetStartingLayersFromCurrentLayers();
	}

	public void SetStartingLayersFromCurrentLayers() {
		startingLayers = GetAllStartingLayers(renderers);
	}

	void SetShaderProperties(SuperspectiveRenderer renderer) {
		void SetLayers() {
			if (!startingLayers.ContainsKey(renderer)) {
				startingLayers.Add(renderer, renderer.gameObject.layer);
			}
			
			switch (visibilityState) {
				case VisibilityState.invisible:
					renderer.gameObject.layer = reverseVisibilityStates
						? startingLayers[renderer]
						: SuperspectivePhysics.InvisibleLayer;
					break;
				case VisibilityState.visible:
					renderer.gameObject.layer = reverseVisibilityStates
						? SuperspectivePhysics.InvisibleLayer
						: startingLayers[renderer];
					break;
				case VisibilityState.partiallyVisible:
				case VisibilityState.partiallyInvisible:
					renderer.gameObject.layer = reverseVisibilityStates
						? (ignorePartiallyVisibleLayerChanges
							? startingLayers[renderer]
							: SuperspectivePhysics.VisibleButNoPlayerCollisionLayer)
						: startingLayers[renderer];
					break;
			}
		}

		bool DetermineInverse() {
			bool inverseShader = false;
			switch (visibilityState) {
				case VisibilityState.invisible:
					break;
				case VisibilityState.partiallyVisible:
					inverseShader = reverseVisibilityStates;
					break;
				case VisibilityState.visible:
					break;
				case VisibilityState.partiallyInvisible:
					inverseShader = !reverseVisibilityStates;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return inverseShader;
		}
		
		SetLayers();
		bool inverseShader = DetermineInverse();
		
		// Disable colliders while invisible
		if (disableColliderWhileInvisible && renderer.TryGetComponent(out Collider c)) {
			c.enabled = (!reverseVisibilityStates && visibilityState != VisibilityState.invisible) ||
			            (reverseVisibilityStates && visibilityState != VisibilityState.visible);
		}

		renderer.SetInt("_Inverse", inverseShader ? 1 : 0);
		SetDimensionKeyword(renderer, effectiveVisibilityState != VisibilityState.visible);
	}

	void SetDimensionKeyword(SuperspectiveRenderer renderer, bool value) {
		const string keyword = "DIMENSION_OBJECT";
		foreach (Material material in renderer.r.materials) {
			if (material.IsKeywordEnabled(keyword) != value) {
				if (value) {
					material.EnableKeyword(keyword);
				}
				else {
					material.DisableKeyword(keyword);
				}
			}
		}
	}

	protected List<Collider> GetAllColliders() {
		List<Collider> result = new List<Collider>();

		void GetCollidersRecursively(Transform parent) {
			// Children who have DimensionObject scripts are treated on only by their own settings
			if (parent != transform && ignoreChildrenWithDimensionObject && parent.GetComponent<DimensionObject>() != null) return;

			result.AddRange(parent.GetComponents<Collider>());

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
			if (thisRenderer != null) {
				allRenderers.Add(thisRenderer);
			}
		}
		else {
			SetSuperspectiveRenderersRecursively(transform, ref allRenderers);
		}
		return allRenderers;
	}

	void SetSuperspectiveRenderersRecursively(Transform parent, ref List<SuperspectiveRenderer> renderersSoFar) {
		// Children who have DimensionObject scripts are treated on only by their own settings
		if (parent != transform && ignoreChildrenWithDimensionObject && parent.GetComponent<DimensionObject>() != null) return;

		SuperspectiveRenderer thisRenderer = parent.GetComponent<SuperspectiveRenderer>();
		if (thisRenderer == null && parent.GetComponent<Renderer>() != null) {
			thisRenderer = parent.gameObject.AddComponent<SuperspectiveRenderer>();
		}

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
	public bool useAdvancedChannelLogic = false;
	public string channelLogic = "";
	float[] maskRenderSolution;
	
	void SetChannelValuesInMaterials(bool turnOn = true) {
		foreach (var r in renderers) {
			if (useAdvancedChannelLogic) {
				if (maskRenderSolution == null) {
					ValidateAndApplyChannelLogic();
				}

				foreach (var material in r.GetMaterials()) {
					material.EnableKeyword("USE_ADVANCED_CHANNEL_LOGIC");
				}
				r.SetFloatArray("_AcceptableMaskValues", maskRenderSolution);
			}
			else {
				foreach (var material in r.GetMaterials()) {
					material.DisableKeyword("USE_ADVANCED_CHANNEL_LOGIC");
				}
				r.SetInt("_Channel", turnOn ? channel : NUM_CHANNELS);
			}
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
	
	[Button("Apply boolean expression")]
	public void ValidateAndApplyChannelLogic() {
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
		maskRenderSolution = SolutionArrayFromPostfixExpression(postfixExpression);
		string solutionLog = string.Join("\n", maskRenderSolution.Select((value, index) => $"{index}\t| {value}"));
		//Debug.Log($"Boolean channel expression applied to {gameObject.name}.\n{solutionLog}");
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
	/// <returns>Int array of size NUM_CHANNELS^2 with 1 or 0 in each cell</returns>
	public static float[] SolutionArrayFromPostfixExpression(List<string> postfixExpression) {
		float[] solution = new float[1 << NUM_CHANNELS];
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

	[Serializable]
	public class DimensionObjectSave : SerializableSaveObject<DimensionObject> {
		bool treatChildrenAsOneObjectRecursively;
		bool ignoreChildrenWithDimensionObject;
		bool disableColliderWhileInvisible;

		bool initialized;
		int channel;
		bool reverseVisibilityStates;
		bool ignorePartiallyVisibleLayerChanges;
		int curDimensionSetInMaterial;

		int startingVisibilityState;
		public int visibilityState;

		public DimensionObjectSave(DimensionObject dimensionObj) : base(dimensionObj) {
			this.treatChildrenAsOneObjectRecursively = dimensionObj.treatChildrenAsOneObjectRecursively;
			this.ignoreChildrenWithDimensionObject = dimensionObj.ignoreChildrenWithDimensionObject;
			this.disableColliderWhileInvisible = dimensionObj.disableColliderWhileInvisible;
			this.initialized = dimensionObj.initialized;
			this.channel = dimensionObj.channel;
			this.reverseVisibilityStates = dimensionObj.reverseVisibilityStates;
			this.ignorePartiallyVisibleLayerChanges = dimensionObj.ignorePartiallyVisibleLayerChanges;
			this.curDimensionSetInMaterial = dimensionObj.curDimensionSetInMaterial;
			this.startingVisibilityState = (int)dimensionObj.startingVisibilityState;
			this.visibilityState = (int)dimensionObj.visibilityState;
		}

		public override void LoadSave(DimensionObject dimensionObj) {
			dimensionObj.treatChildrenAsOneObjectRecursively = this.treatChildrenAsOneObjectRecursively;
			dimensionObj.ignoreChildrenWithDimensionObject = this.ignoreChildrenWithDimensionObject;
			dimensionObj.disableColliderWhileInvisible = this.disableColliderWhileInvisible;
			dimensionObj.initialized = this.initialized;
			dimensionObj.channel = this.channel;
			dimensionObj.reverseVisibilityStates = this.reverseVisibilityStates;
			dimensionObj.ignorePartiallyVisibleLayerChanges = this.ignorePartiallyVisibleLayerChanges;
			dimensionObj.curDimensionSetInMaterial = this.curDimensionSetInMaterial;
			dimensionObj.startingVisibilityState = (VisibilityState)this.startingVisibilityState;
			dimensionObj.visibilityState = (VisibilityState)this.visibilityState;

			dimensionObj.SwitchVisibilityState(dimensionObj.visibilityState, true);
		}
	}
	#endregion
}
