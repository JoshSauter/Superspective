using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Panel : MonoBehaviour {

	Renderer thisRenderer;
	public Color gemColor;
	public Button gemButton;

	public float colorLerpTime = 1.75f;
	MaterialPropertyBlock panelPropBlock;

#region events
	public delegate void PanelAction();
	public event PanelAction OnPanelActivateStart;
	public event PanelAction OnPanelActivateFinish;
#endregion

	// Use this for initialization
	virtual protected void Start () {
		// Set up references
		thisRenderer = GetComponent<Renderer>();

		gemButton = GetComponentInChildren<Button>();
		gemColor = gemButton.GetComponent<MeshRenderer>().material.color;
		gemButton.OnButtonPressFinish += PanelActivate;

		// Set up material property block
		panelPropBlock = new MaterialPropertyBlock();
	}

	virtual protected void PanelActivate(Button b) {
		StartCoroutine(PanelColorLerp());
	}
	
	IEnumerator PanelColorLerp() {
		if (OnPanelActivateStart != null) OnPanelActivateStart();

		thisRenderer.GetPropertyBlock(panelPropBlock);
		Color startColor = thisRenderer.material.color;

		float timeElapsed = 0;
		while (timeElapsed < colorLerpTime) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / colorLerpTime;

			Color curColor = Color.Lerp(startColor, gemColor, t);
			panelPropBlock.SetColor("_Color", curColor);
			thisRenderer.SetPropertyBlock(panelPropBlock);

			yield return null;
		}
		panelPropBlock.SetColor("_Color", gemColor);
		thisRenderer.SetPropertyBlock(panelPropBlock);

		if (OnPanelActivateFinish != null) OnPanelActivateFinish();
	}
}
