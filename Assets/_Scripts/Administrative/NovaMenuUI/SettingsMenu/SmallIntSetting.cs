namespace NovaMenuUI {
    public class SmallIntSetting : FloatSetting {
        public static SmallIntSetting Copy(SmallIntSetting from) {
            return new SmallIntSetting() {
                key = from.key,
                isEnabled = from.isEnabled,
                name = from.name,
                value = from.value,
                defaultValue = from.defaultValue,
                minValue = from.minValue,
                maxValue = from.maxValue
            };
        }
    
        public override string PrintValue() {
            return value.ToString("F0");
        }

        public override void ParseValue(string value) {
            if (float.TryParse(value, out float result)) {
                base.value = (int)result;
            }
        }
    }
}
