using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using EpitaphUtils;
using NaughtyAttributes;
using System.Linq;

[ExecuteInEditMode]
public class InteractableGlow : MonoBehaviour {
	public bool DEBUG = false;
	InteractableGlowManager playerCamGlowController;

	public InteractableObject interactableObject;
	public bool useLargerPrepassMaterial = false;
	public bool overrideGlowColor = false;
	[ShowIf("overrideGlowColor")]
	public Color glowColor = Color.white;

	[Range(0.0f, 1.0f)]
	public float glowAmount = 0;
	public float glowSpeed = 5f;
	public bool recursiveChildRenderers = true;

	public List<Renderer> renderers;
	public Color currentColor {
		get;
		private set;
	}
	// Target glow amount must be set every frame or it turns off
	private float targetGlowAmount;

	void Start() {
		playerCamGlowController = InteractableGlowManager.instance;

		if (recursiveChildRenderers) {
			renderers = Utils.GetComponentsInChildrenRecursively<Renderer>(transform).ToList();
		}
		else {
			renderers = new List<Renderer> { GetComponent<Renderer>() };
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
	private void Update() {
		float diff = targetGlowAmount - glowAmount;
		float glowAmountDelta = Time.deltaTime * glowSpeed;
		glowAmount += glowAmountDelta * Mathf.Sign(diff);
		if (Mathf.Sign(targetGlowAmount - glowAmount) != Mathf.Sign(diff)) {
			glowAmount = targetGlowAmount;
		}
		currentColor = Color.Lerp(Color.clear, glowColor, glowAmount);

		if (glowAmount == 0) {
			enabled = false;
		}
	}

	private void LateUpdate() {
		targetGlowAmount = 0f;
	}
}