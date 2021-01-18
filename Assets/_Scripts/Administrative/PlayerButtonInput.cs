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
	public delegate void StickHeldEvent(Vector2 dir);
	public delegate void StickHeldDurationEvent(Vector2 dir, float durationHeld);

	public event StickHeldEvent OnLeftStickHeld;
	public event StickHeldEvent OnRightStickHeld;
	public event StickHeldDurationEvent OnLeftStickHeldWithDuration;
	public event StickHeldDurationEvent OnRightStickHeldWithDuration;

	public delegate void ButtonPressEvent();
	public delegate void ButtonReleaseEvent();
	public delegate void ButtonHeldEvent(float durationHeld);

	public event ButtonPressEvent OnUpPress;
	public event ButtonPressEvent OnDownPress;
	public event ButtonPressEvent OnRightPress;
	public event ButtonPressEvent OnLeftPress;
	public event ButtonPressEvent OnAction1Press;
	public event ButtonPressEvent OnAction2Press;
	public event ButtonPressEvent OnAction3Press;
	public event ButtonPressEvent OnEscapePress;
	public event ButtonPressEvent OnSpacePress;
	public event ButtonPressEvent OnShiftPress;

	public event ButtonReleaseEvent OnUpRelease;
	public event ButtonReleaseEvent OnDownRelease;
	public event ButtonReleaseEvent OnRightRelease;
	public event ButtonReleaseEvent OnLeftRelease;
	public event ButtonReleaseEvent OnAction1Release;
	public event ButtonReleaseEvent OnAction2Release;
	public event ButtonReleaseEvent OnAction3Release;
	public event ButtonReleaseEvent OnEscapeRelease;
	public event ButtonReleaseEvent OnSpaceRelease;
	public event ButtonReleaseEvent OnShiftRelease;

	public event ButtonHeldEvent OnUpHeld;
	public event ButtonHeldEvent OnDownHeld;
	public event ButtonHeldEvent OnRightHeld;
	public event ButtonHeldEvent OnLeftHeld;
	public event ButtonHeldEvent OnAction1Held;
	public event ButtonHeldEvent OnAction2Held;
	public event ButtonHeldEvent OnAction3Held;
	public event ButtonHeldEvent OnEscapeHeld;
	public event ButtonHeldEvent OnSpaceHeld;
	public event ButtonHeldEvent OnShiftHeld;
#endregion

#region Coroutine bools
	bool inLeftStickHeldCoroutine = false;
	bool inRightStickHeldCoroutine = false;

	bool inUpHoldCoroutine = false;
	bool inDownHoldCoroutine = false;
	bool inRightHoldCoroutine = false;
	bool inLeftHoldCoroutine = false;
	bool inAction1HoldCoroutine = false;
	bool inAction2HoldCoroutine = false;
	bool inAction3HoldCoroutine = false;
	bool inEscapeHoldCoroutine = false;
	bool inSpaceHoldCoroutine = false;
	bool inShiftHoldCoroutine = false;
