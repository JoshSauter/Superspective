using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OldPillarMechanics;

[RequireComponent(typeof(PartiallyVisibleObject))]
public class ResetVisibilityStateOnTeleport : MonoBehaviour {
	public TeleportEnter teleporter;
	private PartiallyVisibleObject pvo;

	// Use this for initialization
	void Start () {
		teleporter.OnTeleportSimple += HandleTeleport;
		pvo = GetComponent<PartiallyVisibleObject>();
	}
	
	void HandleTeleport() {
		pvo.ResetVisibilityState();
	}
}
