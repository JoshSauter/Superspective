using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour {
    float acceleration = 150f;
    float backwardsSpeed = 0.01f;
    float walkSpeed = 4f;
    float runSpeed = 14f;
    float movespeed;
    private Rigidbody thisRigidbody;

	// Use this for initialization
	void Start () {
        movespeed = walkSpeed;
        thisRigidbody = GetComponent<Rigidbody>();
	}

    private void Update() {
        if (Input.GetKey(KeyCode.LeftShift)) {
            movespeed = runSpeed;
        }
        else {
            movespeed = walkSpeed;
        }
    }

    // Update is called once per frame
    void FixedUpdate () {
        Vector3 moveDirection = new Vector2();
        if (Input.GetKey(KeyCode.W)) {
            moveDirection += transform.forward;
        }
        if (Input.GetKey(KeyCode.S)) {
            moveDirection -= transform.forward;
        }
        if (Input.GetKey(KeyCode.A)) {
            moveDirection -= transform.right;
        }
        if (Input.GetKey(KeyCode.D)) {
            moveDirection += transform.right;
        }
        // If no keys are pressed, decelerate to a stop
        if (!(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))) {
            thisRigidbody.velocity = Vector3.Lerp(thisRigidbody.velocity, Vector3.zero, 0.15f);
        }
        // If at least one direction is pressed, move the desired direction
        else {
            Move(moveDirection.normalized);
        }
	}

    void Move(Vector3 direction) {
        // Walking backwards is slower than forwards
        // TODO: This needs to operate based on transform.forward
        if (direction.x == 0) {
            direction.x *= backwardsSpeed;
        }
        Vector3 accelForce = direction * acceleration;
        thisRigidbody.AddForce(accelForce, ForceMode.Acceleration);

        Vector2 curHorizontalMovespeed = new Vector2(thisRigidbody.velocity.x, thisRigidbody.velocity.z);
        if (curHorizontalMovespeed.magnitude > movespeed) {
            Vector2 cappedHorizontalMovespeed = curHorizontalMovespeed.normalized * movespeed;
            thisRigidbody.velocity = new Vector3(cappedHorizontalMovespeed.x, thisRigidbody.velocity.y, cappedHorizontalMovespeed.y);
        }

        Vector3 curDirectionSpeed = new Vector3(thisRigidbody.velocity.x, 0, thisRigidbody.velocity.z);
        float facingSameDirection = Vector3.Dot(curDirectionSpeed.normalized, transform.forward);
        if (facingSameDirection < 0) {
            Vector3 curVel = thisRigidbody.velocity;
            float multiplier = Mathf.Lerp(1, backwardsSpeed, -facingSameDirection);
            curVel.x *= multiplier;
            curVel.z *= multiplier;
            thisRigidbody.velocity = curVel;
        }
    }
}
