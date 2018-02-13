using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single place for all HCI to be processed and output as events.
/// ButtonPressEvents are only called when a button is first pressed.
/// ButtonReleasedEvents are only called when a button is released from being held.
/// ButtonHeldEvents are called each frame that a button continues to be held.
/// TODO: Add controller support here
/// </summary>
public class PlayerButtonInput : Singleton<PlayerButtonInput> {

#region events
	public delegate void ButtonPressEvent();
	public delegate void ButtonReleaseEvent();
	public delegate void ButtonHeldEvent(float durationHeld);

	public event ButtonPressEvent OnUpPress;
	public event ButtonPressEvent OnDownPress;
	public event ButtonPressEvent OnRightPress;
	public event ButtonPressEvent OnLeftPress;
	public event ButtonPressEvent OnEscapePress;
	public event ButtonPressEvent OnSpacePress;
	public event ButtonPressEvent OnShiftPress;

	public event ButtonReleaseEvent OnUpRelease;
	public event ButtonReleaseEvent OnDownRelease;
	public event ButtonReleaseEvent OnRightRelease;
	public event ButtonReleaseEvent OnLeftRelease;
	public event ButtonReleaseEvent OnEscapeRelease;
	public event ButtonReleaseEvent OnSpaceRelease;
	public event ButtonReleaseEvent OnShiftRelease;

	public event ButtonHeldEvent OnUpHeld;
	public event ButtonHeldEvent OnDownHeld;
	public event ButtonHeldEvent OnRightHeld;
	public event ButtonHeldEvent OnLeftHeld;
	public event ButtonHeldEvent OnEscapeHeld;
	public event ButtonHeldEvent OnSpaceHeld;
	public event ButtonHeldEvent OnShiftHeld;
#endregion

#region Coroutine bools
	private bool inUpHoldCoroutine = false;
	private bool inDownHoldCoroutine = false;
	private bool inRightHoldCoroutine = false;
	private bool inLeftHoldCoroutine = false;
	private bool inEscapeHoldCoroutine = false;
	private bool inSpaceHoldCoroutine = false;
	private bool inShiftHoldCoroutine = false;
	#endregion
	
