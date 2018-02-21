using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesirePlatformRiseFall : MonoBehaviour {
	Rigidbody thisRigidbody;
	PlayerLook playerLook;
	Transform lookPanel;


	float curLook = 0;
	float curLookLerpSpeed = 0.1f;
	float deadZone = 0.2f;
	float riseSpeed = 3;
	float platformDecelerationLerpRate = 0.2f;
	float platformAccelerationLerpRate = 0.15f;

	float maxPanelYPosition = 0;
	float minPanelYPosition = -3;

	// Use this for initialization
	void Start () {
		thisRigidbody = GetComponentInParent<Rigidbody>();
		lookPanel = transform.parent.Find("Panel");
	}
	
	// Update is called once per frame
	void Update () {
		if (playerLook != null) {
			HandlePlayerLook(playerLook.normalizedY);
		}
	}

	/// <summary>
	/// Cache's the player's PlayerLook component if the player collides with this object.
	/// </summary>
	/// <param name="other">Object inside the trigger zone</param>
	private void OnTriggerEnter(Collider other) {
		if (playerLook == null && other.tag == "Player") {
			playerLook = other.transform.GetComponent<PlayerLook>();
		}
	}

	/// <summary>
	/// Uncache's the player's PlayerLook component if the player leaves the trigger zone.
	/// </summary>
	/// <param name="other">Object that has just left the trigger zone</param>
	private void OnTriggerExit(Collider other) {
		if (playerLook != null && other.tag == "Player") {
			playerLook = null;
		}
	}

	/// <summary>
	/// Lerps the curLook value towards the normalizedYLook (from the player's look direction).
	/// Then, either moves the attached Panel up or down, or, if the player is looking beyond the deadZone,
	/// will make the platform rise or fall (lerping up or down to speed).
	/// </summary>
	/// <param name="normalizedYLook">The player's current, normalized, rotationY</param>
	private void HandlePlayerLook(float normalizedYLook) {
		curLook = Mathf.Lerp(curLook, normalizedYLook, curLookLerpSpeed);

		if (Mathf.Abs(curLook) < deadZone) {
			thisRigidbody.velocity = Vector3.Lerp(thisRigidbody.velocity, Vector3.zero, platformDecelerationLerpRate);
			MovePanel(curLook);
		}
		else {
			thisRigidbody.velocity = Vector3.Lerp(thisRigidbody.velocity, Mathf.Sign(curLook) * riseSpeed * Vector3.up, platformAccelerationLerpRate);
		}
	}

	private void MovePanel(float curLook) {
		float t = Mathf.InverseLerp(-deadZone, deadZone, curLook);
		Vector3 curPanelPos = lookPanel.localPosition;
		curPanelPos.y = Mathf.Lerp(minPanelYPosition, maxPanelYPosition, t);
		//lookPanel.localPosition = curPanelPos;
	}
}
