using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sign : MonoBehaviour {
    public MagicSpawnDespawn magicDespawnTrigger;
    public TeleportEnter teleporter;
    public Vector3 resetPosition;
    public int numMovesBeforeReset;
    public int numMovesLeft;

    // Use this for initialization
    private void OnEnable() {
        teleporter.OnTeleport += MoveByTeleport;
        magicDespawnTrigger.OnMagicTriggerStay += ConditionalDespawn;
    }

    private void OnDisable() {
        teleporter.OnTeleport -= MoveByTeleport;
        magicDespawnTrigger.OnMagicTriggerStay -= ConditionalDespawn;
    }

	public void MoveByTeleport(Teleport teleporter, Collider player) {
		Vector3 teleportDisplacement = teleporter.enter.transform.position - teleporter.exit.transform.position;
		Move(teleportDisplacement);
	}

    public void Move(Vector3 displacement) {
        transform.position -= displacement;
        
        if (--numMovesLeft < 0) {
            transform.position = resetPosition;
            numMovesLeft = numMovesBeforeReset;
        }
    }

    public void ConditionalDespawn(Collider o) {
        if (numMovesLeft > 11) {
            gameObject.SetActive(false);
        }
    }
}
