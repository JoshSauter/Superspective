namespace Saving {
    public interface ISaveableObject {
        // This ID should come from UniqueId class (should be RequiredComponent on anything saveable)
        string ID { get; }

        bool SkipSave { get; set; }

        void Register();

        SaveObject CreateSave();

        void LoadFromSave(SaveObject save);
    }

    public static class ISaveableObjectExt {
        public static bool HasValidId(this ISaveableObject obj) {
            try {
                string s = obj.ID;

                return !string.IsNullOrEmpty(s);
            }
            catch {
                return false;
            }
        }
    }
}
