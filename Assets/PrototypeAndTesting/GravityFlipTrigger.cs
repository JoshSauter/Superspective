using System.Collections;
using System.Collections.Generic;
using MagicTriggerMechanics;
using UnityEngine;

[RequireComponent(typeof(MagicTrigger))]
public class GravityFlipTrigger : MonoBehaviour {
    MagicTrigger trigger;
    public Vector3 targetGravityDirection = Vector3.down;
    PlayerMovement playerMovement;

    // Start is called before the first frame update
    void Start() {
        playerMovement = Player.instance.movement;
        trigger = GetComponent<MagicTrigger>();
        trigger.OnMagicTriggerStayOneTime += SwitchGravity;
    }

    void SwitchGravity() {
        SetGravitySilently(Physics.gravity.magnitude * targetGravityDirection.normalized);
    }
    
    /// <summary>
    /// Changes the direction and magnitude of Physics.gravity while maintaining the player's camera position and rotation
    /// </summary>
    /// <param name="targetGravity">Gravity to set Physics.gravity equal to</param>
    public void SetGravitySilently(Vector3 targetGravity, bool flipAngleBetween = false) {
        Physics.gravity = targetGravity;

        float angleBetween = Vector3.Angle(playerMovement.transform.up, -Physics.gravity.normalized);
        if (flipAngleBetween) angleBetween = -angleBetween;
        playerMovement.transform.rotation =
            Quaternion.FromToRotation(playerMovement.transform.up, -Physics.gravity.normalized) *
            playerMovement.transform.rotation;

        PlayerLook playerLook = PlayerLook.instance;
        playerLook.RotationY -= angleBetween;
        // playerLook.rotationY = Mathf.Clamp(playerLook.rotationY, -playerLook.yClamp, playerLook.yClamp);
    }
}
