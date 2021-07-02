using UnityEngine;

public class JunkPillar : MonoBehaviour {
    public Transform[] corners;

    public int minNumberOfPieces;
    public int maxNumberOfPieces;

    public Material bufferMaterial;
    public Material pieceMaterial;

    public Vector3 minScale = new Vector3(1, 0.5f, 1f);
    public Vector3 maxScale = new Vector3(4, 1, 4);

    public float radius = 5f;
    public float pillarHeight = 12;

    void OnValidate() {
        if (corners == null) {
            corners = new Transform[4];
            corners[0] = SpawnPillar(new Vector3(radius, 0, radius));
            corners[1] = SpawnPillar(new Vector3(radius, 0, -radius));
            corners[2] = SpawnPillar(new Vector3(-radius, 0, radius));
            corners[3] = SpawnPillar(new Vector3(-radius, 0, -radius));
        }
    }

    Transform SpawnPillar(Vector3 origin) {
        Transform root = new GameObject("PillarRoot").transform;
        //root.SetParent(transform);
        root.localPosition = origin;
        int numPieces = Random.Range(minNumberOfPieces, maxNumberOfPieces);

        for (int i = 0; i < numPieces; i++) {
            float t = (float) i / numPieces;
            float height = t * pillarHeight; // + Random.Range(-0.25f, 0.25f);
            float sin = Mathf.Sin(t * Mathf.PI);
            Vector3 horizontalOffset = 1.3f * Vector3.Lerp(Vector3.zero, root.localPosition, sin / 4f);
            Vector3 scale = new Vector3(
                Random.Range(minScale.x, maxScale.x),
                Random.Range(minScale.y, maxScale.y),
                Random.Range(minScale.z, maxScale.z)
            );
            scale.x *= 1.5f - 0.5f * t;
            scale.z *= 1.5f - 0.5f * t;
            //print(t + "   " + sin);
            horizontalOffset.y = 0;
            SpawnPiece(root, height, horizontalOffset, scale);
        }

        return root;
    }

    void SpawnPiece(Transform root, float height, Vector3 horizontalOffset, Vector3 scale) {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.GetComponent<Renderer>().sharedMaterial = pieceMaterial;
        //go.transform.SetParent(root);
        go.transform.localPosition = horizontalOffset + new Vector3(Random.Range(-1, 1), height, Random.Range(-1, 1));
        go.transform.localScale = scale;
        Vector3 randomEuler = Random.insideUnitSphere;
        go.transform.rotation = Quaternion.Euler(randomEuler.x, randomEuler.y * 180, randomEuler.z);

        GameObject bufferGo = Instantiate(go, root);
        bufferGo.GetComponent<Renderer>().sharedMaterial = bufferMaterial;
    }
}