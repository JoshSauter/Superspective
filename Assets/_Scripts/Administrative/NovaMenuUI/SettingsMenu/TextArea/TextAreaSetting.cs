namespace NovaMenuUI {
    public class TextAreaSetting : Setting {
        public string Name;
        public string Text;
        public string PlaceholderText;

        public override bool IsEqual(Setting otherSetting) {
            if (otherSetting is not TextAreaSetting other) return false;

            return other.Text == Text && other.PlaceholderText == PlaceholderText;
        }
    
        public static TextAreaSetting Copy(TextAreaSetting from) {
            return new TextAreaSetting() {
                key = from.key,
                isEnabled = from.isEnabled,
                Name = from.Name,
                Text = from.Text,
                PlaceholderText = from.PlaceholderText
            };
        }

        public override void CopySettingsFrom(Setting otherSetting) {
            if (otherSetting is not TextAreaSetting other) return;

            Text = other.Text;
            PlaceholderText = other.PlaceholderText;
        }

        public override string PrintValue() {
            return Text;
        }

        public override void ParseValue(string value) {
            Text = value;
        }
    }
}
