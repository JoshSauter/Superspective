using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Audio;

public class Panel : MonoBehaviour {
	EpitaphRenderer thisRenderer;
	public Color gemColor;
	public Button gemButton;

	public float colorLerpTime = 1.75f;

	public bool activated = false;

	// Sound settings
	bool soundActivated = false;
	float minPitch = 0.5f;
	float maxPitch = 1f;
	float minVolume = 0.25f;
	float maxVolume = 1f;
	public SoundEffect electricalHumSound;

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

		gemButton.OnButtonPressBegin += (ctx) => TurnOnSounds();
		gemButton.OnButtonDepressBegin += (ctx) => TurnOffSounds();

		electricalHumSound.audioSource.pitch = minPitch;
		electricalHumSound.audioSource.volume = minVolume;
	}

	private void Update() {
		UpdateSound();
	}

	void TurnOnSounds() {
		soundActivated = true;
	}

	void TurnOffSounds() {
		soundActivated = false;
	}

	void UpdateSound() {
		if (soundActivated && electricalHumSound.audioSource.volume < maxVolume) {
			float soundLerpSpeedOn = 1f;
			float newPitch = Mathf.Clamp(electricalHumSound.audioSource.pitch + Time.deltaTime * soundLerpSpeedOn, minPitch, maxPitch);
			float newVolume = Mathf.Clamp(electricalHumSound.audioSource.volume + Time.deltaTime * soundLerpSpeedOn, minVolume, maxVolume);

			electricalHumSound.audioSource.pitch = newPitch;
			electricalHumSound.audioSource.volume = newVolume;
		}

		if (!soundActivated && electricalHumSound.audioSource.volume > minVolume) {
			float soundLerpSpeedOff = .333f;
			float newPitch = Mathf.Clamp(electricalHumSound.audioSource.pitch - Time.deltaTime * soundLerpSpeedOff, minPitch, maxPitch);
			float newVolume = Mathf.Clamp(electricalHumSound.audioSource.volume - Time.deltaTime * soundLerpSpeedOff, minVolume, maxVolume);

			electricalHumSound.audioSource.pitch = newPitch;
			electricalHumSound.audioSource.volume = newVolume;
		}
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
