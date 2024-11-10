using SuperspectiveUtils;
using UnityEngine;

namespace NovaMenuUI {
    public class FloatSetting : Setting {
        public string name;
        public float value;
        public float defaultValue = 50f;
        public float minValue = 0f;
        public float maxValue = 100f;
    
        public static implicit operator float(FloatSetting s) => s.value;
    
        public static FloatSetting Copy(FloatSetting from) {
            return new FloatSetting() {
                key = from.key,
                isEnabled = from.isEnabled,
                name = from.name,
                value = from.value,
                defaultValue = from.defaultValue,
                minValue = from.minValue,
                maxValue = from.maxValue
            };
        }
    
        public override bool IsEqual(Setting otherSetting) {
            if (otherSetting is not FloatSetting other) return false;
    
            return name == other.name &&
                   isEnabled == other.isEnabled &&
                   value.IsApproximately(other.value) &&
                   defaultValue.IsApproximately(other.defaultValue) &&
                   minValue.IsApproximately(other.minValue) &&
                   maxValue.IsApproximately(other.maxValue);
        }
    
        public override void CopySettingsFrom(Setting otherSetting) {
            if (otherSetting is not FloatSetting other) {
                Debug.LogError($"{otherSetting} is not a FloatSetting");
                return;
            }
    
            name = other.name;
            isEnabled = other.isEnabled;
            value = other.value;
            defaultValue = other.defaultValue;
            minValue = other.minValue;
            maxValue = other.maxValue;
        }
    
        public override string PrintValue() {
            return value.ToString("F2");
        }
    
        public override void ParseValue(string value) {
            if (float.TryParse(value, out float result)) {
                this.value = result;
            }
        }
    }
}
