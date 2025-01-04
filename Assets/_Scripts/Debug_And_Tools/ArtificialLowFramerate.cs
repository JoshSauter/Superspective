using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ArtificialLowFramerate : MonoBehaviour {
	[Header("Toggle with Shift+F5")]
	public float targetFramerate = 60;

	[SerializeField, ReadOnly]
	int numOperations = 0;
	long sum = long.MinValue;
	int maxOperationsDelta = 50000;

	bool limitFramerate = false;

    void Update() {
		if (DebugInput.GetKey(KeyCode.LeftShift) && DebugInput.GetKeyDown(KeyCode.F5)) {
			limitFramerate = !limitFramerate;
		}

		if (limitFramerate) {
			WasteTime();
		}
    }

	void WasteTime() {
		float curFramerate = 1 / Time.deltaTime;
		numOperations += (int)Mathf.Sign(curFramerate - targetFramerate) * Mathf.Min(maxOperationsDelta, 10 * (int)Mathf.Pow((curFramerate - targetFramerate), 2));

		for (int i = 0; i < numOperations; i++) {
			sum += (long)Mathf.Sqrt(Random.Range(0, numOperations));
		}

		if (sum > long.MaxValue - 1) {
			Debug.Log("Ignore this, just here so sum variable is used");
		}
	}
}
