using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Audio;
using static Audio.AudioManager;

public class Footsteps : MonoBehaviour {
	AudioJob audioJob;
	Headbob bob;
	PlayerMovement playerMovement;
	float defaultVolume;
	float curBobAmountUnamplified = 0;
	bool playerWasHeadingDownLastFrame = false;
	float minTimeBetweenHits = 0.25f;
	float timeSinceLastHit = 0f;

	// Alternates between true and false so we only play a sound every other step
	bool shouldForceStepSound = true;

	// Used for creating and retrieving audio jobs
	string id = "Footsteps";

    void Start() {
		playerMovement = GetComponent<PlayerMovement>();
		playerMovement.OnStaircaseStepUp += () => { PlayFootstepAtVolume(shouldForceStepSound, 0.125f); shouldForceStepSound = !shouldForceStepSound; };

		bob = GetComponent<Headbob>();
		audioJob = AudioManager.instance.GetOrCreateJob(AudioName.PlayerFootstep, id);
		defaultVolume = audioJob.audio.volume;

		if (bob == null) {
			Debug.LogWarning("Footsteps requires headbob info");
			this.enabled = false;
		}
	}

    void LateUpdate() {
		timeSinceLastHit += Time.deltaTime;
		bool shouldForcePlay = timeSinceLastHit > minTimeBetweenHits;

		Vector3 playerVelocity = playerMovement.ProjectedHorizontalVelocity();
		float playerSpeed = playerVelocity.magnitude;
		audioJob.audio.volume = Mathf.Lerp(defaultVolume - defaultVolume/2f, defaultVolume + defaultVolume / 2f, Mathf.InverseLerp(0f, 20f, playerSpeed));
		if (playerMovement.grounded.isGrounded && playerSpeed > 0.2f) {
			float thisFrameBobAmount = bob.viewBobCurve.Evaluate(bob.t);
			float thisFrameOffset = thisFrameBobAmount - curBobAmountUnamplified;
			curBobAmountUnamplified = thisFrameBobAmount;
			bool playerIsHeadingUpThisFrame = thisFrameOffset > 0;
			if (playerWasHeadingDownLastFrame && playerIsHeadingUpThisFrame) {
				PlayFootstep(shouldForcePlay);
				timeSinceLastHit = 0f;
			}
			playerWasHeadingDownLastFrame = !playerIsHeadingUpThisFrame;
		}
		else {
			if (playerWasHeadingDownLastFrame) {
				PlayFootstep(shouldForcePlay);
			}
			playerWasHeadingDownLastFrame = false;
		}
	}

	void PlayFootstepAtVolume(bool shouldForcePlay, float tempVolume) {
		float tmp = audioJob.audio.volume;
		audioJob.audio.volume = tempVolume;
		PlayFootstep(shouldForcePlay);
		audioJob.audio.volume = tmp;
	}

	void PlayFootstep(bool shouldForcePlay) {
		timeSinceLastHit = 0f;
		AudioManager.instance.Play(AudioName.PlayerFootstep, id, shouldForcePlay);
	}
}
