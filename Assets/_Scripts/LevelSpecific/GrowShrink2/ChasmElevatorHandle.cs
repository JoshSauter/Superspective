using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId), typeof(InteractableObject))]
public class ChasmElevatorHandle : SaveableObject<ChasmElevatorHandle, ChasmElevatorHandle.ChasmElevatorHandleSave> {
    public Renderer handleRenderer;
    private InteractableObject interact;
    private List<Collider> handleColliders;

    public HandleState startingState = HandleState.Up;
    public enum HandleState {
        Down,
        HeldByPlayer,
        Up
    }
    public StateMachine<HandleState> handleState;

    private const float GRACE_PERIOD = 0.75f; // Minimum time before the handle will snap to up or down (to allow time to leave the top or bottom)
    private const float HANDLE_LERP_SPEED = 0.1f;
    private const float MAX_OFFSET = 0.25f;
    
    private HandleState CloserHandleState => CurHeight >= 0 ? HandleState.Up : HandleState.Down;

    private float desiredHeight;
    private float CurHeight {
        get => transform.localPosition.y;
        set {
            float desiredY = Mathf.Clamp(value, -MAX_OFFSET, MAX_OFFSET);
            transform.localPosition = transform.localPosition.WithY(desiredY);
        }
    }
    
    protected override void Awake() {
        base.Awake();
        handleColliders = GetComponentsInChildren<Collider>().ToList();
        
        handleState = this.StateMachine(startingState);
        
        // Automatically release the handle when it's moved all the way up or down
        handleState.AddStateTransition(HandleState.HeldByPlayer, () => CloserHandleState, () => handleState.Time > GRACE_PERIOD && desiredHeight is >= MAX_OFFSET or <= -MAX_OFFSET);
        
        // Turn off handle colliders while the player is holding it to avoid interfering with raycasts to get mouse position
        handleState.AddTrigger(HandleState.HeldByPlayer, () => handleColliders.ForEach(c => c.enabled = false));
        handleState.AddTrigger(intoState => intoState is HandleState.Down or HandleState.Up, () => handleColliders.ForEach(c => c.enabled = true));
        
        // Set the desired height to the max offset in the appropriate direction when the player is not holding it
        handleState.AddTrigger(HandleState.Down, () => desiredHeight = -MAX_OFFSET);
        handleState.AddTrigger(HandleState.Up, () => desiredHeight = MAX_OFFSET);
        
        // Set the desired height based on the mouse position when the player is holding the handle
        handleState.WithUpdate(HandleState.HeldByPlayer, _ => {
            if (PlayerButtonInput.instance.InteractHeld) {
                desiredHeight = GetDesiredHeight();
            }
            else {
                handleState.Set(CloserHandleState);
            }
        });
    }

    void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;
        
        CurHeight = Mathf.Lerp(CurHeight, desiredHeight, HANDLE_LERP_SPEED);
        
        // Color the handle from red to green depending on position
        Color desiredColor = GetDesiredColor();
        handleRenderer.material.color = desiredColor;
        handleRenderer.material.SetColor("_EmissionColor", desiredColor);
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
		[Serializable]
		public class ChasmElevatorHandleSave : SerializableSaveObject<ChasmElevatorHandle> {
            private StateMachine<HandleState>.StateMachineSave stateSave;
            
			public ChasmElevatorHandleSave(ChasmElevatorHandle script) : base(script) {
                this.stateSave = script.handleState.ToSave();
			}

			public override void LoadSave(ChasmElevatorHandle script) {
                script.handleState.LoadFromSave(this.stateSave);
			}
		}
#endregion
}
