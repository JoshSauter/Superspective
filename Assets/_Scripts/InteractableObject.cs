using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class InteractableObject : MonoBehaviour {
	public Color glowColor = Color.green;
	public delegate void InteractAction();
	public InteractAction OnLeftMouseButtonDown;
	public InteractAction OnLeftMouseButton;
	public InteractAction OnLeftMouseButtonUp;
	public InteractAction OnMouseHoverExit;
	public InteractAction OnMouseHover;

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
			glow.GlowColor = glowColor;
			glow.interactableObject = this;
		}
	}
}
