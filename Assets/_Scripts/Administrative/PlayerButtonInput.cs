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

		// Interact button (Also maps to mouse left-click)
		if (InteractPressed && OnInteractPress != null) {
			OnInteractPress();
			if (!inInteractHoldCoroutine) {
				StartCoroutine(InteractHoldCoroutine());
			}
		}
		else if (InteractReleased && OnInteractRelease != null) {
			OnInteractRelease();
		}

		// Zoom button (Also maps to mouse right-click)
		if (ZoomPressed && OnZoomPress != null) {
			OnZoomPress();
			if (!inZoomHoldCoroutine) {
				StartCoroutine(ZoomHoldCoroutine());
			}
		}
		else if (ZoomReleased && OnZoomRelease != null) {
			OnZoomRelease();
		}

		// AlignObject button (also maps to mouse middle-click)
		if (AlignObjectPressed && OnAlignObjectPress != null) {
			OnAlignObjectPress();
			if (!inAlignObjectHoldCoroutine) {
				StartCoroutine(AlignObjectHoldCoroutine());
			}
		}
		else if (AlignObjectReleased && OnAlignObjectRelease != null) {
			OnAlignObjectRelease();
		}

		// Pause button
		if (PausePressed && OnPausePress != null) {
			OnPausePress();
			if (!inPauseHoldCoroutine) {
				StartCoroutine(PauseHoldCoroutine());
			}
		}
		else if (PauseReleased && OnPauseRelease != null) {
			OnPauseRelease();
		}
		// Jump button
		if (JumpPressed && OnJumpPress != null) {
			OnJumpPress();
			if (!inJumpHoldCoroutine) {
				StartCoroutine(JumpHoldCoroutine());
			}
		}
		else if (JumpReleased && OnJumpRelease != null) {
			OnJumpRelease();
		}
		// Sprint button
		if (SprintPressed && OnSprintPress != null) {
			OnSprintPress();
			if (!inSprintHoldCoroutine) {
				StartCoroutine(SprintHoldCoroutine());
			}
		}
		else if (SprintReleased && OnSprintRelease != null) {
			OnSprintRelease();
		}

	}

#region Combined inputs
	// On first press
	public bool UpPressed => Settings.Keybinds.Forward.Pressed;
	
	public bool DownPressed => Settings.Keybinds.Backward.Pressed;
	
	public bool RightPressed => Settings.Keybinds.Right.Pressed;
	
	public bool LeftPressed => Settings.Keybinds.Left.Pressed;
	
	public bool InteractPressed => Settings.Keybinds.Interact.Pressed;

	public bool ZoomPressed => Settings.Keybinds.Zoom.Pressed;

	public bool AlignObjectPressed => Settings.Keybinds.AlignObject.Pressed;
	
	public bool PausePressed => Settings.Keybinds.Pause.Pressed;
	
	public bool JumpPressed => Settings.Keybinds.Jump.Pressed;
	
	public bool SprintPressed => Settings.Keybinds.Sprint.Pressed;
	

	// On button release
	public bool UpReleased => Settings.Keybinds.Forward.Released;
	
	public bool DownReleased => Settings.Keybinds.Backward.Released;
	
	public bool RightReleased => Settings.Keybinds.Right.Released;
	
	public bool LeftReleased => Settings.Keybinds.Left.Released;
	
	public bool InteractReleased => Settings.Keybinds.Interact.Released;

	public bool ZoomReleased => Settings.Keybinds.Zoom.Released;

	public bool AlignObjectReleased => Settings.Keybinds.AlignObject.Released;

	public bool PauseReleased => Settings.Keybinds.Pause.Released;
	
	public bool JumpReleased => Settings.Keybinds.Jump.Released;
	
	public bool SprintReleased => Settings.Keybinds.Sprint.Released;
	

	// While button held
	public bool UpHeld => Settings.Keybinds.Forward.Held;
	
	public bool DownHeld => Settings.Keybinds.Backward.Held;
	
	public bool RightHeld => Settings.Keybinds.Right.Held;
	
	public bool LeftHeld => Settings.Keybinds.Left.Held;
	
	public bool InteractHeld => Settings.Keybinds.Interact.Held;
	
	public bool ZoomHeld => Settings.Keybinds.Zoom.Held;

	public bool AlignObjectHeld => Settings.Keybinds.AlignObject.Held;
	
	public bool PauseHeld => Settings.Keybinds.Pause.Held;
	
	public bool JumpHeld => Settings.Keybinds.Jump.Held;
	
	public bool SprintHeld => Settings.Keybinds.Sprint.Held;

	// While stick held
	public bool LeftStickHeld => LeftStick.magnitude > 0;
	
	public bool RightStickHeld => RightStick.magnitude > 0;
	
	public bool AnyInputHeld => LeftStickHeld || RightStickHeld || UpHeld || DownHeld || RightHeld || LeftHeld || InteractHeld || ZoomHeld || AlignObjectHeld || PauseHeld || JumpHeld || SprintHeld;

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
	IEnumerator InteractHoldCoroutine() {
		inInteractHoldCoroutine = true;

		float timeHeld = 0;
		while (InteractHeld) {
			if (OnInteractHeld != null) {
				OnInteractHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inInteractHoldCoroutine = false;
	}
	IEnumerator ZoomHoldCoroutine() {
		inZoomHoldCoroutine = true;

		float timeHeld = 0;
		while (ZoomHeld) {
			if (OnZoomHeld != null) {
				OnZoomHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inZoomHoldCoroutine = false;
	}
	IEnumerator AlignObjectHoldCoroutine() {
		inAlignObjectHoldCoroutine = true;

		float timeHeld = 0;
		while (AlignObjectHeld) {
			if (OnAlignObjectHeld != null) {
				OnAlignObjectHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inAlignObjectHoldCoroutine = false;
	}
	IEnumerator PauseHoldCoroutine() {
		inPauseHoldCoroutine = true;

		float timeHeld = 0;
		while (PauseHeld) {
			if (OnPauseHeld != null) {
				OnPauseHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inPauseHoldCoroutine = false;
	}
	IEnumerator JumpHoldCoroutine() {
		inJumpHoldCoroutine = true;

		float timeHeld = 0;
		while (JumpHeld) {
			if (OnJumpHeld != null) {
				OnJumpHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inJumpHoldCoroutine = false;
	}
	IEnumerator SprintHoldCoroutine() {
		inSprintHoldCoroutine = true;

		float timeHeld = 0;
		while (SprintHeld) {
			if (OnSprintHeld != null) {
				OnSprintHeld(timeHeld);
			}

			timeHeld += Time.deltaTime;
			yield return null;
		}

		inSprintHoldCoroutine = false;
	}
}
#endregion
