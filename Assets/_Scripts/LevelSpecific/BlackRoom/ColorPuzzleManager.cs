using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
	public class ColorPuzzleManager : SaveableObject<ColorPuzzleManager, ColorPuzzleManager.ColorPuzzleManagerSave> {
		bool hasSetActivePuzzleToZero = false;
		int _activePuzzle;
		public int activePuzzle {
			get { return _activePuzzle; }
			set {
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

		protected override void Start() {
			base.Start();
			ColorPuzzle.OnColorPuzzleSolvedStateChange += HandlePuzzleSolvedStateChange;
			StartCoroutine(Initialize());
		}

		IEnumerator Initialize() {
			yield return null;
			if (!hasSetActivePuzzleToZero) {
				hasSetActivePuzzleToZero = true;
				activePuzzle = 0;
			}
		}

		void HandlePuzzleSolvedStateChange(ColorPuzzle puzzle, bool solved) {
			if (puzzles.ToList().Contains(puzzle)) {
				if (solved) {
					activePuzzle++;
				}
			}
		}

		#region Saving
		// There's only one player so we don't need a UniqueId here
		public override string ID => "ColorPuzzleManager";

		[Serializable]
		public class ColorPuzzleManagerSave : SerializableSaveObject<ColorPuzzleManager> {
			bool hasSetActivePuzzleToZero;
			int activePuzzle;

			public ColorPuzzleManagerSave(ColorPuzzleManager puzzleManager) : base(puzzleManager) {
				this.hasSetActivePuzzleToZero = puzzleManager.hasSetActivePuzzleToZero;
				this.activePuzzle = puzzleManager.activePuzzle;
			}

			public override void LoadSave(ColorPuzzleManager puzzleManager) {
				puzzleManager.hasSetActivePuzzleToZero = this.hasSetActivePuzzleToZero;
				puzzleManager.activePuzzle = this.activePuzzle;
			}
		}
		#endregion
	}
}