using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardAndMouseInput {
	public bool Pressed { get { return _Pressed(); } }
	public bool Released { get { return _Released(); } }
	public bool Held { get { return _Held(); } }
	private Func<bool> _Pressed;
	private Func<bool> _Released;
	private Func<bool> _Held;
	public string primary;
	public string secondary;

	public KeyboardAndMouseInput(KeyCode key) {
		SetMapping(key);
	}

	public KeyboardAndMouseInput(KeyCode primaryKey, KeyCode secondaryKey) {
		SetMapping(primaryKey, secondaryKey);
	}

	public KeyboardAndMouseInput(int mouseButton) {
		SetMapping(mouseButton);
	}
	public KeyboardAndMouseInput(int primaryMouseButton, int secondaryMouseButton) {
		SetMapping(primaryMouseButton, secondaryMouseButton);
	}

	public KeyboardAndMouseInput(KeyCode primaryKey, int secondaryMouseButton) {
		SetMapping(primaryKey, secondaryMouseButton);
	}

	public KeyboardAndMouseInput(int primaryMouseButton, KeyCode secondaryKey) {
		SetMapping(primaryMouseButton, secondaryKey);
	}

	public KeyboardAndMouseInput SetMapping(KeyCode key) {
		_Pressed = () => Input.GetKeyDown(key);
		_Released = () => Input.GetKeyUp(key);
		_Held = () => Input.GetKey(key);
		primary = key.ToString();
		secondary = "";
		return this;
	}

	public KeyboardAndMouseInput SetMapping(KeyCode primaryKey, KeyCode secondaryKey) {
		_Pressed = () => (Input.GetKeyDown(primaryKey) || Input.GetKeyDown(secondaryKey));
		_Released = () => (Input.GetKeyUp(primaryKey) || Input.GetKeyUp(secondaryKey));
		_Held = () => (Input.GetKey(primaryKey) || Input.GetKey(secondaryKey));
		primary = primaryKey.ToString();
		secondary = secondaryKey.ToString();
		return this;
	}

	public KeyboardAndMouseInput SetMapping(int mouseButton) {
		_Pressed = () => Input.GetMouseButtonDown(mouseButton);
		_Released = () => Input.GetMouseButtonUp(mouseButton);
		_Held = () => Input.GetMouseButton(mouseButton);
		primary = "MB" + (mouseButton + 1);
		secondary = "";
		return this;
	}
	public KeyboardAndMouseInput SetMapping(int primaryMouseButton, int secondaryMouseButton) {
		_Pressed = () => (Input.GetMouseButtonDown(primaryMouseButton) || Input.GetMouseButtonDown(secondaryMouseButton));
		_Released = () => (Input.GetMouseButtonUp(primaryMouseButton) || Input.GetMouseButtonUp(secondaryMouseButton));
		_Held = () => (Input.GetMouseButton(primaryMouseButton) || Input.GetMouseButton(secondaryMouseButton));
		primary = "MB" + (primaryMouseButton + 1);
		secondary = "MB" + (secondaryMouseButton + 1);
		return this;
	}

	public KeyboardAndMouseInput SetMapping(KeyCode primaryKey, int secondaryMouseButton) {
		_Pressed = () => (Input.GetKeyDown(primaryKey) || Input.GetMouseButtonDown(secondaryMouseButton));
		_Released = () => (Input.GetKeyUp(primaryKey) || Input.GetMouseButtonUp(secondaryMouseButton));
		_Held = () => (Input.GetKey(primaryKey) || Input.GetMouseButton(secondaryMouseButton));
		primary = primaryKey.ToString();
		secondary = "MB" + (secondaryMouseButton + 1);
		return this;
	}

	public KeyboardAndMouseInput SetMapping(int primaryMouseButton, KeyCode secondaryKey) {
		_Pressed = () => (Input.GetMouseButtonDown(primaryMouseButton) || Input.GetKeyDown(secondaryKey));
		_Released = () => (Input.GetMouseButtonUp(primaryMouseButton) || Input.GetKeyUp(secondaryKey));
		_Held = () => (Input.GetMouseButton(primaryMouseButton) || Input.GetKey(secondaryKey));
		primary = "MB" + (primaryMouseButton + 1);
		secondary = secondaryKey.ToString();
		return this;
	}
}

public static class KeyboardAndMouseInputs {
	public static KeyboardAndMouseInput Up = new KeyboardAndMouseInput(KeyCode.W);
	public static KeyboardAndMouseInput Down = new KeyboardAndMouseInput(KeyCode.S);
	public static KeyboardAndMouseInput Left = new KeyboardAndMouseInput(KeyCode.A);
	public static KeyboardAndMouseInput Right = new KeyboardAndMouseInput(KeyCode.D);
	public static KeyboardAndMouseInput Action1 = new KeyboardAndMouseInput(0);
	public static KeyboardAndMouseInput Escape = new KeyboardAndMouseInput(KeyCode.Escape);
	public static KeyboardAndMouseInput Space = new KeyboardAndMouseInput(KeyCode.Space);
	public static KeyboardAndMouseInput Shift = new KeyboardAndMouseInput(KeyCode.LeftShift, KeyCode.RightShift);
}
