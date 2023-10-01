using System.Collections;
using System.Collections.Generic;
using SuperspectiveUtils;
using TMPro;
using UnityEngine;

public class WalkingSprintingUI : MonoBehaviour {
    public TMP_Text ui;

    private const float minAlpha = 0f;
    private const float maxAlpha = 0.7f;
    private const float lerpSpeed = 3f;
    
    // Update is called once per frame
    void Update() {
        ui.text = Settings.Gameplay.SprintByDefault ? "(Walking)" : "(Sprinting)";
        bool displayText = (PlayerMovement.instance.autoRun ||
            (PlayerMovement.instance.ToggleSprint && (PlayerMovement.instance.sprintIsToggled != Settings.Gameplay.SprintByDefault) ||
            (!PlayerMovement.instance.ToggleSprint && PlayerButtonInput.instance.SprintHeld)));
        ui.color = ui.color.WithAlpha(Mathf.Lerp(ui.color.a, displayText ? maxAlpha : minAlpha, Time.deltaTime * lerpSpeed));
    }
}
