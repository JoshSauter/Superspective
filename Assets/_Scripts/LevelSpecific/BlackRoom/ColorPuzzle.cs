using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using EpitaphUtils;

public class ColorPuzzle : MonoBehaviour {
	public Transform smallPuzzleParent;
	public bool solved = false;
	public bool isActive = false;
	ColorPuzzleNode[] solutionNodes;
	LightBlocker[] lightBlockers;
	Transform smallPuzzle;

	Vector3 activePos;
	Vector3 inactivePos;

	public delegate void ColorPuzzleSolvedStateChange(ColorPuzzle puzzle, bool isSolved);
	public static event ColorPuzzleSolvedStateChange OnColorPuzzleSolvedStateChange;

    void Start() {
		solutionNodes = GetComponentsInChildren<ColorPuzzleNode>();
		lightBlockers = GetComponentsInChildren<LightBlocker>();
		smallPuzzle = CreateBigPuzzlePieces().transform;
		ColorPuzzleNode.OnSolutionNodeStateChange += HandleSolutionNodeStateChange;

		activePos = transform.localPosition;
		inactivePos = activePos + 20f * Vector3.down;
		SetActive(isActive);
    }

	void Update() {
		transform.localPosition = Vector3.Lerp(transform.localPosition, (isActive) ? activePos : inactivePos, 5 * Time.deltaTime);
		smallPuzzle.localPosition = transform.localPosition;
	}

	void HandleSolutionNodeStateChange(ColorPuzzleNode node, bool isSolved) {
		if (solutionNodes.Contains(node)) {
			bool prevSolvedState = solved;
			solved = solutionNodes.ToList().TrueForAll(n => n.isSolved);
			if (prevSolvedState != solved && OnColorPuzzleSolvedStateChange != null) {
				OnColorPuzzleSolvedStateChange(this, solved);
			}
		}
	}

	GameObject CreateBigPuzzlePieces() {
		GameObject bigPuzzle = Instantiate(this, smallPuzzleParent, false).gameObject;
		DestroyImmediate(bigPuzzle.GetComponent<ColorPuzzle>());

		foreach (Transform t in bigPuzzle.transform) {
			ColorPuzzleNode node = t.GetComponent<ColorPuzzleNode>();
			LightBlocker lightBlocker = t.GetComponent<LightBlocker>();
			if (node != null) DestroyImmediate(node);
			if (lightBlocker != null) DestroyImmediate(lightBlocker);
		}

		return bigPuzzle;
	}

	public void SetActive(bool active) {
		foreach (ColorPuzzleNode node in solutionNodes) {
			node.gameObject.SetActive(active);
		}
		foreach (LightBlocker lb in lightBlockers) {
			foreach (LightBlocker.LightSourceBlocker blocker in lb.blockers) {
				if (blocker.blocker != null) {
					blocker.blocker.SetActive(active);
				}
			}
			lb.gameObject.SetActive(active);
		}
		isActive = active;
	}
}
