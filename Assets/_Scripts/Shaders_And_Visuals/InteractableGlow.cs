using UnityEngine;
using System.Collections;
using EpitaphUtils;
using NaughtyAttributes;

[ExecuteInEditMode]
public class InteractableGlow : MonoBehaviour {
	public InteractableObject interactableObject;
	public bool useLargerPrepassMaterial = true;
	public bool overrideGlowColor = false;
	[ShowIf("overrideGlowColor")]
	public Color GlowColor;
	public float LerpFactor = 10;
	public bool recursiveChildRenderers = true;

	InteractableGlowManager playerCamGlowController;

	public Renderer[] Renderers {
		get;
		private set;
	}
	public Color CurrentColor {
		get { return _currentColor; }
		set { _currentColor = value; }
	}
	private Color _currentColor;
	// Target color must be set every frame or it turns off
	private Color _targetColor;

	void Start() {
		playerCamGlowController = InteractableGlowManager.instance;

		if (recursiveChildRenderers) {
			Renderers = Utils.GetComponentsInChildrenRecursively<Renderer>(transform);
		}
		else {
			Renderers = new Renderer[1] { GetComponent<Renderer>() };
		}
		playerCamGlowController?.Add(this);

		if (interactableObject == null) {
			interactableObject = GetComponent<InteractableObject>();
		}
		interactableObject.OnMouseHover += TurnOnGlow;
		interactableObject.OnMouseHoverExit += TurnOffGlow;
	}

	public void OnEnable() {
		playerCamGlowController = InteractableGlowManager.instance;
		playerCamGlowController?.Add(this);
	}

	public void OnDisable() {
		playerCamGlowController?.Remove(this);
	}

	public void TurnOnGlow() {
		_targetColor = GlowColor;
		enabled = true;
	}

	public void TurnOffGlow() {
		_targetColor = Color.clear;
		enabled = true;
	}

	/// <summary>
	/// Update color, disable self if we reach our target color.
	/// </summary>
	private void Update() {
		_currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * LerpFactor);

		if (ColorsAreCloseEnough(_currentColor, Color.clear)) {
			_currentColor = _targetColor;
			enabled = false;
		}
	}

	private void LateUpdate() {
		_targetColor = Color.clear;
	}

	bool ColorsAreCloseEnough(Color c1, Color c2) {
		float deltaAllowed = 0.0001f;
		var v1 = new Vector4(c1.r, c1.g, c1.b, c1.a);
		var v2 = new Vector4(c2.r, c2.g, c2.b, c2.a);

		return (v2 - v1).magnitude < deltaAllowed;
	}
}