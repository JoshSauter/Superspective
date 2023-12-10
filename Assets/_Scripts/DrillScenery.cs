using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour {
    public Transform pendulum;
    private const float maxRotation = 42.5f;
    private const float minRotation = -48.2f;
    private const float animationTime = 7.5f;

    public AnimationCurve rotationAnimationCurve;
    
    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        float t = (Time.time % animationTime) / animationTime;
        pendulum.rotation = Quaternion.Euler(Vector3.forward * Mathf.Lerp(minRotation, maxRotation, rotationAnimationCurve.Evaluate(t)));
    }
}
