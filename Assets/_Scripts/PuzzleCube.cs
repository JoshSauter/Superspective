using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleCube : MonoBehaviour {
	public PuzzlePanel puzzle;
	UnityEngine.Transform cubesParent;

	// Use this for initialization
	void Start () {
		puzzle.OnPuzzlePanelStateChanged += HandlePuzzleSolvedStateChange;
		cubesParent = transform.Find("Cubes");
	}
	
	void HandlePuzzleSolvedStateChange(bool solved) {
		cubesParent.gameObject.SetActive(solved);
	}
}
