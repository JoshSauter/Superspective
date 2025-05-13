using UnityEngine;
using SuperspectiveUtils;
using System.Linq;
using Sirenix.OdinInspector;

public class InteractableGlow : MonoBehaviour {
	public bool DEBUG = false;
	InteractableGlowManager playerCamGlowController;

	public InteractableObject interactableObject;
	public bool useLargerPrepassMaterial = false;
	[ShowInInspector]
	public bool OverrideGlowColor => interactableObject?.overrideGlowColor ?? false;
	[ShowInInspector, ShowIf(nameof(OverrideGlowColor))]
	public Color GlowColor => interactableObject?.glowColor ?? Color.black;

	[Range(0.0f, 1.0f)]
	public float glowAmount = 0;
	public float glowSpeed = 5f;
	public bool recursiveChildRenderers = true;

	public NullSafeList<Renderer> renderers = new NullSafeList<Renderer>();
	public Color currentColor {
		get;
		private set;
	}
	// Target glow amount must be set every frame or it turns off
	float targetGlowAmount;

	void Start() {
		playerCamGlowController = InteractableGlowManager.instance;

		if (recursiveChildRenderers) {
			renderers = Utils.GetComponentsInChildrenRecursively<Renderer>(transform).ToList();
		}
		else {
			renderers = new NullSafeList<Renderer> { GetComponent<Renderer>() };
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
		targetGlowAmount = 1.0f;
		enabled = true;
	}

	public void TurnOffGlow() {
		targetGlowAmount = 0f;
		enabled = true;
	}

	/// <summary>
	/// Update color, disable self if we reach our target color.
	/// </summary>
	void Update() {
		float diff = targetGlowAmount - glowAmount;
		float glowAmountDelta = Time.deltaTime * glowSpeed;
		glowAmount += glowAmountDelta * Mathf.Sign(diff);
		if (Mathf.Sign(targetGlowAmount - glowAmount) != Mathf.Sign(diff)) {
			glowAmount = targetGlowAmount;
		}
		currentColor = Color.Lerp(Color.clear, GlowColor, glowAmount);

		if (glowAmount == 0) {
			enabled = false;
		}
	}

	void LateUpdate() {
		targetGlowAmount = 0f;
	}
}