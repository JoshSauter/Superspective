using System;
using System.Collections;
using System.Collections.Generic;
using Library.Functional;
using SuperspectiveUtils;
using UnityEngine;

public class KeyboardAndMouseInput {
	public bool Pressed => _PrimaryPressed.Invoke() || _SecondaryPressed.Invoke();
	public bool Released => _PrimaryReleased.Invoke() || _SecondaryReleased.Invoke();
	public bool Held => _PrimaryHeld.Invoke() || _SecondaryHeld.Invoke();
	
	public Either<int, KeyCode> primary;
	public Either<int, KeyCode> secondary;
	public string displayPrimary;
	public string displaySecondary;
	
	private Func<bool> _PrimaryPressed;
	private Func<bool> _PrimaryReleased;
	private Func<bool> _PrimaryHeld;
	private Func<bool> _SecondaryPressed;
	private Func<bool> _SecondaryReleased;
	private Func<bool> _SecondaryHeld;

	public KeyboardAndMouseInput() {}

	public KeyboardAndMouseInput(KeyboardAndMouseInput copyFrom) {
		primary = copyFrom.primary?.Match(mb => new Either<int, KeyCode>(mb), key => new Either<int, KeyCode>(key));
		secondary = copyFrom.secondary?.Match(mb => new Either<int, KeyCode>(mb), key => new Either<int, KeyCode>(key));

		SetMapping(primary, secondary);
	}

	public KeyboardAndMouseInput(KeyCode key) {
		SetMapping(new Either<int, KeyCode>(key), null);
	}

	public KeyboardAndMouseInput(KeyCode primaryKey, KeyCode secondaryKey) {
		SetMapping(new Either<int, KeyCode>(primaryKey), new Either<int, KeyCode>(secondaryKey));
	}

	public KeyboardAndMouseInput(int mouseButton) {
		SetMapping(new Either<int, KeyCode>(mouseButton), null);
	}
	public KeyboardAndMouseInput(int primaryMouseButton, int secondaryMouseButton) {
		SetMapping(new Either<int, KeyCode>(primaryMouseButton), new Either<int, KeyCode>(secondaryMouseButton));
	}

	public KeyboardAndMouseInput(KeyCode primaryKey, int secondaryMouseButton) {
		SetMapping(new Either<int, KeyCode>(primaryKey), new Either<int, KeyCode>(secondaryMouseButton));
	}

	public KeyboardAndMouseInput(int primaryMouseButton, KeyCode secondaryKey) {
		SetMapping(new Either<int, KeyCode>(primaryMouseButton), secondaryKey);
	}

	public KeyboardAndMouseInput SetMapping(Either<int, KeyCode> primaryInput, Either<int, KeyCode> secondaryInput) {
		return SetPrimaryMapping(primaryInput).SetSecondaryMapping(secondaryInput);
	}

	public KeyboardAndMouseInput SetPrimaryMapping(Either<int, KeyCode> input) {
		_PrimaryPressed = () => (input?.Match(Input.GetMouseButtonDown, Input.GetKeyDown) ?? false);
		_PrimaryReleased = () => (input?.Match(Input.GetMouseButtonUp, Input.GetKeyUp) ?? false);
		_PrimaryHeld = () => (input?.Match(Input.GetMouseButton, Input.GetKey) ?? false);
		primary = input;
		displayPrimary = input?.Match(GetMouseButtonDisplayName, key => key.ToString().SplitCamelCase()) ?? "";
		return this;
	}
	
	public KeyboardAndMouseInput SetSecondaryMapping(Either<int, KeyCode> input) {
		_SecondaryPressed = () => (input?.Match(Input.GetMouseButtonDown, Input.GetKeyDown) ?? false);
		_SecondaryReleased = () => (input?.Match(Input.GetMouseButtonUp, Input.GetKeyUp) ?? false);
		_SecondaryHeld = () => (input?.Match(Input.GetMouseButton, Input.GetKey) ?? false);
		secondary = input;
		displaySecondary = input?.Match(GetMouseButtonDisplayName, key => key.ToString().SplitCamelCase()) ?? "";
		return this;
	}

	private string GetMouseButtonDisplayName(int mouseButton) {
		switch (mouseButton) {
			case 0:
				return "Left Mouse";
			case 1:
				return "Right Mouse";
			case 2:
				return "Middle Mouse";
			default:
				return $"MB{mouseButton + 1}";
		}
	}
}

public static class KeyboardAndMouseInputs {
	static KeyboardAndMouseInput AZERTYUp = new KeyboardAndMouseInput(KeyCode.Z);
	static KeyboardAndMouseInput AZERTY = new KeyboardAndMouseInput(KeyCode.S);
	static KeyboardAndMouseInput AZERTYLeft = new KeyboardAndMouseInput(KeyCode.Q);
	static KeyboardAndMouseInput AZERTYRight = new KeyboardAndMouseInput(KeyCode.D);
}