namespace NovaMenuUI {
    public class DropdownOption {
        public object Datum;
        public string DisplayName;
    
        public static DropdownOption Of(string name, object datum) {
            return new DropdownOption() {
                Datum = datum,
                DisplayName = name
            };
        }
    }
}
