using System;
using System.Collections;
using SuperspectiveUtils;
using PictureTeleportMechanics;
using PortalMechanics;
using SerializableClasses;
using UnityEngine;

namespace LevelSpecific.BehindForkTransition {
    public class TurnPortalBackOnTeleport : MonoBehaviour {
        public SuperspectiveReference<Portal, Portal.PortalSave> portalRef;
        // GetOrNull since we only refer to the value when we know the reference is valid (when this scene is active)
        Portal portal => portalRef.GetOrNull();
        PictureTeleport pictureTeleport;

        IEnumerator Start() {
            yield return new WaitUntil(() => gameObject.IsInActiveScene());
            
            pictureTeleport = GetComponent<PictureTeleport>();
            pictureTeleport.OnPictureTeleport += () => portal.SetPortalModes(PortalRenderMode.Normal, PortalPhysicsMode.Normal);
        }
    }
}
