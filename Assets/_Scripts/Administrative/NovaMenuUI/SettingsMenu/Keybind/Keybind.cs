using System;
using System.Collections;
using System.Collections.Generic;
using Library.Functional;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

public class Keybind : UIControl<KeybindVisuals> {
    // static because there can only be one keybind change at a time
    public enum ListeningForKeybindState {
        Idle,
        ListeningForKeybind
    }

    private static StateMachine<ListeningForKeybindState> _state;
    public static StateMachine<ListeningForKeybindState> state => _state ??= StateMachine<ListeningForKeybindState>.CreateWithoutOwner(ListeningForKeybindState.Idle, false, true);

    // Disable inputs for a short time after state goes back to Idle to prevent input fallthrough
    public static bool isListeningForNewKeybind => state == ListeningForKeybindState.ListeningForKeybind ||
                                                   (state.PrevState == ListeningForKeybindState.ListeningForKeybind && state.Time < 0.05f);
    public NovaButton ResetButton;
    public KeybindSetting setting;

    private void OnEnable() {
        Visuals.Primary.OnClick += HandleMappingButtonClicked;
        Visuals.Secondary.OnClick += HandleMappingButtonClicked;
        ResetButton.OnClickSimple += ResetClicked;
    }

    private void OnDisable() {
        Visuals.Primary.OnClick -= HandleMappingButtonClicked;
        Visuals.Secondary.OnClick -= HandleMappingButtonClicked;
        ResetButton.OnClickSimple -= ResetClicked;
    }

    private void ResetClicked() {
        setting.value = new KeyboardAndMouseInput(setting.defaultValue);
        Visuals.PopulateFrom(setting);
    }

    private void HandleMappingButtonClicked(NovaButton button) {
        bool primary = button == Visuals.Primary;
        button.TextBlock.ForEach(t => t.Text = "...");
        StartCoroutine(ListenForNewKeybind(primary));
    }

    IEnumerator ListenForNewKeybind(bool primary) {
        state.Set(ListeningForKeybindState.ListeningForKeybind);

        // Wait a frame to avoid using the left mouse click that triggered the rebind listening
        yield return null;
        
        Either<int, KeyCode> inputPressed = null;
        while (inputPressed == null) {
            for (int i = 0; i < 7; i++) {
                if (Input.GetMouseButtonUp(i)) {
                    inputPressed = new Either<int, KeyCode>(i);
                    break;
                }
            }

            if (inputPressed != null) break;
            
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode))) {
                if (Input.GetKeyUp(key)) {
                    inputPressed = new Either<int, KeyCode>(key);
                    break;
                }
            }

            yield return null;
        }

        NovaButton button = primary ? Visuals.Primary : Visuals.Secondary;
        if (primary) {
            setting.value.SetPrimaryMapping(inputPressed);
        }
        else {
            setting.value.SetSecondaryMapping(inputPressed);
        }
        button.clickState.Set(NovaButton.ClickState.Idle);
        Visuals.PopulateFrom(setting);

        state.Set(ListeningForKeybindState.Idle);
    }
}
