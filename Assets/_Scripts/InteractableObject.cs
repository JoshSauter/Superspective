using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using NaughtyAttributes;

public class InteractableObject : MonoBehaviour {
	public bool useLargerPrepassMaterial = true;
	public bool overrideGlowColor = false;
	[ShowIf("overrideGlowColor")]
	public Color glowColor = new Color(.6f, .35f, .25f, 1f);
	public delegate void InteractAction();
	public InteractAction OnLeftMouseButtonDown;
	public InteractAction OnLeftMouseButton;
	public InteractAction OnLeftMouseButtonUp;
	public InteractAction OnMouseHoverExit;
	public InteractAction OnMouseHover;
	public InteractAction OnMouseHoverEnter;

	public GameObject thisRendererParent;
	public bool recursiveChildRenderers = true;

	public void Awake() {
		if (thisRendererParent == null) {
			thisRendererParent = Utils.GetComponentsInChildrenRecursively<Renderer>(transform)[0].gameObject;
		}

		if (thisRendererParent != null) {
			// gameObject.AddComponent<InteractableGlow>().thisRenderer = thisRenderer;
			InteractableGlow glow = thisRendererParent.gameObject.AddComponent<InteractableGlow>();
			glow.recursiveChildRenderers = recursiveChildRenderers;
			glow.useLargerPrepassMaterial = useLargerPrepassMaterial;
			glow.overrideGlowColor = overrideGlowColor;
			glow.GlowColor = glowColor;
			glow.interactableObject = this;
		}
	}
}
