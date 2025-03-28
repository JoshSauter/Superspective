﻿using UnityEngine;

namespace NovaMenuUI {
    public class ToggleSetting : Setting {
        public string name;
        private bool _value;
    
        public bool value {
            get => _value;
            set {
                if (_value == value) {
                    return;
                }
                
                _value = value;
                OnValueChanged?.Invoke(value);
            }
        }
        public bool defaultValue;
        public delegate void ValueChangedAction(bool newValue);
        public event ValueChangedAction OnValueChanged;
    
        public override bool IsEqual(Setting otherSetting) {
            if (otherSetting is not ToggleSetting other) return false;
    
            return name == other.name &&
                   isEnabled == other.isEnabled &&
                   key == other.key &&
                   name == other.name &&
                   value == other.value &&
                   defaultValue == other.defaultValue;
        }
        
        public static ToggleSetting Copy(ToggleSetting from) {
            return new ToggleSetting() {
                key = from.key,
                isEnabled = from.isEnabled,
                name = from.name,
                value = from.value,
                defaultValue = from.defaultValue
            };
        }
    
        public override void CopySettingsFrom(Setting otherSetting) {
            if (otherSetting is not ToggleSetting other) {
                Debug.LogError($"{otherSetting} is not a ToggleSetting");
                return;
            }
    
            key = other.key;
            isEnabled = other.isEnabled;
            name = other.name;
            value = other.value;
            defaultValue = other.defaultValue;
        }
    
        public override string PrintValue() {
            return value.ToString();
        }
    
        public override void ParseValue(string s) {
            if (bool.TryParse(s, out bool result)) {
                value = result;
            }
        }
    
        public static implicit operator bool(ToggleSetting setting) => setting.value;
    }
}
