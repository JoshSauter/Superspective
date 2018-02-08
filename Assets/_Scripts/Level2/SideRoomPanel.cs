using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SideRoomPanel : MonoBehaviour {
	Color gemColor;
	Button gemButton;

	public Renderer roomRenderer;
	Renderer thisRenderer;
	public float colorLerpTime = 1.75f;
	public float roomEmissionLevel = 0.2f;
	public Color startingRoomColor = new Color(0.1f, 0.1f, 0.1f);
	MaterialPropertyBlock panelPropBlock;
	MaterialPropertyBlock roomPropBlock;

	// Use this for initialization
	void Start () {
		panelPropBlock = new MaterialPropertyBlock();
		roomPropBlock = new MaterialPropertyBlock();
		thisRenderer = GetComponent<Renderer>();

		roomRenderer.GetPropertyBlock(roomPropBlock);
		roomPropBlock.SetColor("_Color", startingRoomColor);
		roomRenderer.SetPropertyBlock(roomPropBlock);

		gemButton = GetComponentInChildren<Button>();
		gemColor = gemButton.GetComponent<MeshRenderer>().material.color;
		gemButton.OnButtonPressFinish += PanelActivate;
	}

	void PanelActivate(Button b) {
		StartCoroutine(PanelColorLerp());
		StartCoroutine(RoomColorLerp());

		b.GetComponent<ObjectHover>().hoveringPaused = true;
		b.GetComponent<RotateObject>().enabled = false;
	}

	IEnumerator PanelColorLerp() {
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
	}

	IEnumerator RoomColorLerp() {
		roomRenderer.GetPropertyBlock(roomPropBlock);
		Color startColor = roomRenderer.material.color;

		yield return new WaitForSeconds(colorLerpTime/2f);

		float timeElapsed = 0;
		while (timeElapsed < colorLerpTime) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / colorLerpTime;

			Color curColor = Color.Lerp(startColor, gemColor, t);
			roomPropBlock.SetColor("_Color", curColor);
			roomPropBlock.SetColor("_EmissionColor", curColor * roomEmissionLevel);
			roomRenderer.SetPropertyBlock(roomPropBlock);

			yield return null;
		}
		//roomPropBlock.SetColor("_Color", gemColor);
		//roomPropBlock.SetColor("_EmissionColor", gemColor * roomEmissionLevel);
		//roomRenderer.SetPropertyBlock(panelPropBlock);
	}
}
