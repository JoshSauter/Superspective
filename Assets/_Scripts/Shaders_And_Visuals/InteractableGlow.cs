using UnityEngine;
using EpitaphUtils;

[ExecuteInEditMode]
public class InteractableGlow : MonoBehaviour {
	public InteractableObject interactableObject;
	public Color GlowColor;
	public float LerpFactor = 10;
	public bool recursiveChildRenderers = true;

	public Renderer[] Renderers {
		get;
		private set;
	}
	public Color CurrentColor {
		get { return _currentColor; }
	}
	private Color _currentColor;
	private Color _targetColor;

	void Start() {
		if (recursiveChildRenderers) {
			Renderers = Utils.GetComponentsInChildrenRecursively<Renderer>(transform);
		}
		else {
			Renderers = new Renderer[1] { GetComponent<Renderer>() };
		}
		InteractableGlowController.instance.Add(this);
		interactableObject.OnMouseHover += OnMouseHover;
		interactableObject.OnMouseHoverExit += OnMouseHoverExit;
	}

	public void OnEnable() {
		InteractableGlowController.instance.Add(this);
	}

	public void OnDisable() {
		InteractableGlowController.instance.Remove(this);
	}

	private void OnMouseHover() {
		_targetColor = GlowColor;
		enabled = true;
	}

	private void OnMouseHoverExit() {
		_targetColor = Color.black;
		enabled = true;
	}

	/// <summary>
	/// Update color, disable self if we reach our target color.
	/// </summary>
	private void Update() {
		_currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * LerpFactor);

		if (ColorsAreCloseEnough(_currentColor, Color.black)) {
			_currentColor = _targetColor;
			enabled = false;
		}
	}

	bool ColorsAreCloseEnough(Color c1, Color c2) {
		float deltaAllowed = 0.0001f;
		var v1 = new Vector3(c1.r, c1.g, c1.b);
		var v2 = new Vector3(c2.r, c2.g, c2.b);

		return (v2 - v1).magnitude < deltaAllowed;
	}
}