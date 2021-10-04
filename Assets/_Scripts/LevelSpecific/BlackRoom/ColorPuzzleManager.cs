using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
	public class ColorPuzzleManager : SaveableObject<ColorPuzzleManager, ColorPuzzleManager.ColorPuzzleManagerSave> {
		public FlashingColor smallGridOverlayFlash;
		public FlashingColor smallGridOutlineFlash;

		int _activePuzzle;
		public int activePuzzle {
			get { return _activePuzzle; }
			set {
				if (value == -1) {
					_activePuzzle = value;
					return;
				}
				if (value == puzzles.Length) {
					Debug.LogError("All puzzles solved!");
					return;
				}
				puzzles[activePuzzle].SetActive(false);
				puzzles[value].SetActive(true);
				_activePuzzle = value;
			}
		}
		ColorPuzzle[] puzzles;

		protected override void Awake() {
			base.Awake();
			puzzles = GetComponentsInChildren<ColorPuzzle>();
		}

		protected override void Init() {
			activePuzzle = -1;
		}

		private void Update() {
			if (Input.GetKeyDown(KeyCode.G)) {
				smallGridOverlayFlash.Flash(3);
				smallGridOutlineFlash.Flash(3);
			}
		}

		public bool CheckSolution() {
			if (activePuzzle == -1) {
				return false;
			}

			return puzzles[activePuzzle].solved;
		}

		#region Saving
		// There's only one player so we don't need a UniqueId here
		public override string ID => "ColorPuzzleManager";

		[Serializable]
		public class ColorPuzzleManagerSave : SerializableSaveObject<ColorPuzzleManager> {
			int activePuzzle;

			public ColorPuzzleManagerSave(ColorPuzzleManager puzzleManager) : base(puzzleManager) {
				this.activePuzzle = puzzleManager.activePuzzle;
			}

			public override void LoadSave(ColorPuzzleManager puzzleManager) {
				puzzleManager.activePuzzle = this.activePuzzle;
			}
		}
		#endregion
	}
}