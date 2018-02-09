using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class SideRoomPanel : MonoBehaviour {
	public Color gemColor;
	Button gemButton;
	Transform[] laserParents;
	public ParticleSystem laser;

	public Renderer roomRenderer;
	Renderer thisRenderer;
	public float colorLerpTime = 1.75f;
	public float roomEmissionLevel = 0.2f;
	public Color startingRoomColor = new Color(0.1f, 0.1f, 0.1f);
	MaterialPropertyBlock panelPropBlock;
	MaterialPropertyBlock roomPropBlock;

#region events
	public delegate void SideRoomPanelAction();
	public SideRoomPanelAction OnPanelActivateStart;
	public SideRoomPanelAction OnPanelActivateFinish;
	public SideRoomPanelAction OnLaserActivateStart;
	public SideRoomPanelAction OnLaserActivateFinish;
#endregion

	// Use this for initialization
	void Start () {
		// Set up references
		thisRenderer = GetComponent<Renderer>();

		List<Transform> laserParentsList = new List<Transform>(Utils.GetComponentsInChildrenOnly<Transform>(laser.transform.parent));
		laserParents = laserParentsList.FindAll(x => x != laser.transform).ToArray();

		gemButton = GetComponentInChildren<Button>();
		gemColor = gemButton.GetComponent<MeshRenderer>().material.color;
		gemButton.OnButtonPressFinish += PanelActivate;

		// Set up material property blocks
		panelPropBlock = new MaterialPropertyBlock();
		roomPropBlock = new MaterialPropertyBlock();

		// Set initial room color
		roomRenderer.GetPropertyBlock(roomPropBlock);
		roomPropBlock.SetColor("_Color", startingRoomColor);
		roomRenderer.SetPropertyBlock(roomPropBlock);
	}

	void PanelActivate(Button b) {
		StartCoroutine(PanelColorLerp());
		StartCoroutine(RoomColorLerp());
		TurnOnLaser();

		b.GetComponent<ObjectHover>().hoveringPaused = true;
		b.GetComponent<RotateObject>().enabled = false;
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

	void TurnOnLaser() {
		if (OnLaserActivateStart != null) OnLaserActivateStart();
		laser.Play();

		foreach (var laserParent in laserParents) {
			MaterialPropertyBlock props = new MaterialPropertyBlock();
			Renderer objRenderer = laserParent.GetComponent<Renderer>();
			objRenderer.GetPropertyBlock(props);
			props.SetColor("_Color", gemColor);
			objRenderer.SetPropertyBlock(props);
		}

		// Approximate how long it takes the laser to hit the receiver
		Invoke("LaserFinish", 0.5f);
	}

	void LaserFinish() {
		if (OnLaserActivateFinish != null)
			OnLaserActivateFinish();
	}

}
