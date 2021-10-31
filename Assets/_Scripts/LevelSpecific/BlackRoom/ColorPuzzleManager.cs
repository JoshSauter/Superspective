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

				if (activePuzzle != -1) {
					puzzles[activePuzzle].SetActive(false);
				}

				if (activePuzzle < puzzles.Length) {
					puzzles[value].SetActive(true);
				}

				_activePuzzle = value;
			}
		}
		ColorPuzzle[] puzzles;
		public int numPuzzles => puzzles.Length;

		protected override void Awake() {
			base.Awake();
			puzzles = GetComponentsInChildren<ColorPuzzle>();
		}

		protected override void Init() {
			activePuzzle = -1;
		}

		public bool CheckSolution(bool outOfRangeDefault = false) {
			if (activePuzzle == -1) {
				return outOfRangeDefault;
			}

			return puzzles[activePuzzle].solved;
		}

		public void FlashIncorrect() {
			smallGridOverlayFlash.Flash(3);
			smallGridOutlineFlash.Flash(3);
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