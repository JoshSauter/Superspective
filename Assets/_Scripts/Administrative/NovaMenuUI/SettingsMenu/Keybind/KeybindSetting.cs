using System;
using System.Collections.Generic;
using SuperspectiveUtils;
using UnityEngine;

public class KeybindSetting : Setting {
    public string Name;
    public KeyboardAndMouseInput Value;
    public KeyboardAndMouseInput DefaultValue;

    public bool Pressed => Value.Pressed;
    public bool Released => Value.Released;
    public bool Held => Value.Held;

    public KeybindSetting() {}

    public KeybindSetting(string name, KeyboardAndMouseInput defaultValue) {
        Name = name;
        key = $"KB_{name}";
        Value = defaultValue;
        DefaultValue = defaultValue;
    }
    
    public static KeybindSetting Copy(KeybindSetting from) {
        
        return new KeybindSetting() {
            key = from.key,
            isEnabled = from.isEnabled,
            Name = from.Name,
            Value = new KeyboardAndMouseInput(from.Value),
            DefaultValue = new KeyboardAndMouseInput(from.DefaultValue)
        };
    }
    
    public override bool IsEqual(Setting otherSetting) {
        if (otherSetting is not KeybindSetting other) return false;

        return Name == other.Name &&
               isEnabled == other.isEnabled &&
               key == other.key &&
               Value.displayPrimary == other.Value.displayPrimary &&
               Value.displaySecondary == other.Value.displaySecondary &&
               DefaultValue.displayPrimary == other.DefaultValue.displayPrimary &&
               DefaultValue.displaySecondary == other.DefaultValue.displaySecondary;
    }

    public override void CopySettingsFrom(Setting otherSetting) {
        if (otherSetting is not KeybindSetting other) {
            Debug.LogError($"{otherSetting} is not a KeybindSetting");
            return;
        }

        Name = other.Name;
        isEnabled = other.isEnabled;

        Value = new KeyboardAndMouseInput(other.Value);
        DefaultValue = new KeyboardAndMouseInput(other.DefaultValue);
    }
}