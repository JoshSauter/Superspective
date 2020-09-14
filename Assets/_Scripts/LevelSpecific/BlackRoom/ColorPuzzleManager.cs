using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LevelSpecific.BlackRoom {
	public class ColorPuzzleManager : MonoBehaviour {
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

		IEnumerator Start() {
			puzzles = GetComponentsInChildren<ColorPuzzle>();
			ColorPuzzle.OnColorPuzzleSolvedStateChange += HandlePuzzleSolvedStateChange;
			yield return null;
			activePuzzle = 0;
		}

		void HandlePuzzleSolvedStateChange(ColorPuzzle puzzle, bool solved) {
			if (puzzles.ToList().Contains(puzzle)) {
				if (solved) {
					activePuzzle++;
				}
			}
		}
	}
}