using Saving;
using UnityEngine;

// GameObjectRef fits into the SaveableObjects framework but does not actually save anything
// It only exists to allow cross-scene references through SerializableReference<GameObjectRef>
[RequireComponent(typeof(UniqueId))]
public class GameObjectRef : SaveableObject<GameObjectRef, GameObjectRef.GameObjectRefSave> {
    public bool setAsInactiveOnStart = false;
    
    UniqueId _id;
    public UniqueId id {
        get {
            if (_id == null) {
                _id = GetComponent<UniqueId>();
            }
            return _id;
        }
    }
    
    // GameObjectRefSave is never actually used since this object always skips saving.
    public class GameObjectRefSave : SerializableSaveObject<GameObjectRef> {
        public GameObjectRefSave(GameObjectRef script) : base(script) {}
        
        public override void LoadSave(GameObjectRef saveableScript) {
            throw new System.NotImplementedException();
        }
    }

    protected override void Awake() {
        base.Awake();
        if (setAsInactiveOnStart) {
            gameObject.SetActive(false);
        }
    }

    public override string ID => id.uniqueId;

    public override bool SkipSave => true;
}