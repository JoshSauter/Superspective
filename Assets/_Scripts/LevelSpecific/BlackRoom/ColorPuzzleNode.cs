using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorPuzzleNode : MonoBehaviour {
	private bool _isSolved = false;
	public bool isSolved {
		get { return _isSolved; }
		set {
			_isSolved = value;
			if (OnSolutionNodeStateChange != null) {
				OnSolutionNodeStateChange(this, value);
			}
		}
	}
	public Color solution = Color.black;
	public Color unsolvedColor = Color.white;
	Color unsolvedEmissionColor = Color.black;
	Color solutionColor;
	Color solutionEmissionColor;
	public LightProjector red, green, blue;
	EpitaphRenderer thisRenderer;

	public delegate void SolutionNodeStateChange(ColorPuzzleNode node, bool solved);
	public static event SolutionNodeStateChange OnSolutionNodeStateChange;

    void Start() {
		thisRenderer = GetComponent<EpitaphRenderer>();
		solutionColor = thisRenderer.GetMainColor();
		solutionEmissionColor = thisRenderer.GetColor("_EmissionColor");
    }

    void Update() {
		isSolved = solution == GetColor();
		if (isSolved) {
			thisRenderer.SetMainColor(solutionColor);
			thisRenderer.SetColor("_EmissionColor", solutionEmissionColor);
		}
		else {
			thisRenderer.SetMainColor(unsolvedColor);
			thisRenderer.SetColor("_EmissionColor", unsolvedEmissionColor);
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
		Vector3 testPos = transform.position;
		Transform cone = projector.transform.GetChild(0);
		Vector3 tipToCenterOfBaseWorld = -cone.up * coneHeight * cone.localScale.y;
		Vector3 tipToEdgeOfBaseWorld = tipToCenterOfBaseWorld + cone.right * coneRadius * cone.localScale.x;
		Vector3 tipToTestPointWorld = testPos - cone.position;
		if (projector == green) {
			Debug.DrawRay(cone.position, tipToCenterOfBaseWorld, Color.white);
			Debug.DrawRay(cone.position, tipToEdgeOfBaseWorld, Color.blue);

			Debug.DrawRay(cone.position, tipToTestPointWorld, Color.red);
		}

		float thresholdForWithinCone = Vector3.Dot(tipToCenterOfBaseWorld.normalized, tipToEdgeOfBaseWorld.normalized);
		float testValue = Vector3.Dot(tipToCenterOfBaseWorld.normalized, tipToTestPointWorld.normalized);

		return testValue > thresholdForWithinCone;
	}

	bool IsBlockedByBlocker(LightProjector projector) {
		Vector3 startPos = transform.position;
		Vector3 diff = projector.transform.GetChild(1).position - startPos;
		Ray toProjector = new Ray(startPos, diff);
		float distance = diff.magnitude - 4;
		//Debug.DrawRay(toProjector.origin, toProjector.direction * distance);
		//Debug.DrawRay(projector.transform.position, projector.transform.right * distance);

		RaycastHit hitInfo;
		Physics.Raycast(toProjector, out hitInfo, distance);
		return Physics.Raycast(toProjector, distance);
	}
}
