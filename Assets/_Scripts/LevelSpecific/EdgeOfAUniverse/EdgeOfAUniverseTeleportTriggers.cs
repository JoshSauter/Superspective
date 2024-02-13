using System;
using System.Collections;
using System.Collections.Generic;
using MagicTriggerMechanics;
using UnityEngine;
using Saving;
using StateUtils;

[RequireComponent(typeof(UniqueId))]
public class EdgeOfAUniverseTeleportTriggers : SaveableObject<EdgeOfAUniverseTeleportTriggers, EdgeOfAUniverseTeleportTriggers.EdgeOfAUniverseTeleportTriggersSave> {
    public MagicTrigger enterEndgameWalkwayTrigger;
    public TeleportEnter firstTeleport;
    private float secondTeleportThreshold = 32; // Distance player has to move in the X direction to be manually teleported
    public Transform secondTeleportExit; // Player teleported manually when they go far enough in the X direction
    public TeleportEnter thirdTeleport;
    private float fourthTeleportThreshold = 32; // Distance player has to move in the X direction to be manually teleported
    public Transform fourthTeleportExit; // Player teleported manually when they go far enough in the X direction

    private float PlayerX => Player.instance.transform.position.x;
    private float playerLastTeleportedX;
    
    public enum State {
        NotStarted,
        EnteredEndgameWalkway,
        TeleportedOnce,
        TeleportedTwice,
        TeleportedThrice,
        TeleportedFourTimes
    }

    public StateMachine<State> state;

    protected override void Awake() {
        base.Awake();

        state = this.StateMachine(State.NotStarted);
    }

    protected override void Start() {
        base.Start();

        enterEndgameWalkwayTrigger.OnMagicTriggerStayOneTime += () => state.Set(State.EnteredEndgameWalkway);
        enterEndgameWalkwayTrigger.OnNegativeMagicTriggerStayOneTime += () => state.Set(State.NotStarted);

        // Record player X position when player teleports away from end walkway
        state.AddTrigger((e) => e is State.TeleportedOnce or State.TeleportedThrice, () => playerLastTeleportedX = PlayerX);
        // Teleport the player to the second teleport exit position, facing the end of the walkway
        state.AddTrigger(State.TeleportedTwice, () => TeleportPlayerToExitTransform(secondTeleportExit));
        // Teleport the player to the fourth teleport exit position, facing the end of the walkway
        state.AddTrigger(State.TeleportedFourTimes, () => TeleportPlayerToExitTransform(fourthTeleportExit));

        state.OnStateChangeSimple += () => {
            PlayerMovement.EndGameMovement endgameMovementState;
            switch (state.state) {
                case State.NotStarted:
                    endgameMovementState = PlayerMovement.EndGameMovement.NotStarted;
                    break;
                case State.EnteredEndgameWalkway:
                    endgameMovementState = PlayerMovement.EndGameMovement.Walking;
                    break;
                case State.TeleportedOnce:
                case State.TeleportedTwice:
                    endgameMovementState = PlayerMovement.EndGameMovement.HorizontalInputMovesPlayerForward;
                    break;
                case State.TeleportedThrice:
                    endgameMovementState = PlayerMovement.EndGameMovement.AllInputMovesPlayerForward;
                    break;
                case State.TeleportedFourTimes:
                    endgameMovementState = PlayerMovement.EndGameMovement.AllInputDisabled;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            PlayerMovement.instance.endGameMovement = endgameMovementState;
        };

            // Set state when player enters first or third teleport trigger
        firstTeleport.OnTeleportSimple += () => state.Set(State.TeleportedOnce);
        thirdTeleport.OnTeleportSimple += () => state.Set(State.TeleportedThrice);
    }

    private void TeleportPlayerToExitTransform(Transform exit) {
        Vector3 playerPos = Player.instance.transform.position;
        const float maxRaycastDistance = 20f;
        Ray ray = new Ray(playerPos, -Player.instance.transform.up * 20);
        RaycastHit rayHit = default;
        float playerToFloorDistance = Physics.Raycast(ray, out RaycastHit raycastHit, maxRaycastDistance, SuperspectivePhysics.PhysicsRaycastLayerMask) ? Vector3.Distance(playerPos, raycastHit.point) : maxRaycastDistance;
        Player.instance.transform.position = exit.position + playerToFloorDistance * exit.up;
        Player.instance.transform.forward = exit.right;
        Player.instance.PlayerCam.transform.forward = exit.right;
        Player.instance.cameraFollow.RecalculateWorldPositionLastFrame();
    }

    void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;

        switch (state.state) {
            case State.NotStarted:
            case State.EnteredEndgameWalkway:
                break;
            case State.TeleportedOnce:
                if (PlayerX - playerLastTeleportedX > secondTeleportThreshold) {
                    state.Set(State.TeleportedTwice);
                }
                break;
            case State.TeleportedTwice:
                break;
            case State.TeleportedThrice:
                if (PlayerX - playerLastTeleportedX > fourthTeleportThreshold) {
                    state.Set(State.TeleportedFourTimes);
                }
                break;
            case State.TeleportedFourTimes:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
#region Saving
		[Serializable]
		public class EdgeOfAUniverseTeleportTriggersSave : SerializableSaveObject<EdgeOfAUniverseTeleportTriggers> {
            private StateMachine<State>.StateMachineSave stateSave;
            
			public EdgeOfAUniverseTeleportTriggersSave(EdgeOfAUniverseTeleportTriggers script) : base(script) {
                this.stateSave = script.state.ToSave();
			}

			public override void LoadSave(EdgeOfAUniverseTeleportTriggers script) {
                script.state.LoadFromSave(this.stateSave);
			}
		}
#endregion
}
