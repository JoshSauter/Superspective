using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour {
    float acceleration = 75f;
    float backwardsSpeed = 0.7f;
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    float movespeed;
    private Rigidbody thisRigidbody;

	CapsuleCollider thisCollider;

	// Use this for initialization
	void Start () {
        movespeed = walkSpeed;
        thisRigidbody = GetComponent<Rigidbody>();
		thisCollider = GetComponent<CapsuleCollider>();
	}

    private void Update() {
        if (Input.GetKey(KeyCode.LeftShift)) {
            movespeed = walkSpeed;
        }
        else {
            movespeed = runSpeed;
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
            Vector2 horizontalVelocity = new Vector2(thisRigidbody.velocity.x, thisRigidbody.velocity.z);
            horizontalVelocity = Vector2.Lerp(horizontalVelocity, Vector2.zero, 0.15f);
            thisRigidbody.velocity = new Vector3(horizontalVelocity.x, thisRigidbody.velocity.y, horizontalVelocity.y);
        }
        // If at least one direction is pressed, move the desired direction
        else {
            Move(moveDirection.normalized);
        }

		SphereCastDown();
	}

    void Move(Vector3 direction) {
        // Walking backwards is slower than forwards
        // TODO: This needs to operate based on transform.forward
        if (direction.x == 0) {
            direction.x *= backwardsSpeed;
        }
        Vector3 accelForce = direction * acceleration;
        thisRigidbody.AddForce(accelForce, ForceMode.Acceleration);

        Vector2 curHorizontalVelocity = HorizontalVelocity();
        if (curHorizontalVelocity.magnitude > movespeed) {
            Vector2 cappedHorizontalMovespeed = curHorizontalVelocity.normalized * movespeed;
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

    public Vector3 HorizontalVelocity3() {
        return new Vector3(thisRigidbody.velocity.x, 0, thisRigidbody.velocity.z);
    }

    public Vector2 HorizontalVelocity() {
        return new Vector2(thisRigidbody.velocity.x, thisRigidbody.velocity.z);
    }

    public float HorizontalMovespeed() {
        return HorizontalVelocity().magnitude;
    }

	public void SphereCastDown() {
		RaycastHit hitInfo = new RaycastHit();
		Physics.SphereCast(transform.position, thisCollider.radius, Vector3.down, out hitInfo, thisCollider.height/2f);
		if (hitInfo.collider != null) {
			//Debug.Log(hitInfo.collider.name + "\n" + hitInfo.normal);
			Physics.gravity = Vector3.Project(Physics.gravity, hitInfo.normal).normalized * Physics.gravity.magnitude;
		}
	}
}
