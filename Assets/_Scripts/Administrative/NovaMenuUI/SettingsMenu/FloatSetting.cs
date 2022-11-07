using SuperspectiveUtils;
using UnityEngine;

public class FloatSetting : Setting {
    public string Name;
    public float Value;
    public float DefaultValue = 50f;
    public float MinValue = 0f;
    public float MaxValue = 100f;

    public static implicit operator float(FloatSetting s) => s.Value;

    public static FloatSetting Copy(FloatSetting from) {
        return new FloatSetting() {
            key = from.key,
            isEnabled = from.isEnabled,
            Name = from.Name,
            Value = from.Value,
            DefaultValue = from.DefaultValue,
            MinValue = from.MinValue,
            MaxValue = from.MaxValue
        };
    }

    public override bool IsEqual(Setting otherSetting) {
        if (otherSetting is not FloatSetting other) return false;

        return Name == other.Name &&
               isEnabled == other.isEnabled &&
               Value.IsApproximately(other.Value) &&
               DefaultValue.IsApproximately(other.DefaultValue) &&
               MinValue.IsApproximately(other.MinValue) &&
               MaxValue.IsApproximately(other.MaxValue);
    }

    public override void CopySettingsFrom(Setting otherSetting) {
        if (otherSetting is not FloatSetting other) {
            Debug.LogError($"{otherSetting} is not a FloatSetting");
            return;
        }

        Name = other.Name;
        isEnabled = other.isEnabled;
        Value = other.Value;
        DefaultValue = other.DefaultValue;
        MinValue = other.MinValue;
        MaxValue = other.MaxValue;
    }

    public override string PrintValue() {
        return Value.ToString("F2");
    }
}
