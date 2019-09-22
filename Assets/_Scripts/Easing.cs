using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Easing {
	public static float EaseInOut(float t) {
		return 0.5f * (1 - Mathf.Cos(t * Mathf.PI));
	}
}
