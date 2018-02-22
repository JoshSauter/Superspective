using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesirePlatformRiseFall : MonoBehaviour {
	Rigidbody panelRigidbody;
	PlayerLook playerLook;
	Transform lookPanel;
	Vector3 notchStartPos;


	float curLook = 0;
	public float curLookLerpSpeed = 0.08f;
	float panelDeadZone = 0.15f;
	float deadZone = 0.2f;
	public float riseSpeed = 3;
	float platformDecelerationLerpRate = 0.2f;
	float platformAccelerationLerpRate = 0.1f;

	float maxPanelYPosition = 0;
	float minPanelYPosition = -3;

	Transform notch;
	Renderer notchRenderer;
	float maxNotchOffset = 0.5f;
	Color notchGreen = new Color(0.09812928f, 0.8897059f, 0.1035884f);
	Color notchRed = new Color(0.8897059f, 0.09812924f, 0.09812924f);

	// Use this for initialization
	void Start () {
		panelRigidbody = GetComponentInParent<Rigidbody>();
		lookPanel = transform.parent.Find("Panel");
		notch = lookPanel.Find("Notch");
		notchStartPos = notch.localPosition;
		notchRenderer = notch.GetComponent<Renderer>();
	}
	
	// Update is called once per frame
	void Update () {
		if (playerLook != null) {
			HandlePlayerLook(playerLook.normalizedY);
		}
		else {
			curLook = Mathf.Lerp(curLook, 0, 0.1f);
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

		MovePanel(curLook);
		MoveNotch(curLook);
		ColorNotch(curLook);
		MovePlatform(curLook);
	}

	private void MovePanel(float curLook) {
		float t = Mathf.InverseLerp(-panelDeadZone, panelDeadZone, curLook);
		Vector3 curPanelPos = lookPanel.localPosition;
		curPanelPos.y = Mathf.Lerp(minPanelYPosition, maxPanelYPosition, t);
		lookPanel.localPosition = curPanelPos;
	}

	private void MoveNotch(float curLook) {
		if (curLook > deadZone || curLook < -deadZone) {
			notch.localPosition = notchStartPos + new Vector3(0, Mathf.Sign(curLook) * maxNotchOffset, 0);

		}
		else if (curLook > panelDeadZone || curLook < -panelDeadZone) {
			float t = Mathf.InverseLerp(panelDeadZone, deadZone, Mathf.Abs(curLook));
			notch.localPosition = notchStartPos + Vector3.Lerp(Vector3.zero, Vector3.up * Mathf.Sign(curLook) * maxNotchOffset, t);
		}
		else {
			notch.localPosition = notchStartPos;
		}
	}

	private void ColorNotch(float curLook) {
		if (curLook > deadZone) {
			EpitaphUtils.Utils.SetColorForRenderer(notchRenderer, notchGreen, "_EmissionColor");
		}
		else if (curLook < -deadZone) {
			EpitaphUtils.Utils.SetColorForRenderer(notchRenderer, notchRed, "_EmissionColor");
		}
		else {
			EpitaphUtils.Utils.SetColorForRenderer(notchRenderer, Color.white, "_EmissionColor");
		}
	}

	private void MovePlatform(float curLook) {
		if (Mathf.Abs(curLook) < deadZone) {
			panelRigidbody.velocity = Vector3.Lerp(panelRigidbody.velocity, Vector3.zero, platformDecelerationLerpRate);
			return;
		}
		else {
			panelRigidbody.velocity = Vector3.Lerp(panelRigidbody.velocity, Mathf.Sign(curLook) * riseSpeed * Vector3.up, platformAccelerationLerpRate);
		}
	}
}
