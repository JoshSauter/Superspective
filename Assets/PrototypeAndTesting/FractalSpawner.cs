using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FractalSpawner : MonoBehaviour {
    public float size = 500f;
    public int recursions = 6;

    void Start() {
        BuildFractal();
    }

    void BuildFractal() {
        Vector3 startPoint = Vector3.zero;
        startPoint.x -= size / 2f;
        startPoint.z -= size / 2f;
        startPoint.y += size / 4f;
        BuildFractalRecursively(startPoint, 0, size/2f);
    }

    void BuildFractalRecursively(Vector3 pos, int depth, float thisSize) {
        Transform newCube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        newCube.SetParent(transform, true);
        newCube.localPosition = pos;
        newCube.localScale = Vector3.one * thisSize;

        newCube.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/Unlit/Unlit");

        if (depth == recursions) {
            return;
        }

        float nextSize = thisSize / 2f;
        float movement = thisSize / 2f + nextSize / 2f;

        BuildFractalRecursively(pos - transform.right * movement - transform.up * nextSize / 2f, depth + 1, nextSize);
        //BuildFractalRecursively(pos + Vector3.up * -movement / 2f, depth + 1);
        BuildFractalRecursively(pos - transform.forward * movement - transform.up * nextSize / 2f, depth + 1, nextSize);
    }
}
