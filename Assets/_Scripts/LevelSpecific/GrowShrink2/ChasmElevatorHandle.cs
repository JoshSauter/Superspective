using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using UnityEngine;
using Saving;
using SerializableClasses;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine.Serialization;

[RequireComponent(typeof(UniqueId), typeof(InteractableObject))]
public class ChasmElevatorHandle : SuperspectiveObject<ChasmElevatorHandle, ChasmElevatorHandle.ChasmElevatorHandleSave>, AudioJobOnGameObject {
    [FormerlySerializedAs("handleRenderer")]
    [SerializeField]
    private Renderer _handleRenderer;
    private SuperspectiveRenderer handleRenderer;

    public Transform handleClickbox;
    
    private InteractableObject interact;
    private List<Collider> handleColliders;

    public HandleState startingState = HandleState.Up;
    public enum HandleState : byte {
        Down,
        HeldByPlayer,
        Up
    }
    public StateMachine<HandleState> handleState;

    private const float GRACE_PERIOD = 0.75f; // Minimum time before the handle will snap to up or down (to allow time to leave the top or bottom)
    private const float HANDLE_LERP_SPEED = 2f;
    private const float MAX_OFFSET = 0.25f;

    private const float HOLD_THRESHOLD = 0.25f; // How long the player must hold the mouse down before it's considered a hold
    private const float AUDIO_PLAY_THRESHOLD = 0.11f; // How far the handle must move before playing audio. Lower value means more clicks
    
    private HandleState CloserHandleState => CurHeight >= 0 ? HandleState.Up : HandleState.Down;

    private AudioManager.AudioJob crankAudio;

    private float desiredHeight;
    private float CurHeight {
        get => transform.localPosition.y;
        set {
            float desiredY = Mathf.Clamp(value, -MAX_OFFSET, MAX_OFFSET);
            transform.localPosition = transform.localPosition.WithY(desiredY);

            if (GameManager.instance.gameHasLoaded && Mathf.Abs(desiredY - heightAudioLastPlayedAt) > AUDIO_PLAY_THRESHOLD) {
                heightAudioLastPlayedAt = desiredY;
                crankAudio = AudioManager.instance.PlayOnGameObject(AudioName.LeverCrank, ID, this, true);
            }
        }
    }

    private float heightAudioLastPlayedAt;
    
    protected override void Awake() {
        base.Awake();
        handleColliders = GetComponentsInChildren<Collider>().ToList();
        handleRenderer = _handleRenderer.GetOrAddComponent<SuperspectiveRenderer>();
        
        handleState = this.StateMachine(startingState);
        
        // Automatically release the handle when it's moved all the way up or down
        handleState.AddStateTransition(HandleState.HeldByPlayer,
            () => CloserHandleState,
            () => handleState.Time > GRACE_PERIOD && desiredHeight is >= MAX_OFFSET or <= -MAX_OFFSET);
        
        // Turn off handle colliders while the player is holding it to avoid interfering with raycasts to get mouse position
        handleState.AddTrigger(HandleState.HeldByPlayer, () => handleColliders.ForEach(c => c.enabled = false));
        handleState.AddTrigger(intoState => intoState is HandleState.Down or HandleState.Up, () => handleColliders.ForEach(c => c.enabled = true));
        
        // Set the desired height to the max offset in the appropriate direction when the player is not holding it
        handleState.AddTrigger(HandleState.Down, () => desiredHeight = -MAX_OFFSET);
        handleState.AddTrigger(HandleState.Up, () => desiredHeight = MAX_OFFSET);

        // Play audio when the handle state changes
        handleState.OnStateChangeSimple += () => {
            switch (handleState.State) {
                case HandleState.HeldByPlayer:
                    AudioManager.instance.PlayOnGameObject(AudioName.LeverSnapUp, ID, this);
                    break;
                case HandleState.Down:
                case HandleState.Up:
                    StopCrankAudio();
                    AudioManager.instance.PlayOnGameObject(AudioName.LeverSnapDown, ID, this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };
        
        // Set the desired height based on the mouse position when the player is holding the handle
        handleState.WithUpdate(HandleState.HeldByPlayer, _ => {
            if (PlayerButtonInput.instance.InteractHeld) {
                desiredHeight = GetDesiredHeight();
            }
            else {
                handleState.Set(DetermineUpOrDownStateAfterClickRelease());
            }
        });
    }

    private void StopCrankAudio() {
        if (crankAudio != null) {
            crankAudio.Stop();
            crankAudio = null;
        }
    }

    void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;
        
        CurHeight = Mathf.Lerp(CurHeight, desiredHeight, HANDLE_LERP_SPEED * Time.deltaTime);

        handleClickbox.position = transform.parent.position;
        
        // Color the handle from red to green depending on position
        Color desiredColor = GetDesiredColor();
        handleRenderer.SetMainColor(desiredColor);
        handleRenderer.SetColor("_EmissionColor", desiredColor);
    }

    protected override void Start() {
        base.Start();

        interact = this.GetOrAddComponent<InteractableObject>();
        interact.OnLeftMouseButtonDown += OnLeftMouseButtonDown;

        CurHeight = startingState == HandleState.Down ? -MAX_OFFSET : MAX_OFFSET;
        desiredHeight = CurHeight;
    }

    private void OnLeftMouseButtonDown() {
        handleState.Set(HandleState.HeldByPlayer);
    }

    private HandleState DetermineUpOrDownStateAfterClickRelease() {
        if (handleState.Time < HOLD_THRESHOLD) {
            return GetDesiredHeight() > 0 ? HandleState.Up : HandleState.Down;
        }
        else return CloserHandleState;
    }

    float GetDesiredHeight() {
        SuperspectiveRaycast raycast = Interact.instance.GetRaycastHits();
        Vector3 mouseLocation = raycast.DidHitObject ? raycast.FirstObjectHit.point : raycast.FinalPosition;
        Vector3 localMouseLocation = transform.InverseTransformPoint(mouseLocation);
        return localMouseLocation.y;
    }

    Color GetDesiredColor() {
        return Color.Lerp(Color.red, Color.green, Mathf.InverseLerp(-MAX_OFFSET, MAX_OFFSET, CurHeight));
    }
    
#region Saving

    public override void LoadSave(ChasmElevatorHandleSave save) {
        handleState.LoadFromSave(save.stateSave);
        heightAudioLastPlayedAt = save.heightAudioLastPlayedAt;
        desiredHeight = save.desiredHeight;
        transform.localPosition = save.localPosition;
    }

    [Serializable]
	public class ChasmElevatorHandleSave : SaveObject<ChasmElevatorHandle> {
        public StateMachineSave<HandleState> stateSave;
        public SerializableVector3 localPosition;
        public float heightAudioLastPlayedAt;
        public float desiredHeight;
        
		public ChasmElevatorHandleSave(ChasmElevatorHandle script) : base(script) {
            this.stateSave = script.handleState.ToSave();
            this.localPosition = script.transform.localPosition;
            this.heightAudioLastPlayedAt = script.heightAudioLastPlayedAt;
            this.desiredHeight = script.desiredHeight;
		}
	}
#endregion

    public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob audioJob) => transform;
}
