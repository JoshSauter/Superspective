using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Panel : MonoBehaviour {

	Renderer thisRenderer;
	public Color gemColor;
	public Button gemButton;

	public float colorLerpTime = 1.75f;
	MaterialPropertyBlock panelPropBlock;

	public bool activated = false;

#region events
	public delegate void PanelAction();
	public event PanelAction OnPanelActivateBegin;
	public event PanelAction OnPanelActivateFinish;
	public event PanelAction OnPanelDeactivateBegin;
	public event PanelAction OnPanelDeactivateFinish;
#endregion

	// Use this for initialization
	virtual protected void Start () {
		// Set up references
		thisRenderer = GetComponent<Renderer>();

		gemButton = GetComponentInChildren<Button>();
		gemButton.deadTimeAfterButtonPress = colorLerpTime;
		gemButton.deadTimeAfterButtonDepress = 0.25f;
		gemColor = gemButton.GetComponent<MeshRenderer>().material.color;
		gemButton.OnButtonPressFinish += PanelActivate;
		gemButton.OnButtonDepressBegin += PanelDeactivate;

		// Set up material property block
		panelPropBlock = new MaterialPropertyBlock();
	}

	virtual protected void PanelActivate(Button b) {
		activated = true;
		StartCoroutine(PanelColorLerp(thisRenderer.material.color, gemColor));
	}

	virtual protected void PanelDeactivate(Button b) {
		activated = false;
		StartCoroutine(PanelColorLerp(gemColor, thisRenderer.material.color));
	}
	
	IEnumerator PanelColorLerp(Color startColor, Color endColor) {
		if (activated && OnPanelActivateBegin != null) OnPanelActivateBegin();
		else if (!activated && OnPanelDeactivateBegin != null) OnPanelDeactivateBegin();

		thisRenderer.GetPropertyBlock(panelPropBlock);

		float timeElapsed = 0;
		while (timeElapsed < colorLerpTime) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / colorLerpTime;

			Color curColor = Color.Lerp(startColor, endColor, t);
			panelPropBlock.SetColor("_Color", curColor);
			thisRenderer.SetPropertyBlock(panelPropBlock);

			yield return null;
		}
		panelPropBlock.SetColor("_Color", endColor);
		thisRenderer.SetPropertyBlock(panelPropBlock);

		if (activated && OnPanelActivateFinish != null) OnPanelActivateFinish();
		else if (!activated && OnPanelDeactivateFinish != null) OnPanelDeactivateFinish();
	}
}
