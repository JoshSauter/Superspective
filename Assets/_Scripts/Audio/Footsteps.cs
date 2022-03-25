using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Audio;
using static Audio.AudioManager;

public class Footsteps : MonoBehaviour {
	private AudioJob audioJobLeft => AudioManager.instance.GetOrCreateJob(AudioName.PlayerFootstep, id + "_Left");
	private AudioJob audioJobRight => AudioManager.instance.GetOrCreateJob(AudioName.PlayerFootstep, id + "_Right");
	Headbob bob;
	PlayerMovement playerMovement;
	float defaultVolume;
	float curBobAmountUnamplified = 0;
	bool playerWasHeadingDownLastFrame = false;
	float minTimeBetweenHits = 0.175f;
	float timeSinceLastHit = 0f;

	// Alternates between true and false so we only play a sound every other step
	bool shouldForceStepSound = true;

	float panMagnitude = .05f;
	bool leftFootActive = true;

	// Used for creating and retrieving audio jobs
	string id = "Footsteps";

    void Start() {
		playerMovement = GetComponent<PlayerMovement>();
		playerMovement.OnStaircaseStepUp += () => { PlayFootstepAtVolume(shouldForceStepSound, 0.125f); shouldForceStepSound = !shouldForceStepSound; };

		bob = GetComponent<Headbob>();
		AudioManager.instance.GetOrCreateJob(AudioName.PlayerFootstep, id + "_Left", SetPanForAudio);
		AudioManager.instance.GetOrCreateJob(AudioName.PlayerFootstep, id + "_Right", SetPanForAudio);
		defaultVolume = audioJobLeft.audio.volume;

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
		float curVolume = Mathf.Lerp(defaultVolume - defaultVolume/2f, defaultVolume + defaultVolume / 2f, Mathf.InverseLerp(0f, 20f, playerSpeed));
		if (leftFootActive) {
			audioJobLeft.audio.volume = curVolume;
		}
		else {
			audioJobRight.audio.volume = curVolume;
		}

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

    void SetPanForAudio(AudioJob audioJob) {
	    bool isLeftFoot = audioJob.id.Contains("Left");
	    audioJob.audio.panStereo = isLeftFoot ? -panMagnitude : panMagnitude;
    }

	void PlayFootstepAtVolume(bool shouldForcePlay, float tempVolume) {
		AudioJob audioJob = leftFootActive ? audioJobLeft : audioJobRight;
		float tmp = audioJob.audio.volume;
		audioJob.audio.volume = tempVolume;
		PlayFootstep(shouldForcePlay);
		audioJob.audio.volume = tmp;
	}

	void PlayFootstep(bool shouldForcePlay) {
		// Since adding left/right foot sounds, shouldForcePlay is just actually whether it should play at all or not
		if (!shouldForcePlay) return;
		timeSinceLastHit = 0f;
		string audioJobId = leftFootActive ? audioJobLeft.uniqueIdentifier : audioJobRight.uniqueIdentifier;
		AudioManager.instance.Play(AudioName.PlayerFootstep, audioJobId, shouldForcePlay);
		leftFootActive = !leftFootActive;
	}
}
