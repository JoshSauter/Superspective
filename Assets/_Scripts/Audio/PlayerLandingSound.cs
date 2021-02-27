using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PortalMechanics;
using SuperspectiveUtils;
using Audio;
using static Audio.AudioManager;

public class PlayerLandingSound : MonoBehaviour {
	//public SoundEffectOld shoeSound;
	//public SoundEffectOld thumpSound;
	AudioJob ruffleSound;
	AudioJob thumpSound;

	PlayerMovement playerMovement;
	bool wasGrounded = true;
	float ruffleStartVolume;
	float thumpStartVolume;
	public float volumeDelta = 0.3f;
	public float minSpeed = 6f;
	public float maxSpeed = 40f;

	float verticalOffset = 0f;
	float maxHeight;
	bool keepTrackOfPlayerAirHeight = false;

	string id = "PlayerLandingSound";

    void Start() {
		playerMovement = GetComponent<PlayerMovement>();
		
		ruffleSound = AudioManager.instance.GetOrCreateJob(AudioName.PlayerJumpLandingRuffle, id);
		thumpSound = AudioManager.instance.GetOrCreateJob(AudioName.PlayerJumpLandingThump, id);

		ruffleStartVolume = ruffleSound.audio.volume;
		thumpStartVolume = thumpSound.audio.volume;

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
					float volume = ruffleStartVolume + Mathf.Lerp(-volumeDelta, volumeDelta, Mathf.InverseLerp(minSpeed, maxSpeed, simulatedSpeedOfImpact));
					ruffleSound.audio.volume = volume;
					//AudioManager.instance.PlayOnGameObject(AudioName.PlayerJumpLandingRuffle, id, gameObject);

					if (simulatedSpeedOfImpact >= 2 * minSpeed) {
						float thumpVolume = thumpStartVolume + Mathf.Lerp(-volumeDelta, volumeDelta, Mathf.InverseLerp(2 * minSpeed, 2 * maxSpeed, simulatedSpeedOfImpact));
						thumpSound.audio.volume = thumpVolume;

						AudioManager.instance.PlayOnGameObject(AudioName.PlayerJumpLandingThump, id, gameObject);
					}

				}

				keepTrackOfPlayerAirHeight = false;
			}
		}

    }
}
