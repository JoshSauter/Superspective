using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour {
	protected InteractableObject interactableObject;
#region events
	public delegate void ButtonAction(Button button);
	public event ButtonAction OnButtonPressBegin;
	public event ButtonAction OnButtonPressFinish;
	public event ButtonAction OnButtonDepressBegin;
	public event ButtonAction OnButtonDepressFinish;
#endregion

	public AnimationCurve buttonPressCurve;
	public AnimationCurve buttonDepressCurve;
	public float timeToPressButton = 1f;
	public float timeToDepressButton = 0.5f;
	public float depressDistance = 1f;
	protected bool inButtonPressOrDepressCoroutine = false;
	public bool buttonPressed = false;

	public bool depressAfterPress = false;
	public float timeBetweenPressEndDepressStart = 0.5f;
	public float deadTimeAfterButtonPress = 0;
	public float deadTimeAfterButtonDepress = 0;

	public virtual void OnLeftMouseButton() { PressButton(); }

	public virtual void Awake() {
		interactableObject = GetComponent<InteractableObject>();
		if (interactableObject == null) {
			interactableObject = gameObject.AddComponent<InteractableObject>();
			interactableObject.OnLeftMouseButton += OnLeftMouseButton;
		}
	}

	public void PressButton() {
		if (!inButtonPressOrDepressCoroutine && !buttonPressed) {
			StartCoroutine(ButtonPress());
		}
		else if (!inButtonPressOrDepressCoroutine && buttonPressed) {
			StartCoroutine(ButtonDepress());
		}
	}

	virtual protected IEnumerator ButtonPress() {
		inButtonPressOrDepressCoroutine = true;
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
		inButtonPressOrDepressCoroutine = false;
		buttonPressed = true;

		TriggerButtonPressFinishEvents();

		if (depressAfterPress) {
			yield return new WaitForSeconds(timeBetweenPressEndDepressStart);
			StartCoroutine(ButtonDepress());
		}
	}

	virtual protected IEnumerator ButtonDepress() {
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

	protected void TriggerButtonPressBeginEvents() {
		if (OnButtonPressBegin != null) OnButtonPressBegin(this);
	}

	protected void TriggerButtonPressFinishEvents() {
		if (OnButtonPressFinish != null) OnButtonPressFinish(this);
	}

	protected void TriggerButtonDepressBeginEvents() {
		if (OnButtonDepressBegin != null) OnButtonDepressBegin(this);
	}

	protected void TriggerButtonDepressFinishEvents() {
		if (OnButtonDepressFinish != null) OnButtonDepressFinish(this);
	}
}
