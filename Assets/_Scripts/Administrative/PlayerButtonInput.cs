using System.Collections;
using System.Collections.Generic;
using Telemetry;
using UnityEngine;

public enum InputType {
	Up,
	Down,
	Right,
	Left,
	Interact,
	Zoom,
	AlignObject,
	Pause,
	Jump,
	Sprint,
	LeftStick,
	RightStick
}

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
	public delegate void ButtonPressEventWithInput(InputType inputType);
	public delegate void ButtonReleaseEventWithInput(InputType inputType);
	public delegate void ButtonHeldEventWithInput(InputType inputType, float durationHeld);

	public event ButtonPressEventWithInput OnAnyPress;
	public event ButtonReleaseEventWithInput OnAnyRelease;
	public event ButtonHeldEventWithInput OnAnyHeld;

	public event ButtonPressEvent OnUpPress;
	public event ButtonPressEvent OnDownPress;
	public event ButtonPressEvent OnRightPress;
	public event ButtonPressEvent OnLeftPress;
	public event ButtonPressEvent OnInteractPress;
	public event ButtonPressEvent OnZoomPress;
	public event ButtonPressEvent OnAlignObjectPress;
	public event ButtonPressEvent OnPausePress;
	public event ButtonPressEvent OnJumpPress;
	public event ButtonPressEvent OnSprintPress;

	public event ButtonReleaseEvent OnUpRelease;
	public event ButtonReleaseEvent OnDownRelease;
	public event ButtonReleaseEvent OnRightRelease;
	public event ButtonReleaseEvent OnLeftRelease;
	public event ButtonReleaseEvent OnInteractRelease;
	public event ButtonReleaseEvent OnZoomRelease;
	public event ButtonReleaseEvent OnAlignObjectRelease;
	public event ButtonReleaseEvent OnPauseRelease;
	public event ButtonReleaseEvent OnJumpRelease;
	public event ButtonReleaseEvent OnSprintRelease;

	public event ButtonHeldEvent OnUpHeld;
	public event ButtonHeldEvent OnDownHeld;
	public event ButtonHeldEvent OnRightHeld;
	public event ButtonHeldEvent OnLeftHeld;
	public event ButtonHeldEvent OnInteractHeld;
	public event ButtonHeldEvent OnZoomHeld;
	public event ButtonHeldEvent OnAlignObjectHeld;
	public event ButtonHeldEvent OnPauseHeld;
	public event ButtonHeldEvent OnJumpHeld;
	public event ButtonHeldEvent OnSprintHeld;
#endregion

#region Coroutine bools
	bool inLeftStickHeldCoroutine = false;
	bool inRightStickHeldCoroutine = false;

	bool inUpHoldCoroutine = false;
	bool inDownHoldCoroutine = false;
	bool inRightHoldCoroutine = false;
	bool inLeftHoldCoroutine = false;
	bool inInteractHoldCoroutine = false;
	bool inZoomHoldCoroutine = false;
	bool inAlignObjectHoldCoroutine = false;
	bool inPauseHoldCoroutine = false;
	bool inJumpHoldCoroutine = false;
	bool inSprintHoldCoroutine = false;
