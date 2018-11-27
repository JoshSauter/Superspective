using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Panel : MonoBehaviour {
	EpitaphRenderer thisRenderer;
	public Color gemColor;
	public Button gemButton;

	public float colorLerpTime = 1.75f;

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
		thisRenderer = gameObject.GetComponent<EpitaphRenderer>();
		if (thisRenderer == null) {
			thisRenderer = gameObject.AddComponent<EpitaphRenderer>();
		}

		gemButton = GetComponentInChildren<Button>();
		gemButton.deadTimeAfterButtonPress = colorLerpTime;
		gemButton.deadTimeAfterButtonDepress = 0.25f;
        EpitaphRenderer gemButtonRenderer = gemButton.GetComponent<EpitaphRenderer>();
        if (gemButtonRenderer == null) {
            gemButtonRenderer = gemButton.gameObject.AddComponent<EpitaphRenderer>();
        }
		gemColor = gemButtonRenderer.GetMainColor();
		gemButton.OnButtonPressFinish += PanelActivate;
		gemButton.OnButtonDepressBegin += PanelDeactivate;
	}

	virtual protected void PanelActivate(Button b) {
		activated = true;
		StartCoroutine(PanelColorLerp(thisRenderer.GetMainColor(), gemColor));
	}

	virtual protected void PanelDeactivate(Button b) {
		activated = false;
		StartCoroutine(PanelColorLerp(gemColor, thisRenderer.GetMainColor()));
	}
	
	IEnumerator PanelColorLerp(Color startColor, Color endColor) {
		if (activated && OnPanelActivateBegin != null) OnPanelActivateBegin();
		else if (!activated && OnPanelDeactivateBegin != null) OnPanelDeactivateBegin();
		
		float timeElapsed = 0;
		while (timeElapsed < colorLerpTime) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / colorLerpTime;

			Color curColor = Color.Lerp(startColor, endColor, t);
			thisRenderer.SetMainColor(curColor);

			yield return null;
		}
		thisRenderer.SetMainColor(endColor);

		if (activated && OnPanelActivateFinish != null) OnPanelActivateFinish();
		else if (!activated && OnPanelDeactivateFinish != null) OnPanelDeactivateFinish();
	}
}
