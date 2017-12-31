using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RecolorOnTeleport : MonoBehaviour {
	public int startIndex;
	protected int index = 0;
	protected int NUM_MATERIALS = 4;

	protected void Awake() {
		index = startIndex;
	}

	void Start() {
		TeleportEnter.OnAnyTeleport += OnTeleportRecolor;
	}

	void OnDisable() {
		TeleportEnter.OnAnyTeleport -= OnTeleportRecolor;
	}

	public void IncrementIndex() {
		index++; index %= NUM_MATERIALS;
	}

	public void DecrementIndex() {
		if (index == 0)
			index = NUM_MATERIALS - 1;
		else
			index--;
	}

	public abstract void SetColor();

	private void OnTeleportRecolor(Teleport teleporter, Collider player) {
		float rotationBetweenEnterExit = TeleportEnter.GetRotationAngleBetweenTeleporters(teleporter);

		// Handle forward rotations
		for (int i = Mathf.RoundToInt(rotationBetweenEnterExit); i > 0; i -= 90) {
			IncrementIndex();
		}
		// Handle backward rotations
		for (int i = Mathf.RoundToInt(rotationBetweenEnterExit); i < 0; i += 90) {
			DecrementIndex();
		}
		SetColor();
	}
}