	public void Update() {
		// Up button
		if (UpPressed && OnUpPress != null) {
			OnUpPress();
			if (!inUpHoldCoroutine) {
				StartCoroutine(UpHoldCoroutine());
			}
		}
		else if (UpReleased && OnUpRelease != null) {
			OnUpRelease();
		}
		// Down button
		if (DownPressed && OnDownPress != null) {
			OnDownPress();
			if (!inDownHoldCoroutine) {
				StartCoroutine(DownHoldCoroutine());
			}
		}
		else if (DownReleased && OnDownRelease != null) {
			OnDownRelease();
		}
		// Right button
		if (RightPressed && OnRightPress != null) {
			OnRightPress();
			if (!inRightHoldCoroutine) {
				StartCoroutine(RightHoldCoroutine());
			}
		}
		else if (RightReleased && OnRightRelease != null) {
			OnRightRelease();
		}
		// Left button
		if (LeftPressed && OnLeftPress != null) {
			OnLeftPress();
			if (!inLeftHoldCoroutine) {
				StartCoroutine(LeftHoldCoroutine());
			}
		}
		else if (LeftReleased && OnLeftRelease != null) {
			OnLeftRelease();
		}
		// Escape button
		if (EscapePressed && OnEscapePress != null) {
			OnEscapePress();
			if (!inEscapeHoldCoroutine) {
				StartCoroutine(EscapeHoldCoroutine());
			}
		}
		else if (EscapeReleased && OnEscapeRelease != null) {
			OnEscapeRelease();
		}
		// Space button
		if (SpacePressed && OnSpacePress != null) {
			OnSpacePress();
			if (!inSpaceHoldCoroutine) {
				StartCoroutine(SpaceHoldCoroutine());
			}
		}
		else if (SpaceReleased && OnSpaceRelease != null) {
			OnSpaceRelease();
		}
		// Shift button
		if (ShiftPressed && OnShiftPress != null) {
			OnShiftPress();
			if (!inShiftHoldCoroutine) {
				StartCoroutine(ShiftHoldCoroutine());
			}
		}
		else if (ShiftReleased && OnShiftRelease != null) {
			OnShiftRelease();
		}

	}

#region Combined inputs
	// On first press
	public bool UpPressed {
		get { return Input.GetKeyDown(KeyCode.W); }
	}
	public bool DownPressed {
		get { return Input.GetKeyDown(KeyCode.S); }
	}
	public bool RightPressed {
		get { return Input.GetKeyDown(KeyCode.D); }
	}
	public bool LeftPressed {
		get { return Input.GetKeyDown(KeyCode.A); }
	}
	public bool EscapePressed {
		get { return Input.GetKeyDown(KeyCode.Escape); }
	}
	public bool SpacePressed {
		get { return Input.GetKeyDown(KeyCode.Space); }
	}
	public bool ShiftPressed {
		get { return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift); }
	}

	// On button release
	public bool UpReleased {
		get { return Input.GetKeyUp(KeyCode.W); }
	}
	public bool DownReleased {
		get { return Input.GetKeyUp(KeyCode.S); }
	}
	public bool RightReleased {
		get { return Input.GetKeyUp(KeyCode.D); }
	}
	public bool LeftReleased {
		get { return Input.GetKeyUp(KeyCode.A); }
	}
	public bool EscapeReleased {
		get { return Input.GetKeyUp(KeyCode.Escape); }
	}
	public bool SpaceReleased {
		get { return Input.GetKeyUp(KeyCode.Space); }
	}
	public bool ShiftReleased {
		get { return Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift); }
	}

	// While button held
	public bool UpHeld {
		get { return Input.GetKey(KeyCode.W); }
	}
	public bool DownHeld {
		get { return Input.GetKey(KeyCode.S); }
	}
	public bool RightHeld {
		get { return Input.GetKey(KeyCode.D); }
	}
	public bool LeftHeld {
		get { return Input.GetKey(KeyCode.A); }
	}
	public bool EscapeHeld {
		get { return Input.GetKey(KeyCode.Escape); }
	}
	public bool SpaceHeld {
		get { return Input.GetKey(KeyCode.Space); }
	}
	public bool ShiftHeld {
		get { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
	}
#endregion

#region ButtonHeld Coroutines
	IEnumerator UpHoldCoroutine() {
		inUpHoldCoroutine = true;
		
		float timeHeld = 0;
		while (UpHeld) {
			if (OnUpHeld != null) {
				OnUpHeld(timeHeld);

				timeHeld += Time.deltaTime;
				yield return null;
			}
		}

		inUpHoldCoroutine = false;
	}
	IEnumerator DownHoldCoroutine() {
		inDownHoldCoroutine = true;

		float timeHeld = 0;
		while (DownHeld) {
			if (OnDownHeld != null) {
				OnDownHeld(timeHeld);

				timeHeld += Time.deltaTime;
				yield return null;
			}
		}

		inDownHoldCoroutine = false;
	}
	IEnumerator RightHoldCoroutine() {
		inRightHoldCoroutine = true;

		float timeHeld = 0;
		while (RightHeld) {
			if (OnRightHeld != null) {
				OnRightHeld(timeHeld);

				timeHeld += Time.deltaTime;
				yield return null;
			}
		}

		inRightHoldCoroutine = false;
	}
	IEnumerator LeftHoldCoroutine() {
		inLeftHoldCoroutine = true;

		float timeHeld = 0;
		while (LeftHeld) {
			if (OnLeftHeld != null) {
				OnLeftHeld(timeHeld);

				timeHeld += Time.deltaTime;
				yield return null;
			}
		}

		inLeftHoldCoroutine = false;
	}
	IEnumerator EscapeHoldCoroutine() {
		inEscapeHoldCoroutine = true;

		float timeHeld = 0;
		while (EscapeHeld) {
			if (OnEscapeHeld != null) {
				OnEscapeHeld(timeHeld);

				timeHeld += Time.deltaTime;
				yield return null;
			}
		}

		inEscapeHoldCoroutine = false;
	}
	IEnumerator SpaceHoldCoroutine() {
		inSpaceHoldCoroutine = true;

		float timeHeld = 0;
		while (SpaceHeld) {
			if (OnSpaceHeld != null) {
				OnSpaceHeld(timeHeld);

				timeHeld += Time.deltaTime;
				yield return null;
			}
		}

		inSpaceHoldCoroutine = false;
	}
	IEnumerator ShiftHoldCoroutine() {
		inShiftHoldCoroutine = true;

		float timeHeld = 0;
		while (ShiftHeld) {
			if (OnShiftHeld != null) {
				OnShiftHeld(timeHeld);

				timeHeld += Time.deltaTime;
				yield return null;
			}
		}

		inShiftHoldCoroutine = false;
	}
}
#endregion
