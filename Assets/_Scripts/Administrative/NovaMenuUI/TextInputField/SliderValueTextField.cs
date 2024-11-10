using UnityEngine;

namespace NovaMenuUI {
    public class SliderValueTextField : SuperspectiveTextField {
        public int minValue = 0;
        public int maxValue = 100;

        public override string SanitizeInput(string existing, string desired) {
            // Allow empty text block (will be converted to min value after focus lost)
            if (string.IsNullOrEmpty(desired)) {
                return desired;
            }
            if (int.TryParse(desired, out int value)) {
                value = Mathf.Clamp(value, minValue, maxValue);
                return value.ToString();
            }
            else {
                return existing;
            }
        }
    }
}
