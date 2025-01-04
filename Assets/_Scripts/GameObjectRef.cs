using Saving;
using UnityEngine;

// GameObjectRef fits into the SaveableObjects framework but does not actually save anything
// It only exists to allow cross-scene references through SerializableReference<GameObjectRef>
// TODO: Deprecate this. SuperspectiveObject contains this functionality now
[RequireComponent(typeof(UniqueId))]
public class GameObjectRef : SuperspectiveObject<GameObjectRef, GameObjectRef.GameObjectRefSave> {
    public bool setAsInactiveOnStart = false;

    public override void LoadSave(GameObjectRefSave save) {
        throw new System.NotImplementedException();
    }

    // GameObjectRefSave is never actually used since this object always skips saving.
    public class GameObjectRefSave : SaveObject<GameObjectRef> {
        public GameObjectRefSave(GameObjectRef script) : base(script) {}
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