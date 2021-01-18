using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShrinkGrowHallway : MonoBehaviour {
	public float maxScale = 4;
	public float minScale = 1;
	public float targetScale = 1;
	float scaleLerpSpeed = 5f;
	public TransitionTrigger triggerZone;
	public UnityEngine.Transform objectToTransform;

    void Start() {
		triggerZone.OnTransitionTrigger += SetScale;
    }

    void Update() {
		Vector3 objPos = objectToTransform.position;
		Vector3 pivot = Player.instance.movement.bottomOfPlayer;

		float newScale = Mathf.Lerp(objectToTransform.localScale.x, targetScale, scaleLerpSpeed * Time.deltaTime);

		float scaleFactor = newScale / objectToTransform.localScale.x; // relataive scale factor
		var endScale = objectToTransform.localScale * scaleFactor;

		Vector3 positionDiff = objPos - pivot; // diff from object pivot to desired pivot/origin

		// calc final position post-scale
		Vector3 finalPos = (positionDiff * scaleFactor) + pivot;

		// finally, actually perform the scale/translation
		objectToTransform.localScale = endScale;
		objectToTransform.position = finalPos;
	}

	void SetScale(Collider maybePlayer, float t) {
		if (maybePlayer.transform != Player.instance.transform) return;

		targetScale = Mathf.Lerp(minScale, maxScale, t);
	}
}
