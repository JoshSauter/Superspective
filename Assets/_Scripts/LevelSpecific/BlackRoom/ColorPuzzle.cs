using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SuperspectiveUtils;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
	public class ColorPuzzle : SaveableObject<ColorPuzzle, ColorPuzzle.ColorPuzzleSave> {
		public Transform smallPuzzleParent;
		public bool solved = false;
		public bool isActive = false;
		ColorPuzzleNode[] solutionNodes;
		LightBlocker[] lightBlockers;
		Transform smallPuzzle;

		Vector3 activePos;
		Vector3 inactivePos;

		protected override void Start() {
			base.Start();
			solutionNodes = GetComponentsInChildren<ColorPuzzleNode>();
			lightBlockers = GetComponentsInChildren<LightBlocker>();
			smallPuzzle = CreateSmallPuzzlePieces().transform;
			ColorPuzzleNode.OnSolutionNodeStateChange += HandleSolutionNodeStateChange;

			activePos = transform.localPosition;
			inactivePos = activePos + 20f * Vector3.down;
			SetActive(isActive);
		}

		void Update() {
			transform.localPosition = Vector3.Lerp(transform.localPosition, (isActive) ? activePos : inactivePos, 5 * Time.deltaTime);
			if (smallPuzzle != null) {
				smallPuzzle.localPosition = transform.localPosition;
			}
		}

		void HandleSolutionNodeStateChange(ColorPuzzleNode node, bool isSolved) {
			if (solutionNodes.Contains(node)) {
				bool prevSolvedState = solved;
				solved = solutionNodes.ToList().TrueForAll(n => n.isSolved);
			}
		}

		GameObject CreateSmallPuzzlePieces() {
			GameObject smallPuzzle = Instantiate(this, smallPuzzleParent, false).gameObject;
			DestroyImmediate(smallPuzzle.GetComponent<ColorPuzzle>());

			foreach (Transform t in smallPuzzle.transform) {
				ColorPuzzleNode node = t.GetComponent<ColorPuzzleNode>();
				LightBlocker lightBlocker = t.GetComponent<LightBlocker>();
				if (node != null) DestroyImmediate(node);
				if (lightBlocker != null) DestroyImmediate(lightBlocker);
			}

			return smallPuzzle;
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

#region Saving
		public override string ID => $"BlackRoom_ColorPuzzle{transform.GetSiblingIndex()}";

		[Serializable]
		public class ColorPuzzleSave : SerializableSaveObject<ColorPuzzle> {
			bool solved;
			bool isActive;
			public ColorPuzzleSave(ColorPuzzle colorPuzzle) : base(colorPuzzle) {
				this.solved = colorPuzzle.solved;
				this.isActive = colorPuzzle.isActive;
			}

			public override void LoadSave(ColorPuzzle colorPuzzle) {
				colorPuzzle.solved = this.solved;
				colorPuzzle.isActive = this.isActive;
			}
		}
	}
#endregion
}