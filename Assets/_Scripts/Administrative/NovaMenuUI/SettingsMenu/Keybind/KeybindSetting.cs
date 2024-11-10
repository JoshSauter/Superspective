using Library.Functional;
using SuperspectiveUtils;
using UnityEngine;

namespace NovaMenuUI {
    public class KeybindSetting : Setting {
        public string name;
        public KeyboardAndMouseInput value;
        public KeyboardAndMouseInput defaultValue;

        public bool Pressed => value.Pressed;
        public bool Released => value.Released;
        public bool Held => value.Held;

        public KeybindSetting() {}

        public KeybindSetting(string name, KeyboardAndMouseInput defaultValue) {
            this.name = name;
            key = $"KB_{name}";
            value = defaultValue;
            this.defaultValue = defaultValue;
        }
        
        public static KeybindSetting Copy(KeybindSetting from) {
            return new KeybindSetting() {
                key = from.key,
                isEnabled = from.isEnabled,
                name = from.name,
                value = new KeyboardAndMouseInput(from.value),
                defaultValue = new KeyboardAndMouseInput(from.defaultValue)
            };
        }
        
        public override bool IsEqual(Setting otherSetting) {
            if (otherSetting is not KeybindSetting other) return false;

            return name == other.name &&
                   isEnabled == other.isEnabled &&
                   key == other.key &&
                   value.displayPrimary == other.value.displayPrimary &&
                   value.displaySecondary == other.value.displaySecondary &&
                   defaultValue.displayPrimary == other.defaultValue.displayPrimary &&
                   defaultValue.displaySecondary == other.defaultValue.displaySecondary;
        }

        public override void CopySettingsFrom(Setting otherSetting) {
            if (otherSetting is not KeybindSetting other) {
                Debug.LogError($"{otherSetting} is not a KeybindSetting");
                return;
            }

            name = other.name;
            isEnabled = other.isEnabled;

            value = new KeyboardAndMouseInput(other.value);
            defaultValue = new KeyboardAndMouseInput(other.defaultValue);
        }

        public override string PrintValue() {
            return $"{value.displayPrimary}{Option<string>.Of(value.displaySecondary).FilterNot(string.IsNullOrEmpty).Map(v => $" | {v}").GetOrElse("")}";
        }

        public override void ParseValue(string s) {
            var parts = s.Split(" | ");
            
            Either<int, KeyCode> FromString(string s) {
                switch (s) {
                    case "":
                        return null;
                    case "Left Mouse":
                        return new Either<int, KeyCode>(0);
                    case "Right Mouse":
                        return new Either<int, KeyCode>(1);
                    case "Middle Mouse":
                        return new Either<int, KeyCode>(2);
                    default: {
                        if (s.StartsWith("MB") && int.TryParse(s.Substring(2), out int mouseButton)) {
                            // We display the mouse button as 1 higher so subtract 1 (e.g. "MB4" actually has a mouse button value of 3)
                            return new Either<int, KeyCode>(mouseButton - 1);
                        }
                        else if (KeyCode.TryParse(s.StripWhitespace(), out KeyCode key)) {
                            return new Either<int, KeyCode>(key);
                        }
                        else {
                            return null;
                        }
                    }
                }
            }

            Either<int, KeyCode> primaryKeybind = null;
            Either<int, KeyCode> secondaryKeybind = null;
            
            if (parts.Length > 0) {
                primaryKeybind = FromString(parts[0]);
            }
            if (parts.Length > 1) {
                secondaryKeybind = FromString(parts[1]);
            }

            value.SetMapping(primaryKeybind, secondaryKeybind);
        }
    }
}
