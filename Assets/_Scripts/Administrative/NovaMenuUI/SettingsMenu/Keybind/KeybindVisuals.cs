using Nova;

public class KeybindVisuals : ItemVisuals {
    public Keybind keybind;
    public TextBlock Name;
    public NovaButton Primary;
    public NovaButton Secondary;

    public void PopulateFrom(KeybindSetting setting) {
        keybind.gameObject.name = $"[Keybind] {setting.name}";
        Name.Text = setting.name;
        Primary.TextBlock.ForEach(tb => tb.Text = setting.value.displayPrimary);
        Secondary.TextBlock.ForEach(tb => tb.Text = setting.value.displaySecondary);

        if (setting.value.displayPrimary == "") {
            Primary.TextBlock.ForEach(tb => tb.Text = "-");
        }
        if (setting.value.displaySecondary == "") {
            Secondary.TextBlock.ForEach(tb => tb.Text = "-");
        }

        keybind.setting = setting;
    }
}
