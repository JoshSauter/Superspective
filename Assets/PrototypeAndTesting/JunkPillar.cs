using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JunkPillar : MonoBehaviour {
	public int minNumberOfPieces;
	public int maxNumberOfPieces;

	public Material bufferMaterial;
	public Material pieceMaterial;

	public Vector3 minScale = new Vector3(1, 0.5f, 1f);
	public Vector3 maxScale = new Vector3(4, 1, 4);

	public float pillarHeight = 12;

    void Start() {
		SpawnPillar();
    }

    void Update() {
        
    }

	void SpawnPillar() {
		int numPieces = Random.Range(minNumberOfPieces, maxNumberOfPieces);

		for (int i = 0; i < numPieces; i++) {
			float t = ((float)i / numPieces);
			float height = t * pillarHeight;// + Random.Range(-0.25f, 0.25f);
			float sin = Mathf.Sin(t * Mathf.PI);
			Vector3 horizontalOffset = Vector3.Lerp(Vector3.zero, transform.localPosition, sin / 4f);
			//print(t + "   " + sin);
			horizontalOffset.y = 0;
			SpawnPiece(height, horizontalOffset);
		}
	}

	void SpawnPiece(float height, Vector3 horizontalOffset) {
		GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
		go.GetComponent<Renderer>().sharedMaterial = pieceMaterial;
		go.transform.SetParent(transform);
		go.transform.localPosition = horizontalOffset + new Vector3(Random.Range(-1, 1), height, Random.Range(-1, 1));
		go.transform.localScale = new Vector3(Random.Range(minScale.x, maxScale.x), Random.Range(minScale.y, maxScale.y), Random.Range(minScale.z, maxScale.z));
		Vector3 randomEuler = Random.insideUnitSphere;
		go.transform.rotation = Quaternion.Euler(randomEuler.x, randomEuler.y * 180, randomEuler.z);

		GameObject bufferGo = Instantiate(go, transform);
		bufferGo.GetComponent<Renderer>().sharedMaterial = bufferMaterial;
	}

}
