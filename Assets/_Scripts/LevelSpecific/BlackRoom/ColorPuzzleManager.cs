using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
	public class ColorPuzzleManager : MonoBehaviour, SaveableObject {
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

		void Awake() {
			puzzles = GetComponentsInChildren<ColorPuzzle>();
		}

		IEnumerator Start() {
			ColorPuzzle.OnColorPuzzleSolvedStateChange += HandlePuzzleSolvedStateChange;
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
		public bool SkipSave { get; set; }
		// There's only one player so we don't need a UniqueId here
		public string ID => "ColorPuzzleManager";

		[Serializable]
		class ColorPuzzleManagerSave {
			bool hasSetActivePuzzleToZero;
			int activePuzzle;

			public ColorPuzzleManagerSave(ColorPuzzleManager puzzleManager) {
				this.hasSetActivePuzzleToZero = puzzleManager.hasSetActivePuzzleToZero;
				this.activePuzzle = puzzleManager.activePuzzle;
			}

			public void LoadSave(ColorPuzzleManager puzzleManager) {
				puzzleManager.hasSetActivePuzzleToZero = this.hasSetActivePuzzleToZero;
				puzzleManager.activePuzzle = this.activePuzzle;
			}
		}

		public object GetSaveObject() {
			return new ColorPuzzleManagerSave(this);
		}

		public void LoadFromSavedObject(object savedObject) {
			ColorPuzzleManagerSave save = savedObject as ColorPuzzleManagerSave;

			save.LoadSave(this);
		}
		#endregion
	}
}