#endregion
	
	public void Update() {
		// Left stick
		if (LeftStickHeld) {
			if (!inLeftStickHeldCoroutine) {
				StartCoroutine(LeftStickHeldCoroutine());
			}
		}
		// Right stick
		if (RightStickHeld) {
			if (!inRightStickHeldCoroutine) {
				StartCoroutine(RightStickHeldCoroutine());
			}
		}

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

		// Action1 button (Also maps to mouse left-click)
		if (Action1Pressed && OnAction1Press != null) {
			OnAction1Press();
			if (!inAction1HoldCoroutine) {
				StartCoroutine(Action1HoldCoroutine());
			}
		}
		else if (Action1Released && OnAction1Release != null) {
			OnAction1Release();
		}

		// Action2 button (Also maps to mouse right-click)
		if (Action2Pressed && OnAction2Press != null) {
			OnAction2Press();
			if (!inAction2HoldCoroutine) {
				StartCoroutine(Action2HoldCoroutine());
			}
		}
		else if (Action2Released && OnAction2Release != null) {
			OnAction2Release();
		}

		// Action3 button (also maps to mouse middle-click)
		if (Action3Pressed && OnAction3Press != null) {
			OnAction3Press();
			if (!inAction3HoldCoroutine) {
				StartCoroutine(Action3HoldCoroutine());
			}
		}
		else if (Action3Released && OnAction3Release != null) {
			OnAction3Release();
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
	public bool UpPressed => KeyboardAndMouseInputs.Up.Pressed;
	
	public bool DownPressed => KeyboardAndMouseInputs.Down.Pressed;
	
	public bool RightPressed => KeyboardAndMouseInputs.Right.Pressed;
	
	public bool LeftPressed => KeyboardAndMouseInputs.Left.Pressed;
	
	public bool Action1Pressed => KeyboardAndMouseInputs.Action1.Pressed;

	public bool Action2Pressed => KeyboardAndMouseInputs.Action2.Pressed;

	public bool Action3Pressed => KeyboardAndMouseInputs.Action3.Pressed;
	
	public bool EscapePressed => KeyboardAndMouseInputs.Escape.Pressed;
	
	public bool SpacePressed => KeyboardAndMouseInputs.Space.Pressed;
	
	public bool ShiftPressed => KeyboardAndMouseInputs.Shift.Pressed;
	

	// On button release
	public bool UpReleased => KeyboardAndMouseInputs.Up.Released;
	
	public bool DownReleased => KeyboardAndMouseInputs.Down.Released;
	
	public bool RightReleased => KeyboardAndMouseInputs.Right.Released;
	
	public bool LeftReleased => KeyboardAndMouseInputs.Left.Released;
	
	public bool Action1Released => KeyboardAndMouseInputs.Action1.Released;

	public bool Action2Released => KeyboardAndMouseInputs.Action2.Released;

	public bool Action3Released => KeyboardAndMouseInputs.Action3.Released;

	public bool EscapeReleased => KeyboardAndMouseInputs.Escape.Released;
	
	public bool SpaceReleased => KeyboardAndMouseInputs.Space.Released;
	
	public bool ShiftReleased => KeyboardAndMouseInputs.Shift.Released;
	

	// While button held
	public bool UpHeld => KeyboardAndMouseInputs.Up.Held;
	
	public bool DownHeld => KeyboardAndMouseInputs.Down.Held;
	
	public bool RightHeld => KeyboardAndMouseInputs.Right.Held;
	
	public bool LeftHeld => KeyboardAndMouseInputs.Left.Held;
	
	public bool Action1Held => KeyboardAndMouseInputs.Action1.Held;
	
	public bool Action2Held => KeyboardAndMouseInputs.Action2.Held;

	public bool Action3Held => KeyboardAndMouseInputs.Action3.Held;
	
	public bool EscapeHeld => KeyboardAndMouseInputs.Escape.Held;
	
	public bool SpaceHeld => KeyboardAndMouseInputs.Space.Held;
	
	public bool ShiftHeld => KeyboardAndMouseInputs.Shift.Held;
	

	// While stick held
	public bool LeftStickHeld => LeftStick.magnitude > 0;
	
	public bool RightStickHeld => RightStick.magnitude > 0;
	

	public Vector2 LeftStick {
		get {
			// TODO: Add controller input here
			Vector2 keyboardInput = Vector2.zero;
			if (UpHeld) keyboardInput += Vector2.up;
			if (DownHeld) keyboardInput += Vector2.down;
			if (RightHeld) keyboardInput += Vector2.right;
			if (LeftHeld) keyboardInput += Vector2.left;
			// TODO: Add deadzone
			keyboardInput = Vector2.ClampMagnitude(keyboardInput, 1);

			return keyboardInput;
		}
	}
	public Vector2 RightStick {
		get {
			// TODO: Add controller input here
			Vector2 mouseInput = Vector2.zero;
			mouseInput += Input.GetAxis("Mouse Y") * Vector2.up;
			mouseInput += Input.GetAxis("Mouse X") * Vector2.right;

			// TODO: Add deadzone
			// Mouse input is NOT normalized, it can and will go above 1 magnitude
			// Multiply mouseInput by some value in 0-1 range to bring it in line with controller
			return mouseInput * 0.35f;
		}
	}
#endregion

#region ButtonHeld Coroutines
	IEnumerator LeftStickHeldCoroutine() {
		inLeftStickHeldCoroutine = true;

		float timeHeld = 0;
		while (LeftStickHeld) {
			Vector2 leftStick = LeftStick;
			if (OnLeftStickHeld != null) {
				OnLeftStickHeld(leftStick);
			}
			if (OnLeftStickHeldWithDuration != null) {
				OnLeftStickHeldWithDuration(leftStick, timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inLeftStickHeldCoroutine = false;
	}
	IEnumerator RightStickHeldCoroutine() {
		inRightStickHeldCoroutine = true;

		float timeHeld = 0;
		while (RightStickHeld) {
			Vector2 rightStick = RightStick;
			if (OnRightStickHeld != null) {
				OnRightStickHeld(RightStick);
			}
			if (OnRightStickHeldWithDuration != null) {
				OnRightStickHeldWithDuration(RightStick, timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inRightStickHeldCoroutine = false;
	}

	IEnumerator UpHoldCoroutine() {
		inUpHoldCoroutine = true;
		
		float timeHeld = 0;
		while (UpHeld) {
			if (OnUpHeld != null) {
				OnUpHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inUpHoldCoroutine = false;
	}
	IEnumerator DownHoldCoroutine() {
		inDownHoldCoroutine = true;

		float timeHeld = 0;
		while (DownHeld) {
			if (OnDownHeld != null) {
				OnDownHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inDownHoldCoroutine = false;
	}
	IEnumerator RightHoldCoroutine() {
		inRightHoldCoroutine = true;

		float timeHeld = 0;
		while (RightHeld) {
			if (OnRightHeld != null) {
				OnRightHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inRightHoldCoroutine = false;
	}
	IEnumerator LeftHoldCoroutine() {
		inLeftHoldCoroutine = true;

		float timeHeld = 0;
		while (LeftHeld) {
			if (OnLeftHeld != null) {
				OnLeftHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inLeftHoldCoroutine = false;
	}
	IEnumerator Action1HoldCoroutine() {
		inAction1HoldCoroutine = true;

		float timeHeld = 0;
		while (Action1Held) {
			if (OnAction1Held != null) {
				OnAction1Held(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inAction1HoldCoroutine = false;
	}
	IEnumerator Action2HoldCoroutine() {
		inAction2HoldCoroutine = true;

		float timeHeld = 0;
		while (Action2Held) {
			if (OnAction2Held != null) {
				OnAction2Held(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inAction2HoldCoroutine = false;
	}
	IEnumerator Action3HoldCoroutine() {
		inAction3HoldCoroutine = true;

		float timeHeld = 0;
		while (Action3Held) {
			if (OnAction3Held != null) {
				OnAction3Held(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inAction3HoldCoroutine = false;
	}
	IEnumerator EscapeHoldCoroutine() {
		inEscapeHoldCoroutine = true;

		float timeHeld = 0;
		while (EscapeHeld) {
			if (OnEscapeHeld != null) {
				OnEscapeHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inEscapeHoldCoroutine = false;
	}
	IEnumerator SpaceHoldCoroutine() {
		inSpaceHoldCoroutine = true;

		float timeHeld = 0;
		while (SpaceHeld) {
			if (OnSpaceHeld != null) {
				OnSpaceHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inSpaceHoldCoroutine = false;
	}
	IEnumerator ShiftHoldCoroutine() {
		inShiftHoldCoroutine = true;

		float timeHeld = 0;
		while (ShiftHeld) {
			if (OnShiftHeld != null) {
				OnShiftHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inShiftHoldCoroutine = false;
	}
}
#endregion
