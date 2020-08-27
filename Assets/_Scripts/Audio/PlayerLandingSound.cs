using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PortalMechanics;
using EpitaphUtils;

public class PlayerLandingSound : MonoBehaviour {
	public SoundSettings shoeSound;
	public SoundSettings thumpSound;
	PlayerMovement playerMovement;
	bool wasGrounded = true;
	float startVolume;
	float thumpStartVolume;
	public float volumeDelta = 0.3f;
	public float minSpeed = 6f;
	public float maxSpeed = 40f;

    void Start() {
		playerMovement = GetComponent<PlayerMovement>();
		startVolume = shoeSound.volume;
		thumpStartVolume = thumpSound.volume;

		Portal.OnAnyPortalTeleport += HandlePlayerTeleported;
    }

	void HandlePlayerTeleported(Portal inPortal, Collider objTeleported) {
		if (!objTeleported.gameObject.TaggedAsPlayer()) return;


	}

    void FixedUpdate() {
        if (wasGrounded && !playerMovement.grounded) {
			StartCoroutine(KeepTrackOfPlayerAirHeight());
		}
		wasGrounded = playerMovement.grounded;
    }

	IEnumerator KeepTrackOfPlayerAirHeight() {
		float verticalOffset = 0f;
		float maxHeight = verticalOffset;
		while (!playerMovement.grounded) {
			float diff = Vector3.Dot(playerMovement.ProjectedVerticalVelocity(), -Physics.gravity.normalized) * Time.fixedDeltaTime;
			verticalOffset += diff;
			if (verticalOffset > maxHeight) {
				maxHeight = verticalOffset;
			}

			yield return new WaitForFixedUpdate();
		}
		float maxDiff = maxHeight - verticalOffset;

		if (maxDiff < 0) yield break;

		float simulatedSpeedOfImpact = Mathf.Sqrt(2 * Physics.gravity.magnitude * maxDiff);

		// Debug.LogWarning("SimulatedSpeed: " + simulatedSpeedOfImpact);
		if (simulatedSpeedOfImpact < minSpeed) yield break;

		float volume = startVolume + Mathf.Lerp(-volumeDelta, volumeDelta, Mathf.InverseLerp(minSpeed, maxSpeed, simulatedSpeedOfImpact));
		shoeSound.volume = volume;

		//SoundManager.instance.Play("PlayerLandingShoes", shoeSound);

		if (simulatedSpeedOfImpact < 2 * minSpeed) yield break;

		float thumpVolume = thumpStartVolume + Mathf.Lerp(-volumeDelta, volumeDelta, Mathf.InverseLerp(2 * minSpeed, 2 * maxSpeed, simulatedSpeedOfImpact));
		thumpSound.volume = thumpVolume;

		SoundManager.instance.Play("PlayerLandingThump", thumpSound);
	}
}
