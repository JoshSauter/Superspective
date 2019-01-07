using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class VolumetricPortalTrigger : MagicSpawnDespawn {
	public PortalContainer portal;
	PortalTeleporter portalTeleporter;

	// Use this for initialization
	IEnumerator Start () {
		base.Start();

		yield return null;

		portalTeleporter = portal.teleporter;
		GameObject volumetricPortal = portal.volumetricPortal;
		objectsToEnable = new GameObject[] { volumetricPortal };
		objectsToDisable = new GameObject[0];
		scriptsToEnable = new MonoBehaviour[0];
		scriptsToDisable = new MonoBehaviour[0];

		InitializeCollider();
		OnMagicTriggerExit += TriggerExit;
		gameObject.name = "VolumetricPortalTrigger";
	}

	void InitializeCollider() {
		Collider copyTarget = portalTeleporter.GetComponent<Collider>();
		if (copyTarget.GetType() == typeof(MeshCollider)) {
			gameObject.PasteComponent(copyTarget as MeshCollider);
		}
		else {
			gameObject.PasteComponent(copyTarget as BoxCollider);
		}
		targetDirection = portalTeleporter.teleporter.trigger.targetDirection.normalized;
		portalTeleporter.teleporter.OnTeleportSimple += OnTeleport;
		triggerCondition = TriggerConditionType.PlayerMovingDirection;
		playerFaceThreshold = 0.01f;
		transform.position -= targetDirection * 0.4f;
		portalTeleporter.transform.position += targetDirection * 0.5f;
	}

	void TriggerExit(Collider c) {
		DisableEnabledObjects();
	}

	void OnTeleport() {
		DisableEnabledObjects();
	}

	void DisableEnabledObjects() {
		foreach (var obj in objectsToEnable) {
			obj.SetActive(false);
		}
	}
}
