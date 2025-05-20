using System;
using System.Collections;
using System.Collections.Generic;
using MagicTriggerMechanics;
using UnityEngine;
using Saving;
using SuperspectiveAttributes;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class ChasmCubeAdjustTrigger : SuperspectiveObject<ChasmCubeAdjustTrigger, ChasmCubeAdjustTrigger.ChasmCubeAdjustTriggerSave> {
    public GlobalMagicTrigger trigger;
    public TriggerOverlapZone cubeForm;

    [SaveUnityObject]
    public PickupObject cubeInZone;

    protected override void Start() {
        base.Start();
        
        trigger.OnMagicTriggerStayOneTime += TriggerChasmCubeAdjust;
    }

    private void FixedUpdate() {
	    if (!trigger.enabled && TryFindCubeInZone(out cubeInZone)) {
		    debug.Log($"Cube found in zone: {cubeInZone.ID}");
		    trigger.enabled = true;
	    }
    }

    private void TriggerChasmCubeAdjust() {
	    if (cubeInZone) {
		    cubeInZone.transform.position = cubeForm.transform.position;
		    cubeInZone.transform.rotation = RightAngleRotations.GetNearest(cubeInZone.transform.rotation);
		    cubeInZone.freezeRotationStateMachine.Set(PickupObject.FreezeRotationState.Frozen);
	    }
	    else {
		    debug.LogError("No cube in zone!", true);
	    }
    }

    private bool TryFindCubeInZone(out PickupObject pickup) {
	    pickup = null;
	    foreach (var c in cubeForm.objectsInZone) {
		    PickupObject maybeCube = c.transform.FindInParentsRecursively<PickupObject>();
		    // Only cubes that are large enough to cover the chasm should be considered
		    if (maybeCube && maybeCube.Scale > 7) {
			    pickup = maybeCube;
			    return true;
		    }
	    }
	    return false;
    }

#region Saving
		[Serializable]
		public class ChasmCubeAdjustTriggerSave : SaveObject<ChasmCubeAdjustTrigger> {

			public ChasmCubeAdjustTriggerSave(ChasmCubeAdjustTrigger script) : base(script) {
			    
			}
		}

        public override void LoadSave(ChasmCubeAdjustTriggerSave save) {
            
        }
#endregion
}
