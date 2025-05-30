using SuperspectiveUtils;
using UnityEngine;

namespace Audio {
	// next 61
	public enum AudioName {
		CubeDrop = 0,
		CubePickup = 1,
		CubeImpact = 2,
		ReceptacleEnter = 3,
		ReceptacleExit = 4,
		ElevatorClose = 5,
		ElevatorOpen = 6,
		ElevatorMove = 7,
		ElectricalHum = 8,
		PowerTrailHum = 9,
		PowerTrailBootup = 34,
		PowerTrailShutdown = 35,
		ViewLockObject = 10,
		PlayerFootstep = 11,
		PlayerFootstepGlass = 39,
		PlayerJumpLandingRuffle = 12,
		PlayerJumpLandingThump = 13,
		PlayerJump = 14,
		SoundPuzzleBoxes = 15,
		SoundPuzzleStraightLines = 16,
		SoundPuzzleSymbols = 17,
		SoundPuzzleWiggle = 18,
		MetalCreak = 19,
		LoopingMachinery = 20,
		LoopingMachineryMassive = 48,
		Rainstick = 21,
		RainstickFast = 27,
		DrumSingleHitLow = 22,
		LowPulse = 23,
		EmptyVoid_8152358 = 24,
		LightSwitch = 25,
		LaserBeamShort = 26,
		CubeSpawnerSpawn = 28,
		CubeSpawnerClose = 30,
		CubeSpawnerDespawn = 33,
		DisabledSound = 29,
		FallingWind = 31,
		FallingWindLow = 32,
		InteractableHover = 36,
		ButtonPress = 37,
		ButtonUnpress = 38,
		PortalDoorMovingStart = 40,
		PortalDoorMoving = 41,
		PortalDoorMovingEnd = 42,
		LaserLoopStart = 43,
		LaserLoop = 44,
		AirWhoosh = 45,
		IncorrectAnswer = 46,
		CorrectAnswer = 47,
		WhiteNoiseSpacey = 51,
		WallsShifting = 52,
		MachineClick = 53,
		MachineOn = 54,
		MachineOff = 55,
		LeverSnapDown = 57,
		LeverSnapUp = 58,
		LeverCrank = 59,
		// UI Sounds
		UI_ShortBlip = 49,
		UI_HoverBlip = 50,
		LevelChangeSting = 56,
		// Music
		Music_Dusk = 60,
	}

	public static class AudioNameExt {
		private static AudioClip[] _glassFootstepClips;
		private static AudioClip[] _correctAnswerClips;

		private static AudioClip[] glassFootstepClips {
			get {
				if (_glassFootstepClips == null || _glassFootstepClips.Length == 0) {
					_glassFootstepClips = Resources.LoadAll<AudioClip>("Audio/Sounds/PlayerSounds/Scale/");
				}

				return _glassFootstepClips;
			}
		}

		private static AudioClip[] correctAnswerClips {
			get {
				if (_correctAnswerClips == null || _correctAnswerClips.Length == 0) {
					_correctAnswerClips = Resources.LoadAll<AudioClip>("Audio/Sounds/PuzzleSounds/CorrectAnswer/");
				}

				return _correctAnswerClips;
			}
		}
		
		public static AudioClip GetAudioClip(this AudioName audioType, AudioSource source) {
			if (source == null) {
				Debug.LogError("Can't get audio clip for " + audioType + " because source is null");
				return null;
			}
			switch (audioType) {
				case AudioName.PlayerFootstepGlass:
					return glassFootstepClips.DifferentRandomElementFrom();
				case AudioName.CorrectAnswer:
					return correctAnswerClips.DifferentRandomElementFrom();
				default:
					return source.clip;
			}
		}
		
		public static AudioManager.AudioType SfxOrMusic(this AudioName audioType) {
			// TODO: Expand this as needed (probably with a switch or something)
			return audioType == AudioName.Music_Dusk ? AudioManager.AudioType.Music : AudioManager.AudioType.SFX;
		}
	}
}
