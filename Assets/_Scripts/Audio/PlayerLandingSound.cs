using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PortalMechanics;
using EpitaphUtils;
using Audio;

public class PlayerLandingSound : MonoBehaviour {
	public SoundEffect shoeSound;
	public SoundEffect thumpSound;
	PlayerMovement playerMovement;
	bool wasGrounded = true;
	float startVolume;
	float thumpStartVolume;
	public float volumeDelta = 0.3f;
	public float minSpeed = 6f;
	public float maxSpeed = 40f;

	float verticalOffset = 0f;
	float maxHeight;
	bool keepTrackOfPlayerAirHeight = false;

    void Start() {
		playerMovement = GetComponent<PlayerMovement>();
		startVolume = shoeSound.audioSource.volume;
		thumpStartVolume = thumpSound.audioSource.volume;

		Portal.OnAnyPortalTeleport += HandlePlayerTeleported;
    }

	void HandlePlayerTeleported(Portal inPortal, Collider objTeleported) {
		if (!objTeleported.gameObject.TaggedAsPlayer()) return;
	}

    void FixedUpdate() {
        if (wasGrounded && !playerMovement.grounded.isGrounded) {
			keepTrackOfPlayerAirHeight = true;

			verticalOffset = 0f;
			maxHeight = verticalOffset;
		}
		wasGrounded = playerMovement.grounded.isGrounded;

		if (keepTrackOfPlayerAirHeight) {
			if (!playerMovement.grounded.isGrounded) {
				float diff = Vector3.Dot(playerMovement.ProjectedVerticalVelocity(), -Physics.gravity.normalized) * Time.fixedDeltaTime;
				verticalOffset += diff;
				if (verticalOffset > maxHeight) {
					maxHeight = verticalOffset;
				}
			}
			else {
				float maxDiff = maxHeight - verticalOffset;

				if (maxDiff < 0)
					return;

				float simulatedSpeedOfImpact = Mathf.Sqrt(2 * Physics.gravity.magnitude * maxDiff);

				// Debug.LogWarning("SimulatedSpeed: " + simulatedSpeedOfImpact);
				if (simulatedSpeedOfImpact >= minSpeed) {
					float volume = startVolume + Mathf.Lerp(-volumeDelta, volumeDelta, Mathf.InverseLerp(minSpeed, maxSpeed, simulatedSpeedOfImpact));
					shoeSound.audioSource.volume = volume;


					if (simulatedSpeedOfImpact >= 2 * minSpeed) {
						float thumpVolume = thumpStartVolume + Mathf.Lerp(-volumeDelta, volumeDelta, Mathf.InverseLerp(2 * minSpeed, 2 * maxSpeed, simulatedSpeedOfImpact));
						thumpSound.audioSource.volume = thumpVolume;

						thumpSound.Play();
					}

				}

				keepTrackOfPlayerAirHeight = false;
			}
		}

    }
}
