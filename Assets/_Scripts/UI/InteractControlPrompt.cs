using System.Collections;
using System.Collections.Generic;
using LevelManagement;
using SerializableClasses;
using UnityEngine;

public class InteractControlPrompt : ControlPrompt {
    public SerializableReference<PickupObject, PickupObject.PickupObjectSave> cubeToBePickedUp;

    private bool cubeHasEverBeenHeld = false;
    private bool CubeIsHeld => LevelManager.instance.loadedSceneNames.Contains(Levels.ForkWhiteRoom.ToName()) && (cubeToBePickedUp.GetOrNull()?.isHeld ?? false);
    
    protected override bool CanStopDisplaying => base.CanStopDisplaying && cubeHasEverBeenHeld;

    protected override void Update() {
        base.Update();

        if (CubeIsHeld) {
            cubeHasEverBeenHeld = true;
        }
    }
}
