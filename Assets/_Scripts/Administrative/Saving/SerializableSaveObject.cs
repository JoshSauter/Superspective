using System;

namespace Saving {
    [Serializable]
    public abstract class SerializableSaveObject<T> {
        // Must be overridden by inheriting classes
        protected SerializableSaveObject() { }
        
        public abstract void LoadSave(T saveableScript);
    }
}