namespace Saving {
    public interface SaveableObject {
        // This ID should come from UniqueId class (should be RequiredComponent on anything saveable)
        string ID { get; }

        object GetSaveObject();

        void LoadFromSavedObject(object savedObject);

		bool HasValidId() {
            try {
                string s = ID;
                return true;
            }
            catch {
                return false;
			}
		}
	}
}