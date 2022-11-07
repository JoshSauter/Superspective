public class HeaderSettingsItem : SettingsItem {
    public string Name;

    public static HeaderSettingsItem Of(string name) {
        return new HeaderSettingsItem() {
            Name = name
        };
    }
}
