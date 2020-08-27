using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PortalMechanics;
using EpitaphUtils;

public class WhiteRoom3PortalSwitch : MonoBehaviour {
    public Portal[] frontPortals;
    public Portal[] backPortals;

    Portal stairPortal;
    Portal bridgePortal;

    // Start is called before the first frame update
    IEnumerator Start() {
        stairPortal = GetComponent<Portal>();
        yield return new WaitUntil(() => stairPortal.otherPortal != null);
        bridgePortal = stairPortal.otherPortal;

        stairPortal.OnPortalTeleportSimple += (c) => { if (c.TaggedAsPlayer()) EnableDisablePortals(false); };
        bridgePortal.OnPortalTeleportSimple += (c) => { if (c.TaggedAsPlayer()) EnableDisablePortals(true); };
    }

    void EnableDisablePortals(bool frontPortalsActive) {
        foreach (var p in frontPortalsActive ? frontPortals : backPortals) {
            p.gameObject.SetActive(true);
        }
        foreach (var p in frontPortalsActive ? backPortals : frontPortals) {
            p.gameObject.SetActive(false);
        }
    }
}
