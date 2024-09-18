using System;
using Audio;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.UI;

public class InteractJuice : MonoBehaviour {
    public Interact interact;
    private Image reticle => interact.reticle;
    private Image reticleOutside => interact.reticleOutside;
    
    // Reticle Color Juice
    readonly Color reticleOutsideSelectColor = new Color(0.1f, 0.75f, 0.075f, 0.75f);
    readonly Color reticleSelectColor = new Color(0.15f, 1, 0.15f, 0.9f);
    readonly Color reticleSelectDisabledColor = new Color(.8f, 0.15f, 0.15f, .9f);
    Color reticleUnselectColor;
    Color reticleOutsideUnselectColor;

    private enum JuiceColorState {
        Nothing,
        Interactable,
        Disabled
    }
    private StateMachine<JuiceColorState> juiceColorState;
    
    // Reticle Bloom Juice
    private const float newInteractableObjHoveredReticleBloom = 1.75f;
    private const float bloomTime = 0.25f;
    enum BloomState {
        Idle,
        Blooming
    }
    private StateMachine<BloomState> bloomState;

    enum LastObjectHoveredState {
        Idle,
        HoveredOverSomething
    }
    private StateMachine<LastObjectHoveredState> lastObjectHoveredState;
    private InteractableObject lastObjectHovered;
    private const float timeBeforeLosingLastObjectHovered = 0.25f;
    

    private void Start() {
        juiceColorState = this.StateMachine(JuiceColorState.Nothing);
        bloomState = this.StateMachine(BloomState.Idle);
        lastObjectHoveredState = this.StateMachine(LastObjectHoveredState.Idle);
        
        
        reticleUnselectColor = reticle.color;
        reticleOutsideUnselectColor = reticleOutside.color;
        
        // Keeping track of last object hovered
        lastObjectHoveredState.AddStateTransition(LastObjectHoveredState.HoveredOverSomething, LastObjectHoveredState.Idle, timeBeforeLosingLastObjectHovered);
        lastObjectHoveredState.AddTrigger(LastObjectHoveredState.Idle, 0f, () => lastObjectHovered = null);
        
        // Color-change triggers
        juiceColorState.AddTrigger(JuiceColorState.Interactable, 0f, () => {
            DisabledInteractableMessage.instance.timedMsg.HideMessage();
            UpdateHelpText();
            SetReticleColors(reticleSelectColor, reticleOutsideSelectColor);
        });
        juiceColorState.AddTrigger(JuiceColorState.Disabled, 0f, () => {
            InteractableMessage.instance.timedMsg.HideMessage();
            UpdateHelpText();
            SetReticleColors(reticleSelectDisabledColor, reticleSelectDisabledColor);
        });
        juiceColorState.AddTrigger(JuiceColorState.Nothing, 0f, () => {
            DisabledInteractableMessage.instance.timedMsg.HideMessage();
            InteractableMessage.instance.timedMsg.HideMessage();
            SetReticleColors(reticleUnselectColor, reticleOutsideUnselectColor);
        });
        
        // Reticle bloom state transition + triggers
        bloomState.AddStateTransition(BloomState.Blooming, BloomState.Idle, bloomTime);
        bloomState.AddTrigger(BloomState.Blooming, 0f, () => 
            reticleOutside.rectTransform.localScale = Vector3.one * newInteractableObjHoveredReticleBloom);
        bloomState.AddTrigger(BloomState.Idle, 0f, () =>
            reticleOutside.rectTransform.localScale = Vector3.one);
    }

    private void LateUpdate() {
        if (!GameManager.instance.gameHasLoaded) return;
        
        InteractableObject interactableObjectHovered = interact.interactableObjectHovered;
        bool hoveredOverNewObject = UpdateLastHoveredOverState(interactableObjectHovered);
        UpdateColorState(Player.instance.IsHoldingSomething ? lastObjectHovered : interactableObjectHovered);
        UpdateReticleBloomState(hoveredOverNewObject);
        UpdateHelpText();
    }

    private void UpdateHelpText() {
        if (juiceColorState == JuiceColorState.Disabled) {
            if (Settings.Gameplay.ShowDisabledReason && lastObjectHovered.disabledHelpText != "") {
                DisabledInteractableMessage.instance.timedMsg.ShowMessage(lastObjectHovered.disabledHelpText);
            }
        }
        else if (juiceColorState == JuiceColorState.Interactable) {
            if (Settings.Gameplay.ShowInteractionHelp && lastObjectHovered.enabledHelpText != "") {
                InteractableMessage.instance.timedMsg.ShowMessage(lastObjectHovered.enabledHelpText);
            }
        }
    }

    // Returns true if we are hovering over a new object
    private bool UpdateLastHoveredOverState(InteractableObject interactableObjectHovered) {
        bool hoveredOverNewObject = false;
        if (interactableObjectHovered != null) {
            if (!interactableObjectHovered.IsSameAs(lastObjectHovered)) {
                hoveredOverNewObject = true;
                
                // Play an on-hover sound (unless it's an object we're already holding)
                bool hoveredOverHeldObj = false;
                if (Player.instance.IsHoldingSomething) {
                    if (Player.instance.heldObject != null &&
                        Player.instance.heldObject.gameObject == interactableObjectHovered.gameObject) {
                        hoveredOverHeldObj = true;
                    }
                }
                if (!hoveredOverHeldObj) {
                    bool disabled = interactableObjectHovered.state == InteractableObject.InteractableState.Disabled;
                    AudioManager.instance.Play(AudioName.InteractableHover, "InteractableHover", false,
                        job => job.basePitch = disabled ? 0.5f : 1f);
                }
            }
            lastObjectHovered = interactableObjectHovered;
            lastObjectHoveredState.Set(LastObjectHoveredState.HoveredOverSomething, true);
        } else if (Player.instance.IsHoldingSomething) {
            lastObjectHoveredState.Set(LastObjectHoveredState.HoveredOverSomething, true);
        }

        return hoveredOverNewObject;
    }

    private void UpdateReticleBloomState(bool hoveredOverNewObject) {
        if (hoveredOverNewObject) {
            bool disabled = lastObjectHovered.state == InteractableObject.InteractableState.Disabled;
            if (!disabled) {
                bloomState.Set(BloomState.Blooming);
            }
        }
        
        if (bloomState.State == BloomState.Blooming) {
            float t = bloomState.Time / bloomTime;
            reticleOutside.rectTransform.localScale = Vector3.one * Mathf.Lerp(newInteractableObjHoveredReticleBloom, 1, t*t);
        }
    }

    private void UpdateColorState(InteractableObject interactableObjectHovered) {
        // Debug.Log($"UpdateColorState: interactableObjectHovered: {interactableObjectHovered?.FullPath() ?? "None"}");
        if (interactableObjectHovered != null) {
            bool disabled = interactableObjectHovered.state == InteractableObject.InteractableState.Disabled;
            if (disabled) {
                juiceColorState.Set(JuiceColorState.Disabled);
            }
            else {
                juiceColorState.Set(JuiceColorState.Interactable);
            }
        }
        else {
            juiceColorState.Set(JuiceColorState.Nothing);
        }
    }

    private void SetReticleColors(Color insideColor, Color outsideColor) {
        reticle.color = insideColor;
        reticleOutside.color = outsideColor;
    }
}