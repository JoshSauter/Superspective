using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonHold : Button {

	bool buttonHeld = false;

#region events
	public event ButtonAction OnButtonHeld;
	#endregion

	override public void Awake() {
		base.Awake();

		interactableObject.OnLeftMouseButtonUp += OnLeftMouseButtonUp;
		interactableObject.OnMouseHoverExit += OnLeftMouseButtonFocusLost;
	}

	public void OnLeftMouseButtonUp() {
		ButtonNoLongerHeld();
	}

	public void OnLeftMouseButtonFocusLost() {
		ButtonNoLongerHeld();
	}

	protected override IEnumerator ButtonPress() {
		inButtonPressOrDepressCoroutine = true;
		buttonHeld = true;
		Vector3 startPos = transform.position;
		Vector3 endPos = startPos + transform.up * depressDistance;

		TriggerButtonPressBeginEvents();

		float timeElapsed = 0;
		while (timeElapsed < timeToPressButton) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / timeToPressButton;

			transform.position = Vector3.Lerp(startPos, endPos, buttonPressCurve.Evaluate(t));

			yield return null;
		}

		transform.position = endPos;

		yield return new WaitForSeconds(deadTimeAfterButtonPress);

		// State here: Button pressed
		while (buttonHeld) {
			TriggerButtonHeldEvents();

			yield return null;
		}
		inButtonPressOrDepressCoroutine = false;
		buttonPressed = true;

		TriggerButtonPressFinishEvents();

		yield return new WaitForSeconds(timeBetweenPressEndDepressStart);
		StartCoroutine(ButtonDepress());
	}

	protected override IEnumerator ButtonDepress() {
		inButtonPressOrDepressCoroutine = true;
		Vector3 startPos = transform.position;
		Vector3 endPos = startPos - transform.up * depressDistance;

		TriggerButtonDepressBeginEvents();

		float timeElapsed = 0;
		while (timeElapsed < timeToDepressButton) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / timeToDepressButton;

			transform.position = Vector3.Lerp(startPos, endPos, buttonDepressCurve.Evaluate(t));

			yield return null;
		}

		transform.position = endPos;

		yield return new WaitForSeconds(deadTimeAfterButtonDepress);
		inButtonPressOrDepressCoroutine = false;
		buttonPressed = false;

		TriggerButtonDepressFinishEvents();
	}

	protected void TriggerButtonHeldEvents() {
		if (OnButtonHeld != null) OnButtonHeld(this);
	}

	private void ButtonNoLongerHeld() {
		buttonHeld = false;
	}
}
