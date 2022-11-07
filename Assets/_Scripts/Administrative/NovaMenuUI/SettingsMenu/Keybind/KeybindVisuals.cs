using Nova;

public class KeybindVisuals : ItemVisuals {
    public Keybind keybind;
    public TextBlock Name;
    public NovaButton Primary;
    public NovaButton Secondary;

    public void PopulateFrom(KeybindSetting setting) {
        Name.Text = setting.Name;
        Primary.TextBlock.ForEach(tb => tb.Text = setting.Value.displayPrimary);
        Secondary.TextBlock.ForEach(tb => tb.Text = setting.Value.displaySecondary);

        if (setting.Value.displayPrimary == "") {
            Primary.TextBlock.ForEach(tb => tb.Text = "-");
        }
        if (setting.Value.displaySecondary == "") {
            Secondary.TextBlock.ForEach(tb => tb.Text = "-");
        }

        keybind.setting = setting;
    }
}
