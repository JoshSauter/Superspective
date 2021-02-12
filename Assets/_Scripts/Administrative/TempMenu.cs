using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TempMenu : Singleton<TempMenu> {
	public bool menuIsOpen => tempMenu.activeInHierarchy;
	public GameObject tempMenu;
	public Slider generalSensitivitySlider;
	public Slider xSensitivitySlider;
	public Slider ySensitivitySlider;
	public Slider headbobSlider;

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
    }

	public void OpenMenu() {
		tempMenu.SetActive(true);
		// Assumes Time.timeScale is always 1 when we're not paused
		Time.timeScale = 0;
		Cursor.visible = true;
		cachedLockMode = Cursor.lockState;
		Cursor.lockState = CursorLockMode.Confined;
	}

	public void CloseMenu(bool restoreTimeScale = true) {
		tempMenu.SetActive(false);
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
