using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SideRoomPanel : MonoBehaviour {
	Color gemColor;
	Button gemButton;

	Renderer thisRenderer;
	public float colorLerpTime = 1.75f;
	MaterialPropertyBlock propBlock;

	// Use this for initialization
	void Start () {
		propBlock = new MaterialPropertyBlock();
		thisRenderer = GetComponent<Renderer>();

		gemButton = GetComponentInChildren<Button>();
		gemColor = gemButton.GetComponent<MeshRenderer>().material.color;
		gemButton.OnButtonPressFinish += PanelActivate;
	}

	void PanelActivate(Button b) {
		StartCoroutine(ColorLerp());

		b.GetComponent<ObjectHover>().hoveringPaused = true;
		b.GetComponent<RotateObject>().enabled = false;
	}

	IEnumerator ColorLerp() {
		thisRenderer.GetPropertyBlock(propBlock);
		Color startColor = thisRenderer.material.color;

		float timeElapsed = 0;
		while (timeElapsed < colorLerpTime) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / colorLerpTime;

			propBlock.SetColor("_Color", Color.Lerp(startColor, gemColor, t));
			thisRenderer.SetPropertyBlock(propBlock);

			yield return null;
		}
		propBlock.SetColor("_Color", gemColor);
		thisRenderer.SetPropertyBlock(propBlock);
	}
}
