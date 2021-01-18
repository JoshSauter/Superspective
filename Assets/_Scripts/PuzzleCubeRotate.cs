using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ObjectScale))]
public class PuzzleCubeRotate : MonoBehaviour {
    public int startRotate;
    public float rotationTime = 0.5f;
    float period;
    Quaternion startRotation;

    void Awake() {
        startRotation = transform.rotation;
    }

    void OnEnable() {
        StartCoroutine(DoRotations());
    }

    void OnDisable() {
        transform.rotation = startRotation;
        StopAllCoroutines();
    }

    // Use this for initialization
    IEnumerator DoRotations() {
        period = GetComponent<ObjectScale>().period;
        int i = startRotate;
        while (enabled) {
            yield return new WaitForSeconds((period - rotationTime) / 2f);
            StartCoroutine(Rotate(transform.rotation, DestinationRotation(i)));
            i++;
            yield return new WaitForSeconds((period + rotationTime) / 2f);
        }
    }

    IEnumerator Rotate(Quaternion startRot, Quaternion destRot) {
        float timeElapsed = 0;
        while (timeElapsed < rotationTime) {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / rotationTime;

            transform.rotation = Quaternion.Lerp(startRot, destRot, t);

            yield return null;
        }

        transform.rotation = destRot;
    }

    Quaternion DestinationRotation(int index) {
        switch (index % 4) {
            case 0:
                return Quaternion.Euler(Vector3.up * 45);
            case 1:
                return Quaternion.Euler(Vector3.forward * 45);
            case 2:
                return Quaternion.Euler(Vector3.right * 45);
            case 3:
                return Quaternion.Euler(Vector3.zero);
            default:
                return new Quaternion();
        }
    }
}