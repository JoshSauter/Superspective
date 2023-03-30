using System.Collections;
using System.Collections.Generic;
using SuperspectiveUtils;
using UnityEngine;

public class GrowShrinkHallwayOld : MonoBehaviour {
	public float maxScale = 4;
	public float minScale = 1;
	public float targetScale = 1;
	float scaleLerpSpeed = 5f;
	public GrowShrinkTransitionTrigger triggerZone;
	public Transform objectToTransform;

    void Start() {
		triggerZone.OnTransitionTrigger += SetScale;
    }

    void Update() {
		Vector3 objPos = objectToTransform.position;
		Vector3 pivot = Player.instance.movement.bottomOfPlayer;

		float newScale = Mathf.Lerp(objectToTransform.localScale.x, targetScale, scaleLerpSpeed * Time.deltaTime);

		float scaleFactor = newScale / objectToTransform.localScale.x; // relative scale factor
		var endScale = objectToTransform.localScale * scaleFactor;

		Vector3 positionDiff = objPos - pivot; // diff from object pivot to desired pivot/origin

		// calc final position post-scale
		Vector3 finalPos = (positionDiff * scaleFactor) + pivot;

		// finally, actually perform the scale/translation
		objectToTransform.localScale = endScale;
		objectToTransform.position = finalPos;
	}

    void SetScale(Collider maybePlayer, float t) {
	    if (!maybePlayer.TaggedAsPlayer()) return;

	    targetScale = Mathf.Lerp(minScale, maxScale, 1-t);
    }
}
