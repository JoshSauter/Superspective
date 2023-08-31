using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class HideInteractOnCondition : MonoBehaviour {
    public ConditionType conditionType;
    public enum ConditionType {
        PlayerScale
    }

    bool IsPlayerScaleCondition => conditionType == ConditionType.PlayerScale;

    [ShowIf(nameof(IsPlayerScaleCondition))]
    [MinMaxSlider(0.0f, 64f)]
    public Vector2 acceptablePlayerScaleRange;

    InteractableObject interactableObject;
    
    // Start is called before the first frame update
    void Start() {
        if (interactableObject == null) {
            interactableObject = GetComponent<InteractableObject>();
        }
    }

    // Update is called once per frame
    void Update() {
        switch (conditionType) {
            case ConditionType.PlayerScale:
                float playerScale = Player.instance.scale;
                if (playerScale >= acceptablePlayerScaleRange.x && playerScale < acceptablePlayerScaleRange.y) {
                    interactableObject.SetAsInteractable();
                }
                else {
                    interactableObject.SetAsHidden();
                }
                break;
        }
    }
}
