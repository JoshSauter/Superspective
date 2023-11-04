using System;
using System.Collections.Generic;
using System.Linq;
using PoweredObjects;
using PowerTrailMechanics;
using SuperspectiveUtils;
using UnityEditor;
using UnityEngine;

namespace OneTimeScripts {
    // [InitializeOnLoad]
    // public class MigratePoweredObjects {
    //     [MenuItem("One Time Scripts/Migrate Powered Objects In Selection")]
    //     static void MigratePoweredObjectsInSelection() {
    //         List<TriggerPowerTrailFromButton> scriptsToMigrate = Selection.objects
    //             .OfType<GameObject>()
    //             .Select(o => o.GetComponent<TriggerPowerTrailFromButton>())
    //             .Where(script => script != null)
    //             .ToList();
    //
    //         scriptsToMigrate.ForEach(Migrate);
    //         Debug.Log($"Migrated {scriptsToMigrate.Count} {nameof(TriggerPowerTrailFromButton)}");
    //         scriptsToMigrate.ForEach(UnityEngine.Object.DestroyImmediate);
    //     }
    //
    //     static void Migrate(TriggerPowerTrailFromButton script) {
    //         if (script.button == null) {
    //             Debug.LogError($"No button set on {script.name}");
    //             return;
    //         }
    //
    //         if (script.button.pwr == null) {
    //             script.button.pwr = script.button.GetOrAddComponent<PoweredObject>();
    //         }
    //         PoweredObject buttonPwr = script.button.pwr;
    //
    //         PowerTrail powerTrail = script.GetComponent<PowerTrail>();
    //         if (powerTrail == null) {
    //             Debug.LogError($"No {nameof(PowerTrail)} found on {script.FullPath()}");
    //             return;
    //         }
    //
    //         if (powerTrail.pwr == null) {
    //             powerTrail.pwr = powerTrail.GetOrAddComponent<PoweredObject>();
    //         }
    //         PoweredObject powerTrailPwr = powerTrail.pwr;
    //
    //         switch (script.whatToControl) {
    //             case TriggerPowerTrailFromButton.PowerControl.PowerOnAndOff:
    //                 powerTrailPwr.powerMode = PowerMode.PowerOn | PowerMode.PowerOff;
    //                 break;
    //             case TriggerPowerTrailFromButton.PowerControl.PowerOnOnly:
    //                 powerTrailPwr.powerMode = PowerMode.PowerOn;
    //                 break;
    //             case TriggerPowerTrailFromButton.PowerControl.PowerOffOnly:
    //                 powerTrailPwr.powerMode = PowerMode.PowerOff;
    //                 break;
    //             default:
    //                 throw new ArgumentOutOfRangeException();
    //         }
    //
    //         powerTrailPwr.parentMultiMode = MultiMode.Single;
    //         powerTrailPwr.source = buttonPwr;
    //
    //         // Mark the script gameobject's scene as dirty so that the changes are saved
    //         EditorUtility.SetDirty(script.gameObject);
    //     }
    // }
}
