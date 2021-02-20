using System;
using PictureTeleportMechanics;
using PortalMechanics;
using SerializableClasses;
using UnityEngine;

namespace LevelSpecific.BehindForkTransition {
    public class TurnPortalBackOnTeleport : MonoBehaviour {
        public SerializableReference<Portal> portalRef;
        Portal portal => portalRef.Reference;
        PictureTeleport pictureTeleport;

        void Start() {
            pictureTeleport = GetComponent<PictureTeleport>();
            pictureTeleport.OnPictureTeleport += () => portal.gameObject.SetActive(true);
        }
    }
}