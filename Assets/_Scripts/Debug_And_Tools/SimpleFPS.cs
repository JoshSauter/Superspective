using UnityEngine;
using System.Collections;

public class SimpleFPS : MonoBehaviour {
	public Gradient fpsTextGradient;
	string label = "";
	float count;

	GUIStyle style;

	IEnumerator Start() {
		style = new GUIStyle();

		GUI.depth = 2;
		while (true) {
			if (Time.timeScale > 0) {
				yield return new WaitForSeconds(0.1f);
				count = (1 / Time.deltaTime);
				style.normal.textColor = fpsTextGradient.Evaluate(Mathf.InverseLerp(15, 60, count));
				label = "FPS :" + (Mathf.Round(count));
			}
			else {
				label = "Pause";
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	void OnGUI() {
		if (!Debug.isDebugBuild) return;
		GUI.Label(new Rect(5, 40, 100, 25), label, style);
	}
}
