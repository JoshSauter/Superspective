using System;
using System.Collections;
using System.Collections.Generic;
using Library.Functional;
using SuperspectiveUtils;
using UnityEngine;

public class Keybind : UIControl<KeybindVisuals> {
    // static because there can only be one keybind change at a time
    public static bool isListeningForNewKeybind = false;
    public KeybindSetting setting;

    // Start is called before the first frame update
    void Start() {
        Visuals.Primary.OnClick += HandleMappingButtonClicked;
        Visuals.Secondary.OnClick += HandleMappingButtonClicked;
    }

    private void HandleMappingButtonClicked(NovaButton button) {
        bool primary = button == Visuals.Primary;
        button.TextBlock.ForEach(t => t.Text = "...");
        StartCoroutine(ListenForNewKeybind(primary));
    }

    IEnumerator ListenForNewKeybind(bool primary) {
        isListeningForNewKeybind = true;

        Either<int, KeyCode> inputPressed = null;
        while (inputPressed == null) {
            for (int i = 0; i < 7; i++) {
                if (Input.GetMouseButtonDown(i)) {
                    inputPressed = new Either<int, KeyCode>(i);
                    break;
                }
            }

            if (inputPressed != null) break;
            
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode))) {
                if (Input.GetKeyDown(key)) {
                    inputPressed = new Either<int, KeyCode>(key);
                    break;
                }
            }

            yield return null;
        }

        NovaButton button = primary ? Visuals.Primary : Visuals.Secondary;
        if (primary) {
            setting.Value.SetPrimaryMapping(inputPressed);
        }
        else {
            setting.Value.SetSecondaryMapping(inputPressed);
        }
        button.buttonState.Set(NovaButton.ButtonState.Idle);
        Visuals.PopulateFrom(setting);
        
        isListeningForNewKeybind = false;
    }
}
