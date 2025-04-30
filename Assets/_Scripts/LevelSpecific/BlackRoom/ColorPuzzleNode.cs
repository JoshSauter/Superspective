using System;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.BlackRoom {
	public class ColorPuzzleNode : MonoBehaviour { // Revisit saving: doesn't work because we're duplicating the object at runtime SuperspectiveObject<ColorPuzzleNode, ColorPuzzleNode.ColorPuzzleNodeSave> {
		//public override string ID => $"{this.FullPath()}.{GetType().Name}";
		
		public StateMachine<PuzzleNodeState> state;

		public enum PuzzleNodeState {
			NotSolved,
			Solved
		}
		public bool isSolved {
			get => state.State == PuzzleNodeState.Solved;
			private set {
				if (isSolved == value) return;

				thisRenderer.SetColor("_EmissionColor", value ? solutionEmissionColor : unsolvedEmissionColor);
				scaleWhenStateChanged = transform.localScale.x;
				state.Set(value ? PuzzleNodeState.Solved : PuzzleNodeState.NotSolved);
			}
		}
		public Color solution = Color.black;
		Color solutionColor;
		Color solutionEmissionColor;
		Color unsolvedEmissionColor = Color.black;
		public LightProjector red, green, blue;
		SuperspectiveRenderer thisRenderer;

		float scaleWhenStateChanged = minScale;
		const float minScale = 4f;
		const float maxScale = 8f;
		const float scaleChangeTime = .5f;
		public AnimationCurve scaleCurve;

		public delegate void SolutionNodeStateChange(ColorPuzzleNode node, bool solved);
		public static event SolutionNodeStateChange OnSolutionNodeStateChange;

		protected void Start() {
			//base.Start();
			
			thisRenderer = GetComponent<SuperspectiveRenderer>();
			solutionColor = thisRenderer.GetMainColor();
			solutionEmissionColor = thisRenderer.GetColor("_EmissionColor");
			thisRenderer.SetMainColor(solutionColor);
			thisRenderer.SetColor("_EmissionColor", unsolvedEmissionColor);
		}

		protected void OnEnable() {
			//base.OnEnable();
			
			state = this.StateMachine(PuzzleNodeState.NotSolved);
			
			state.OnStateChangeSimple += TriggerEvent;
		}

		protected void OnDisable() {
			//base.OnDisable();
			
			state.OnStateChangeSimple -= TriggerEvent;
		}

		void TriggerEvent() {
			OnSolutionNodeStateChange?.Invoke(this, isSolved);
		}

		void Update() {
			isSolved = solution == GetColor();
			float t = state.Time / scaleChangeTime;
			if (isSolved) {
				if (t < 1) {
					float size = Mathf.LerpUnclamped(scaleWhenStateChanged, maxScale, scaleCurve.Evaluate(t));
					transform.localScale = new Vector3(size, transform.localScale.y, size);
				}
				else {
					transform.localScale = new Vector3(maxScale, transform.localScale.y, maxScale);
				}
			}
			else {
				if (t < 1) {
					float size = Mathf.LerpUnclamped(scaleWhenStateChanged, minScale, scaleCurve.Evaluate(t));
					transform.localScale = new Vector3(size, transform.localScale.y, size);
				}
				else {
					transform.localScale = new Vector3(minScale, transform.localScale.y, minScale);
				}
			}
		}

		Color GetColor() {
			Color finalColor = Color.black;
			finalColor.r += GetColorValue(red);
			finalColor.g += GetColorValue(green);
			finalColor.b += GetColorValue(blue);

			return finalColor;
		}

		const float coneHeight = 120;
		const float coneRadius = 16;
		int GetColorValue(LightProjector projector) {
			bool isWithinCone = IsWithinCone(projector);
			bool isBlocked = IsBlockedByBlocker(projector);

			return (isWithinCone && !isBlocked) ? 1 : 0;
		}

		bool IsWithinCone(LightProjector projector) {
			Vector3 testPos = transform.position + Vector3.down;
			Transform cone = projector.transform.GetChild(0);
			if (!cone.gameObject.activeSelf) {
				return false;
			}
			Vector3 tipToCenterOfBaseWorld = -cone.up * (coneHeight * cone.localScale.y);
			Vector3 tipToEdgeOfBaseWorld = tipToCenterOfBaseWorld + cone.right * (coneRadius * cone.localScale.x);
			Vector3 tipToTestPointWorld = testPos - cone.position;
			// if (projector == green) {
			// 	Debug.DrawRay(cone.position, tipToCenterOfBaseWorld, Color.white);
			// 	Debug.DrawRay(cone.position, tipToEdgeOfBaseWorld, Color.blue);
			//
			// 	Debug.DrawRay(cone.position, tipToTestPointWorld, Color.red);
			// }

			float thresholdForWithinCone = Vector3.Dot(tipToCenterOfBaseWorld.normalized, tipToEdgeOfBaseWorld.normalized);
			float testValue = Vector3.Dot(tipToCenterOfBaseWorld.normalized, tipToTestPointWorld.normalized);

			return testValue > thresholdForWithinCone;
		}

		bool IsBlockedByBlocker(LightProjector projector) {
			UnityEngine.Transform lightSphere = projector.transform.GetChild(1);
			if (!lightSphere.gameObject.activeSelf) {
				return false;
			}
			Vector3 startPos = transform.position;
			Vector3 diff = lightSphere.position - startPos;
			Ray toProjector = new Ray(startPos, diff);
			float distance = diff.magnitude - 4;
			//Debug.DrawRay(toProjector.origin, toProjector.direction * distance);
			//Debug.DrawRay(projector.transform.position, projector.transform.right * distance);

			RaycastHit hitInfo;
			Physics.Raycast(toProjector, out hitInfo, distance);
			return Physics.Raycast(toProjector, distance);
		}
		
		
		//public override void LoadSave(ColorPuzzleNodeSave save) { }
		//
		//[Serializable]
		//public class ColorPuzzleNodeSave : SaveObject<ColorPuzzleNode> {
		//	public ColorPuzzleNodeSave(ColorPuzzleNode script) : base(script) { }
		//}

	}
}