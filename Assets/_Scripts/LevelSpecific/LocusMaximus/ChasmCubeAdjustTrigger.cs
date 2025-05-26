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
	    if (!trigger.enabled && TryFindLargeCubeInZone(cubeForm, out cubeInZone)) {
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

    public static bool TryFindLargeCubeInZone(TriggerOverlapZone triggerOverlap, out PickupObject pickup) {
	    pickup = null;
	    foreach (var pickupObject in triggerOverlap.pickupObjectsInZone.Values) {
		    // Only cubes that are large enough to cover the chasm should be considered
		    if (pickupObject.Scale > 7) {
			    pickup = pickupObject;
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
