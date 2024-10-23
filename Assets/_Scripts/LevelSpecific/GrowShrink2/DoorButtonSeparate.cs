using System.Collections;
using System.Collections.Generic;
using Interactables;
using SuperspectiveUtils;
using UnityEngine;

public class DoorButtonSeparate : MonoBehaviour {
    public Button button;

    private Vector3 startPos;
    private Vector3 endPosLeft;
    private Vector3 endPosRight;

    public Transform buttonLeft;
    public Transform buttonRight;

    private float timeAtLastChange;
    
    private const float OPEN_DISTANCE = 0.125f;
    private const float OPEN_TIME = 2.5f;
    
    void Start() {
        // Buttons left & right start in same position
        startPos = buttonLeft.localPosition;
        endPosLeft = startPos + Vector3.right * OPEN_DISTANCE;
        endPosRight = startPos + Vector3.left * OPEN_DISTANCE;
        
        button.OnButtonPressBegin += _ => {
            timeAtLastChange = Time.time;
        };
        button.OnButtonUnpressBegin += _ => {
            timeAtLastChange = Time.time;
        };
    }

    void Update() {
        Vector3 desiredPosLeft = button.pwr.PowerIsOn ? endPosLeft : startPos;
        Vector3 desiredPosRight = button.pwr.PowerIsOn ? endPosRight : startPos;
        
        float timeSinceChange = Time.time - timeAtLastChange;
        float t = Easing.EaseInOut(timeSinceChange / OPEN_TIME);
        
        buttonLeft.localPosition = Vector3.Lerp(buttonLeft.localPosition, desiredPosLeft, t);
        buttonRight.localPosition = Vector3.Lerp(buttonRight.localPosition, desiredPosRight, t);
    }
}
