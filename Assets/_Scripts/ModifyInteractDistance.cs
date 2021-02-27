using SuperspectiveUtils;
using UnityEngine;

public class ModifyInteractDistance : MonoBehaviour {
    public float desiredInteractDistance = 8f;

    void OnTriggerExit(Collider other) {
        if (other.TaggedAsPlayer()) Interact.instance.interactionDistance = Interact.defaultInteractionDistance;
    }

    void OnTriggerStay(Collider other) {
        if (other.TaggedAsPlayer()) Interact.instance.interactionDistance = desiredInteractDistance;
    }
}