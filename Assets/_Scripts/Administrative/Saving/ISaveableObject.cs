namespace Saving {
    public interface ISaveableObject {
        // This ID should come from UniqueId class (should be RequiredComponent on anything saveable)
        string ID { get; }

        object GetSaveObject();

        void Register();

        void RestoreStateFromSave(object savedObject);

        bool SkipSave { get; set; }
    }
}