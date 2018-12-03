using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class SideRoomPanel : Panel {
	Transform[] laserParents;
	public ParticleSystem laser;

	public Renderer roomRenderer;
	public float roomEmissionLevel = 0.2f;
	public Color startingRoomColor = new Color(0.1f, 0.1f, 0.1f);
	MaterialPropertyBlock roomPropBlock;

#region events
	public event PanelAction OnLaserActivateStart;
	public event PanelAction OnLaserActivateFinish;
#endregion

	// Use this for initialization
	override protected void Start () {
		base.Start();
		List<Transform> laserParentsList = new List<Transform>(laser.transform.parent.GetComponentsInChildrenOnly<Transform>());
		laserParents = laserParentsList.FindAll(x => x != laser.transform).ToArray();


		// Set up material property block
		roomPropBlock = new MaterialPropertyBlock();

		// Set initial room color
		roomRenderer.GetPropertyBlock(roomPropBlock);
		roomPropBlock.SetColor("_Color", startingRoomColor);
		roomRenderer.SetPropertyBlock(roomPropBlock);
	}

	protected override void PanelActivate(Button b) {
		base.PanelActivate(b);
		StartCoroutine(RoomColorLerp());
		TurnOnLaser();

		b.GetComponent<ObjectHover>().hoveringPaused = true;
		b.GetComponent<RotateObject>().enabled = false;
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
