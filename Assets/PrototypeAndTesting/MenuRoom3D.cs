using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class MenuRoom3D : MonoBehaviour {
	UnityEngine.Transform[] pieces;

	public AnimationCurve buildAnimationCurve;
	private float maxPieceSpawnDelay = 0.5f;
	private float buildTime = 0.5f;

    IEnumerator Start() {
		pieces = transform.GetComponentsInChildrenOnly<UnityEngine.Transform>();
		yield return new WaitForSeconds(1);

		foreach (UnityEngine.Transform piece in pieces) {
			StartCoroutine(SpawnPiece(piece));
		}
	}

    void Update() {
        
    }

	IEnumerator SpawnPiece(UnityEngine.Transform piece) {
		piece.localScale = Vector3.zero;
		Vector3 offset = piece.localPosition;
		piece.localPosition += Random.insideUnitSphere * Random.Range(0f, 3f);
		yield return new WaitForSeconds(Random.Range(0, maxPieceSpawnDelay));

		float timeElapsed = 0;
		float thisPieceBuildTime = buildTime + Random.Range(-0.25f, 0.75f);
		while (timeElapsed < thisPieceBuildTime) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / thisPieceBuildTime;

			piece.localScale = Vector3.one * Mathf.Sqrt(t*t);
			piece.localPosition = Vector3.Lerp(piece.localPosition, offset, t*t);
			yield return null;
		}

		piece.localScale = Vector3.one;
	}
}