#endregion
	
	public void Update() {
		if (NovaPauseMenu.instance.PauseMenuIsOpen) return;
		
		// Left stick
		if (LeftStickHeld) {
			if (!inLeftStickHeldCoroutine) {
				StartCoroutine(LeftStickHeldCoroutine());
			}
		}
		else if (!LeftStickHeld)
		// Right stick
		if (RightStickHeld) {
			if (!inRightStickHeldCoroutine) {
				StartCoroutine(RightStickHeldCoroutine());
			}
		}

		// Up button
		if (UpPressed) {
			OnUpPress?.Invoke();
			OnAnyPress?.Invoke(InputType.Up);
			if (!inUpHoldCoroutine) {
				StartCoroutine(UpHoldCoroutine());
			}
		}
		else if (UpReleased) {
			OnUpRelease?.Invoke();
			OnAnyRelease?.Invoke(InputType.Up);
		}
		
		// Down button
		if (DownPressed) {
			OnDownPress?.Invoke();
			OnAnyPress?.Invoke(InputType.Down);
			if (!inDownHoldCoroutine) {
				StartCoroutine(DownHoldCoroutine());
			}
		}
		else if (DownReleased) {
			OnDownRelease?.Invoke();
			OnAnyRelease?.Invoke(InputType.Down);
		}
		
		// Right button
		if (RightPressed) {
			OnRightPress?.Invoke();
			OnAnyPress?.Invoke(InputType.Right);
			if (!inRightHoldCoroutine) {
				StartCoroutine(RightHoldCoroutine());
			}
		}
		else if (RightReleased) {
			OnRightRelease?.Invoke();
			OnAnyRelease?.Invoke(InputType.Right);
		}
		
		// Left button
		if (LeftPressed) {
			OnLeftPress?.Invoke();
			OnAnyPress?.Invoke(InputType.Left);
			if (!inLeftHoldCoroutine) {
				StartCoroutine(LeftHoldCoroutine());
			}
		}
		else if (LeftReleased) {
			OnLeftRelease?.Invoke();
			OnAnyRelease?.Invoke(InputType.Left);
		}

		// Interact button (Also maps to mouse left-click)
		if (InteractPressed) {
			OnInteractPress?.Invoke();
			OnAnyPress?.Invoke(InputType.Interact);
			if (!inInteractHoldCoroutine) {
				StartCoroutine(InteractHoldCoroutine());
			}
		}
		else if (InteractReleased) {
			OnInteractRelease?.Invoke();
			OnAnyRelease?.Invoke(InputType.Interact);
		}

		// Zoom button (Also maps to mouse right-click)
		if (ZoomPressed) {
			OnZoomPress?.Invoke();
			OnAnyPress?.Invoke(InputType.Zoom);
			if (!inZoomHoldCoroutine) {
				StartCoroutine(ZoomHoldCoroutine());
			}
		}
		else if (ZoomReleased) {
			OnZoomRelease?.Invoke();
			OnAnyRelease?.Invoke(InputType.Zoom);
		}

		// AlignObject button (also maps to mouse middle-click)
		if (AlignObjectPressed) {
			OnAlignObjectPress?.Invoke();
			OnAnyPress?.Invoke(InputType.AlignObject);
			if (!inAlignObjectHoldCoroutine) {
				StartCoroutine(AlignObjectHoldCoroutine());
			}
		}
		else if (AlignObjectReleased) {
			OnAlignObjectRelease?.Invoke();
			OnAnyRelease?.Invoke(InputType.AlignObject);
		}

		// Pause button
		if (PausePressed) {
			OnPausePress?.Invoke();
			OnAnyPress?.Invoke(InputType.Pause);
			if (!inPauseHoldCoroutine) {
				StartCoroutine(PauseHoldCoroutine());
			}
		}
		else if (PauseReleased) {
			OnPauseRelease?.Invoke();
			OnAnyRelease?.Invoke(InputType.Pause);
		}
		
		// Jump button
		if (JumpPressed) {
			OnJumpPress?.Invoke();
			OnAnyPress?.Invoke(InputType.Jump);
			if (!inJumpHoldCoroutine) {
				StartCoroutine(JumpHoldCoroutine());
			}
		}
		else if (JumpReleased) {
			OnJumpRelease?.Invoke();
			OnAnyRelease?.Invoke(InputType.Jump);
		}
		
		// Sprint button
		if (SprintPressed) {
			OnSprintPress?.Invoke();
			OnAnyPress?.Invoke(InputType.Sprint);
			if (!inSprintHoldCoroutine) {
				StartCoroutine(SprintHoldCoroutine());
			}
		}
		else if (SprintReleased) {
			OnSprintRelease?.Invoke();
			OnAnyRelease?.Invoke(InputType.Sprint);
		}

	}

