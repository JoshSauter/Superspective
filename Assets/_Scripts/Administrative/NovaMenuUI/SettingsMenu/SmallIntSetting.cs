using SuperspectiveUtils;
using UnityEngine;

public class SmallIntSetting : FloatSetting {
    public static SmallIntSetting Copy(SmallIntSetting from) {
        return new SmallIntSetting() {
            key = from.key,
            isEnabled = from.isEnabled,
            Name = from.Name,
            Value = from.Value,
            DefaultValue = from.DefaultValue,
            MinValue = from.MinValue,
            MaxValue = from.MaxValue
        };
    }
}