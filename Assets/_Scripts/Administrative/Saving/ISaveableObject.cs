namespace Saving {
    public interface ISaveableObject {
        // This ID should come from UniqueId class (should be RequiredComponent on anything saveable)
        string ID { get; }

        SerializableSaveObject GetSaveObject();

        void Register();

        void RestoreStateFromSave(SerializableSaveObject savedObject);

        bool SkipSave { get; set; }
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
