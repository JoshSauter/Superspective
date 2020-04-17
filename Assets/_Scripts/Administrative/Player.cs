using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Singleton<Player> {
	public PlayerLook look;
	public PlayerMovement movement;
	public Headbob headbob;
	public CameraFollow cameraFollow;

	public Collider collider;
	public Renderer renderer;
	public Vector3 playerSize { get { return renderer.bounds.size; } }

	private void Start() {
		renderer = GetComponentInChildren<Renderer>();
		collider = GetComponentInChildren<Collider>();
		look = GetComponent<PlayerLook>();
		movement = GetComponent<PlayerMovement>();
		headbob = GetComponent<Headbob>();
		cameraFollow = GetComponentInChildren<CameraFollow>();
	}
}
