using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CartesianGridTracker : MonoBehaviour {
	public Vector2 min;
	public Vector2 max;

	LightProjector projector;

	void Start() {
		projector = transform.parent.GetComponentInParent<ProjectorControls>().projector;
	}

    void Update() {
		float tx = projector.curSideToSideAnimTime;
		float ty = projector.curUpAndDownAnimTime;
		transform.localPosition = new Vector2(Mathf.Lerp(min.x, max.x, tx), Mathf.Lerp(min.y, max.y, ty));
    }
}