#region Combined inputs
	// On first press
	public bool UpPressed => Settings.Keybinds.Forward.Pressed || InputTelemetry.KeyPressSimulated(InputType.Up);
	
	public bool DownPressed => Settings.Keybinds.Backward.Pressed || InputTelemetry.KeyPressSimulated(InputType.Down);
	
	public bool RightPressed => Settings.Keybinds.Right.Pressed || InputTelemetry.KeyPressSimulated(InputType.Right);
	
	public bool LeftPressed => Settings.Keybinds.Left.Pressed || InputTelemetry.KeyPressSimulated(InputType.Left);
	
	public bool InteractPressed => Settings.Keybinds.Interact.Pressed || InputTelemetry.KeyPressSimulated(InputType.Interact);

	public bool ZoomPressed => Settings.Keybinds.Zoom.Pressed || InputTelemetry.KeyPressSimulated(InputType.Zoom);

	public bool AlignObjectPressed => Settings.Keybinds.AlignObject.Pressed || InputTelemetry.KeyPressSimulated(InputType.AlignObject);
	
	public bool PausePressed => Settings.Keybinds.Pause.Pressed || InputTelemetry.KeyPressSimulated(InputType.Pause);
	
	public bool JumpPressed => Settings.Keybinds.Jump.Pressed || InputTelemetry.KeyPressSimulated(InputType.Jump);
	
	public bool SprintPressed => Settings.Keybinds.Sprint.Pressed || InputTelemetry.KeyPressSimulated(InputType.Sprint);
	

	// On button release
	public bool UpReleased => Settings.Keybinds.Forward.Released || InputTelemetry.KeyReleaseSimulated(InputType.Up);
	
	public bool DownReleased => Settings.Keybinds.Backward.Released || InputTelemetry.KeyReleaseSimulated(InputType.Down);
	
	public bool RightReleased => Settings.Keybinds.Right.Released || InputTelemetry.KeyReleaseSimulated(InputType.Right);
	
	public bool LeftReleased => Settings.Keybinds.Left.Released || InputTelemetry.KeyReleaseSimulated(InputType.Left);
	
	public bool InteractReleased => Settings.Keybinds.Interact.Released || InputTelemetry.KeyReleaseSimulated(InputType.Interact);

	public bool ZoomReleased => Settings.Keybinds.Zoom.Released || InputTelemetry.KeyReleaseSimulated(InputType.Zoom);

	public bool AlignObjectReleased => Settings.Keybinds.AlignObject.Released || InputTelemetry.KeyReleaseSimulated(InputType.AlignObject);

	public bool PauseReleased => Settings.Keybinds.Pause.Released || InputTelemetry.KeyReleaseSimulated(InputType.Pause);
	
	public bool JumpReleased => Settings.Keybinds.Jump.Released || InputTelemetry.KeyReleaseSimulated(InputType.Jump);
	
	public bool SprintReleased => Settings.Keybinds.Sprint.Released || InputTelemetry.KeyReleaseSimulated(InputType.Sprint);
	

	// While button held
	public bool UpHeld => Settings.Keybinds.Forward.Held || InputTelemetry.KeyHeldSimulated(InputType.Up);
	
	public bool DownHeld => Settings.Keybinds.Backward.Held || InputTelemetry.KeyHeldSimulated(InputType.Down);
	
	public bool RightHeld => Settings.Keybinds.Right.Held || InputTelemetry.KeyHeldSimulated(InputType.Right);
	
	public bool LeftHeld => Settings.Keybinds.Left.Held || InputTelemetry.KeyHeldSimulated(InputType.Left);
	
	public bool InteractHeld => Settings.Keybinds.Interact.Held || InputTelemetry.KeyHeldSimulated(InputType.Interact);
	
	public bool ZoomHeld => Settings.Keybinds.Zoom.Held || InputTelemetry.KeyHeldSimulated(InputType.Zoom);

	public bool AlignObjectHeld => Settings.Keybinds.AlignObject.Held || InputTelemetry.KeyHeldSimulated(InputType.AlignObject);
	
	public bool PauseHeld => Settings.Keybinds.Pause.Held || InputTelemetry.KeyHeldSimulated(InputType.Pause);
	
	public bool JumpHeld => Settings.Keybinds.Jump.Held || InputTelemetry.KeyHeldSimulated(InputType.Jump);
	
	public bool SprintHeld => Settings.Keybinds.Sprint.Held || InputTelemetry.KeyHeldSimulated(InputType.Sprint);

	// While stick held
	public bool LeftStickHeld => LeftStick.magnitude > 0;
	
	public bool RightStickHeld => RightStick.magnitude > 0;
	
	public bool AnyInputHeld => LeftStickHeld || RightStickHeld || UpHeld || DownHeld || RightHeld || LeftHeld || InteractHeld || ZoomHeld || AlignObjectHeld || PauseHeld || JumpHeld || SprintHeld;

	public Vector2 LeftStick {
		get {
			Vector2 leftStickSimulated = InputTelemetry.StickSimulated(InputType.LeftStick);
			if (leftStickSimulated.magnitude > 0) {
				return leftStickSimulated;
			}
			
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
			Vector2 rightStickSimulated = InputTelemetry.StickSimulated(InputType.RightStick);
			if (rightStickSimulated.magnitude > 0) {
				return rightStickSimulated;
			}
			
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

		OnAnyPress?.Invoke(InputType.LeftStick);
		float timeHeld = 0;
		while (LeftStickHeld) {
			Vector2 leftStick = LeftStick;
			OnLeftStickHeld?.Invoke(leftStick);
			OnLeftStickHeldWithDuration?.Invoke(leftStick, timeHeld);
			OnAnyHeld?.Invoke(InputType.LeftStick, timeHeld);

			timeHeld += Time.deltaTime;
			yield return null;
		}

		OnAnyRelease?.Invoke(InputType.LeftStick);
		inLeftStickHeldCoroutine = false;
	}
	IEnumerator RightStickHeldCoroutine() {
		inRightStickHeldCoroutine = true;

		OnAnyPress?.Invoke(InputType.RightStick);
		float timeHeld = 0;
		while (RightStickHeld) {
			Vector2 rightStick = RightStick;
			OnRightStickHeld?.Invoke(rightStick);
			OnRightStickHeldWithDuration?.Invoke(rightStick, timeHeld);
			OnAnyHeld?.Invoke(InputType.RightStick, timeHeld);
			
			timeHeld += Time.deltaTime;
			yield return null;
		}

		OnAnyRelease?.Invoke(InputType.RightStick);
		inRightStickHeldCoroutine = false;
	}

	IEnumerator UpHoldCoroutine() {
		inUpHoldCoroutine = true;
		
		float timeHeld = 0;
		while (UpHeld) {
			OnUpHeld?.Invoke(timeHeld);
			OnAnyHeld?.Invoke(InputType.Up, timeHeld);

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inUpHoldCoroutine = false;
	}
	IEnumerator DownHoldCoroutine() {
		inDownHoldCoroutine = true;

		float timeHeld = 0;
		while (DownHeld) {
			OnDownHeld?.Invoke(timeHeld);
			OnAnyHeld?.Invoke(InputType.Down, timeHeld);

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inDownHoldCoroutine = false;
	}
	IEnumerator RightHoldCoroutine() {
		inRightHoldCoroutine = true;

		float timeHeld = 0;
		while (RightHeld) {
			OnRightHeld?.Invoke(timeHeld);
			OnAnyHeld?.Invoke(InputType.Right, timeHeld);

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inRightHoldCoroutine = false;
	}
	IEnumerator LeftHoldCoroutine() {
		inLeftHoldCoroutine = true;

		float timeHeld = 0;
		while (LeftHeld) {
			OnLeftHeld?.Invoke(timeHeld);
			OnAnyHeld?.Invoke(InputType.Left, timeHeld);

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inLeftHoldCoroutine = false;
	}
	IEnumerator InteractHoldCoroutine() {
		inInteractHoldCoroutine = true;

		float timeHeld = 0;
		while (InteractHeld) {
			OnInteractHeld?.Invoke(timeHeld);
			OnAnyHeld?.Invoke(InputType.Interact, timeHeld);

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inInteractHoldCoroutine = false;
	}
	IEnumerator ZoomHoldCoroutine() {
		inZoomHoldCoroutine = true;

		float timeHeld = 0;
		while (ZoomHeld) {
			OnZoomHeld?.Invoke(timeHeld);
			OnAnyHeld?.Invoke(InputType.Zoom, timeHeld);

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inZoomHoldCoroutine = false;
	}
	IEnumerator AlignObjectHoldCoroutine() {
		inAlignObjectHoldCoroutine = true;

		float timeHeld = 0;
		while (AlignObjectHeld) {
			OnAlignObjectHeld?.Invoke(timeHeld);
			OnAnyHeld?.Invoke(InputType.AlignObject, timeHeld);

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inAlignObjectHoldCoroutine = false;
	}
	IEnumerator PauseHoldCoroutine() {
		inPauseHoldCoroutine = true;

		float timeHeld = 0;
		while (PauseHeld) {
			OnPauseHeld?.Invoke(timeHeld);
			OnAnyHeld?.Invoke(InputType.Pause, timeHeld);

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inPauseHoldCoroutine = false;
	}
	IEnumerator JumpHoldCoroutine() {
		inJumpHoldCoroutine = true;

		float timeHeld = 0;
		while (JumpHeld) {
			OnJumpHeld?.Invoke(timeHeld);
			OnAnyHeld?.Invoke(InputType.Jump, timeHeld);

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inJumpHoldCoroutine = false;
	}
	IEnumerator SprintHoldCoroutine() {
		inSprintHoldCoroutine = true;

		float timeHeld = 0;
		while (SprintHeld) {
			OnSprintHeld?.Invoke(timeHeld);
			OnAnyHeld?.Invoke(InputType.Sprint, timeHeld);

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inSprintHoldCoroutine = false;
	}
}
#endregion
