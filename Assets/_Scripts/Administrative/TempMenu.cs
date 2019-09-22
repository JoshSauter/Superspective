using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TempMenu : MonoBehaviour {
	public GameObject tempMenu;
	public Slider generalSensitivitySlider;
	public Slider xSensitivitySlider;
	public Slider ySensitivitySlider;

	private CursorLockMode cachedLockMode;

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
    }

	private void Update() {
		if (PlayerButtonInput.instance.EscapePressed) {
			bool becomeActive = !tempMenu.activeSelf;
			tempMenu.SetActive(becomeActive);
			PlayerLook.instance.frozen = becomeActive;
			Cursor.visible = becomeActive;
			if (becomeActive) {
				cachedLockMode = Cursor.lockState;
				Cursor.lockState = CursorLockMode.Confined;
			}
			else {
				Cursor.lockState = cachedLockMode;
			}
		}
	}
}
