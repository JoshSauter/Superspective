using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NovaMenuUI {
    public abstract class Setting : SettingsItem {
        public bool isEnabled = true;
        public string key;

        public abstract bool IsEqual(Setting other);

        public abstract void CopySettingsFrom(Setting other);

        public abstract string PrintValue();

        public abstract void ParseValue(string value);

        public static Setting Copy(Setting other) {
            switch (other) {
                case SmallIntSetting smallIntSetting:
                    return SmallIntSetting.Copy(smallIntSetting);
                case FloatSetting floatSetting:
                    return FloatSetting.Copy(floatSetting);
                case DropdownSetting dropdownSetting:
                    return DropdownSetting.Copy(dropdownSetting);
                case KeybindSetting keybindSetting:
                    return KeybindSetting.Copy(keybindSetting);
                case TextAreaSetting textAreaSetting:
                    return TextAreaSetting.Copy(textAreaSetting);
                case ToggleSetting toggleSetting:
                    return ToggleSetting.Copy(toggleSetting);
                default: throw new ArgumentOutOfRangeException($"{other.GetType()} not handled in switch statement");
            }
        }
    }
}
