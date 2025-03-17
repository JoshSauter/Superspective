using System;
using MagicTriggerMechanics;
using PortalMechanics;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class InfiniteHallwayNew : SuperspectiveObject<InfiniteHallwayNew, InfiniteHallwayNew.InfiniteHallwayNewSave> {
    // The bottom portal (in the hallway) jumps around depending on the state of the hallway
    private readonly Vector3 BOT_PORTAL_ENTRANCE_POSITION = Vector3.right * 2;
    private readonly Vector3 BOT_PORTAL_ENTRANCE_DIRECTION = Vector3.left;
    private readonly Vector3 BOT_PORTAL_CONNECTED_EXITS_POSITION = Vector3.right * 100;
    private readonly Vector3 BOT_PORTAL_CONNECTED_EXITS_DIRECTION = Vector3.right;
    
    private const int MAX_INF_TELEPORT_FOR_TEXT = 7; // How many times we can move the text before we should just turn it off
    
    public TeleportEnter infiniteTeleporter;
    public GlobalMagicTrigger goingNowhereText;
    public int timesInfTeleported = 0;
    
    public Portal bottomPortal;
    public Portal topPortal;

    public MeshRenderer inArrowsUpper;

    public GameObject correctAnswerTriggers;
    
    public enum State {
        Inactive,
        InfiniteHallway_Bottom,
        InfiniteHallway_Top,
        ConnectedExits
    }
    public StateMachine<State> state;

    protected override void Awake() {
        base.Awake();
        
        InitializeStateMachine(state);
        infiniteTeleporter.OnTeleport += MoveText;
    }
    
    void MoveText(Collider teleportEnter, Collider teleportExit, GameObject player) {
        goingNowhereText.transform.position += (teleportExit.transform.position - teleportEnter.transform.position);
        timesInfTeleported++;
        if (timesInfTeleported >= MAX_INF_TELEPORT_FOR_TEXT) {
            // Turn on the GlobalMagicTrigger that will disable the text when the player looks away
            goingNowhereText.enabled = true;
        }
    }

    // Provided methods are called by Unity Events set up on the triggers in the editor
    public void SetAsInactive() {
        if (state == State.ConnectedExits) state.Set(State.Inactive);
    }
    public void SetAsInactiveFromInfiniteHallway_Bottom() {
        if (state == State.InfiniteHallway_Bottom) state.Set(State.Inactive);
    }
    public void SetAsInfiniteHallway_Bottom() {
        // Ignore the player passing through the bottom entrance if we arrived from the top
        if (state == State.Inactive) state.Set(State.InfiniteHallway_Bottom);
    }
    public void SetAsInfiniteHallway_Top() {
        if (state == State.Inactive) state.Set(State.InfiniteHallway_Top);
    }
    public void SetAsConnectedExits() {
        state.Set(State.ConnectedExits);
    }

    private void InitializeStateMachine(State startingState) {
        state = this.StateMachine(startingState);

        topPortal.OnPortalTeleportPlayerSimple += SetAsInfiniteHallway_Top;

        state.OnStateChangeSimple += () => {
            infiniteTeleporter.enabled = state != State.ConnectedExits;
            inArrowsUpper.enabled = state == State.ConnectedExits;
            correctAnswerTriggers.SetActive(state == State.ConnectedExits);
            
            switch (state.State) {
                case State.Inactive:
                    // When the infinite hallway is not in use by the player, and is not connected,
                    // we place the bottom portal at the entrance (but turn off its rendering and physics) so that
                    // the top portal also looks like an infinite hallway.
                    bottomPortal.transform.localPosition = BOT_PORTAL_ENTRANCE_POSITION;
                    bottomPortal.transform.forward = BOT_PORTAL_ENTRANCE_DIRECTION;
                    bottomPortal.SetPortalModes(PortalRenderMode.Invisible, PortalPhysicsMode.None);
                    break;
                case State.InfiniteHallway_Bottom:
                    bottomPortal.transform.localPosition = BOT_PORTAL_ENTRANCE_POSITION;
                    bottomPortal.transform.forward = BOT_PORTAL_ENTRANCE_DIRECTION;
                    bottomPortal.SetPortalModes(PortalRenderMode.Invisible, PortalPhysicsMode.None);
                    break;
                case State.InfiniteHallway_Top:
                    bottomPortal.transform.localPosition = BOT_PORTAL_ENTRANCE_POSITION;
                    bottomPortal.transform.forward = BOT_PORTAL_ENTRANCE_DIRECTION;
                    bottomPortal.SetPortalModes(PortalRenderMode.Normal, PortalPhysicsMode.Normal);
                    break;
                case State.ConnectedExits:
                    if (state.PrevState == State.InfiniteHallway_Top) {
                        // We must flip the player around when the exits are connected from the top inf hallway
                        Transform playerTransform = Player.instance.transform;
                        Vector3 localPos = bottomPortal.transform.InverseTransformPoint(playerTransform.position);
                        Quaternion localRot = Quaternion.Inverse(bottomPortal.transform.rotation) * playerTransform.rotation;
                        Vector3 localVelocity = bottomPortal.transform.InverseTransformDirection(PlayerMovement.instance.thisRigidbody.velocity);
                        
                        bottomPortal.transform.localPosition = BOT_PORTAL_CONNECTED_EXITS_POSITION;
                        bottomPortal.transform.forward = BOT_PORTAL_CONNECTED_EXITS_DIRECTION;
                        bottomPortal.SetPortalModes(PortalRenderMode.Normal, PortalPhysicsMode.Normal);
                        
                        playerTransform.position = bottomPortal.transform.TransformPoint(localPos);
                        playerTransform.rotation = bottomPortal.transform.rotation * localRot;
                        PlayerMovement.instance.thisRigidbody.velocity = bottomPortal.transform.TransformDirection(localVelocity);
                        
                        Player.instance.cameraFollow.RecalculateWorldPositionLastFrame();
                    }
                    else {
                        bottomPortal.transform.localPosition = BOT_PORTAL_CONNECTED_EXITS_POSITION;
                        bottomPortal.transform.forward = BOT_PORTAL_CONNECTED_EXITS_DIRECTION;
                        bottomPortal.SetPortalModes(PortalRenderMode.Normal, PortalPhysicsMode.Normal);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };
    }

    void TopPortalEntered() {
        state.Set(State.InfiniteHallway_Top);
        
    }
    
#region Saving
		[Serializable]
		public class InfiniteHallwayNewSave : SaveObject<InfiniteHallwayNew> {
            public StateMachine<State>.StateMachineSave stateSave;
            
			public InfiniteHallwayNewSave(InfiniteHallwayNew script) : base(script) {
                this.stateSave = script.state.ToSave();
			}
		}

        public override void LoadSave(InfiniteHallwayNewSave save) {
            state.LoadFromSave(save.stateSave);
        }
#endregion
}
