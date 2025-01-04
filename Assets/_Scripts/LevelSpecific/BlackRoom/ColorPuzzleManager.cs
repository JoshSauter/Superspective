using Saving;
using System;

namespace LevelSpecific.BlackRoom {
	public class ColorPuzzleManager : SingletonSuperspectiveObject<ColorPuzzleManager, ColorPuzzleManager.ColorPuzzleManagerSave> {
		public FlashingColor smallGridOverlayFlash;
		public FlashingColor smallGridOutlineFlash;

		int _activePuzzle;
		public int ActivePuzzle {
			get => _activePuzzle;
			set {
				if (value == -1) {
					_activePuzzle = value;
					return;
				}
				if (value >= puzzles.Length) {
					// Debug.LogError("All puzzles solved!");
					return;
				}

				// Disable previous active puzzle
				if (ActivePuzzle != -1) {
					puzzles[ActivePuzzle].SetActive(false);
				}

				// Enable new active puzzle
				if (ActivePuzzle < puzzles.Length) {
					puzzles[value].SetActive(true);
				}

				_activePuzzle = value;
			}
		}

		public bool IsFirstPuzzle => ActivePuzzle == 0;
		public bool IsLastPuzzle => ActivePuzzle == puzzles.Length - 1;
		public ColorPuzzle[] puzzles;
		public int NumPuzzles => puzzles.Length;

		protected override void Awake() {
			base.Awake();
			puzzles = GetComponentsInChildren<ColorPuzzle>();
		}

		protected override void Init() {
			base.Init();
			ActivePuzzle = -1;
		}

		public bool CheckSolution(bool outOfRangeDefault = false) {
			if (ActivePuzzle == -1) {
				return outOfRangeDefault;
			}

			return puzzles[ActivePuzzle].solved;
		}

		public void FlashIncorrect() {
			smallGridOverlayFlash.Flash(3);
			smallGridOutlineFlash.Flash(3);
		}

#region Saving

		public override void LoadSave(ColorPuzzleManagerSave save) {
			ActivePuzzle = save.activePuzzle;
		}

		// There's only one of these so we don't need a UniqueId here
		public override string ID => "ColorPuzzleManager";

		[Serializable]
		public class ColorPuzzleManagerSave : SaveObject<ColorPuzzleManager> {
			public int activePuzzle;

			public ColorPuzzleManagerSave(ColorPuzzleManager puzzleManager) : base(puzzleManager) {
				this.activePuzzle = puzzleManager.ActivePuzzle;
			}
		}
#endregion
	}
}