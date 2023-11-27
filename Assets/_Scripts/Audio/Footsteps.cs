using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Audio;
using static Audio.AudioManager;

public class Footsteps : MonoBehaviour {
	private AudioJob audioJobLeft => AudioManager.instance.GetOrCreateJob(AudioName.PlayerFootstep, id + "_Left", SetPanForAudio);
	private AudioJob audioJobRight => AudioManager.instance.GetOrCreateJob(AudioName.PlayerFootstep, id + "_Right", SetPanForAudio);
	
	private AudioJob audioJobLeftGlass => AudioManager.instance.GetOrCreateJob(AudioName.PlayerFootstep, id + "_Left_Glass", SetPanForAudio);
	private AudioJob audioJobRightGlass => AudioManager.instance.GetOrCreateJob(AudioName.PlayerFootstep, id + "_Right_Glass", SetPanForAudio);
	
	Headbob bob;
	PlayerMovement playerMovement;
	float defaultVolume;
	float curBobAmountUnamplified = 0;
	bool playerWasHeadingDownLastFrame = false;
	float minTimeBetweenHits = 0.175f;
	float timeSinceLastHit = 0f;

	private const float glassMinVolume = 0.05f;
	private const float glassMaxVolume = 0.3f;
	private float lastGlassVolume = glassMinVolume;

	private float randomGlassVolume {
		get {
			float nextGlassVolume = Mathf.Lerp(lastGlassVolume, Random.Range(glassMinVolume, glassMaxVolume), 0.5f);
			lastGlassVolume = nextGlassVolume;
			return lastGlassVolume;
		}
	} 

	// Alternates between true and false so we only play a sound every other step
	bool shouldForceStepSound = true;

	float panMagnitude = .05f;
	bool leftFootActive = true;

	// Used for creating and retrieving audio jobs
	string id = "Footsteps";

    void Start() {
		playerMovement = GetComponent<PlayerMovement>();
		playerMovement.OnStaircaseStepUp += () => {
			PlayFootstep(shouldForceStepSound, 0.125f);
			shouldForceStepSound = !shouldForceStepSound;
		};

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

		if (playerMovement.IsGrounded && playerSpeed > 0.2f) {
			float thisFrameBobAmount = bob.viewBobCurve.Evaluate(bob.t);
			float thisFrameOffset = thisFrameBobAmount - curBobAmountUnamplified;
			curBobAmountUnamplified = thisFrameBobAmount;
			bool playerIsHeadingUpThisFrame = thisFrameOffset > 0;
			if (playerWasHeadingDownLastFrame && playerIsHeadingUpThisFrame) {
				PlayFootstep(shouldForcePlay, curVolume);
				timeSinceLastHit = 0f;
			}
			playerWasHeadingDownLastFrame = !playerIsHeadingUpThisFrame;
		}
		else {
			if (playerWasHeadingDownLastFrame) {
				PlayFootstep(shouldForcePlay, curVolume);
			}
			playerWasHeadingDownLastFrame = false;
		}
	}

    void SetPanForAudio(AudioJob audioJob) {
	    bool isLeftFoot = audioJob.id.Contains("Left");
	    audioJob.audio.panStereo = isLeftFoot ? -panMagnitude : panMagnitude;
    }

	void PlayFootstep(bool shouldForcePlay, float volume) {
		// Since adding left/right foot sounds, shouldForcePlay is just actually whether it should play at all or not
		if (!shouldForcePlay) return;
		timeSinceLastHit = 0f;
		string audioJobId = leftFootActive ? audioJobLeft.uniqueIdentifier : audioJobRight.uniqueIdentifier;
		AudioManager.instance.Play(AudioName.PlayerFootstep, audioJobId, shouldForcePlay, (audio) => audio.baseVolume = volume);
		if (playerMovement.groundMovement.WalkingOnGlass()) {
			string glassAudioJobId = leftFootActive ? audioJobLeftGlass.uniqueIdentifier : audioJobRightGlass.uniqueIdentifier;
			AudioManager.instance.Play(AudioName.PlayerFootstepGlass, glassAudioJobId, shouldForcePlay, SetGlassVolume);
		}

		leftFootActive = !leftFootActive;
	}

	void SetGlassVolume(AudioJob audioJob) {
		audioJob.audio.volume = randomGlassVolume;
	}
}
