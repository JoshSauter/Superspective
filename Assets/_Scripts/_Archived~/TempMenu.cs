using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TempMenu : Singleton<TempMenu> {
	public GameObject tempMenu;
	public Slider generalSensitivitySlider;
	public Slider xSensitivitySlider;
	public Slider ySensitivitySlider;
	public Slider headbobSlider;
	public Slider portalDownsampleSlider;

	public CursorLockMode cachedLockMode;

    void Start() {
		generalSensitivitySlider.onValueChanged.AddListener(delegate {
			PlayerLook.instance.generalSensitivity = generalSensitivitySlider.value;
		});
		xSensitivitySlider.onValueChanged.AddListener(delegate {
			PlayerLook.instance.sensitivityX = xSensitivitySlider.value;
		});
		ySensitivitySlider.onValueChanged.AddListener(delegate {
			PlayerLook.instance.sensitivityY = ySensitivitySlider.value;
		});
		headbobSlider.onValueChanged.AddListener(delegate {
			Player.instance.headbob.headbobAmount = headbobSlider.value;
		});
		portalDownsampleSlider.onValueChanged.AddListener(delegate {
			SuperspectiveScreen.instance.portalDownsampleAmount = Mathf.RoundToInt(portalDownsampleSlider.value);
		});
    }

    public void OpenMenu() {
		NovaPauseMenu.instance.OpenPauseMenu();
		
		// Assumes Time.timeScale is always 1 when we're not paused
		Time.timeScale = 0.05f;
		Cursor.visible = true;
		cachedLockMode = Cursor.lockState;
		Cursor.lockState = CursorLockMode.Confined;
	}

	public void CloseMenu(bool restoreTimeScale = true) {
		NovaPauseMenu.instance.ClosePauseMenu();
		// Assumes Time.timeScale is always 1 when we're not paused
		if (restoreTimeScale) {
			Time.timeScale = 1;
		}
		Cursor.visible = false;
		Cursor.lockState = cachedLockMode;
	}

	void Update() {
		if (PlayerButtonInput.instance.EscapePressed) {
			bool becomeActive = !tempMenu.activeSelf;
			if (becomeActive) {
				OpenMenu();
			}
			else {
				CloseMenu();
			}
		}
	}
}
