﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using System.Net.Mime;
using System.Linq;
using System;
using NaughtyAttributes;
using Audio;

namespace PowerTrailMechanics {
	public class NodeTrailInfo {
		public Node parent;
		public Node thisNode;
		public float startDistance;
		public float endDistance;
	}

	public class PowerTrail : MonoBehaviour {
		public bool DEBUG = false;
		public DebugLogger debug;

		public enum PowerTrailState {
			depowered,
			partiallyPowered,
			powered
		}

		public Renderer[] renderers;
		Collider[] colliders;
		Material[] materials;
		public NodeSystem powerNodes;
		public List<NodeTrailInfo> trailInfo = new List<NodeTrailInfo>();

		// Data to be sent to the GPU. Positions are not repeated. Index order matches node ID
		const int MAX_NODES = 512;
		Vector4[] nodePositions;        // Positions of each node. w value is unused and ignored
		int[] endNodeIndex;
		int[] startNodeIndex;
		float[] interpolationValues;    // [0-1] interpolation value between startPosition and endPosition for each trail. Only GPU data that changes at runtime
		public bool reverseVisibility = false;
		const string nodePositionsKey = "_NodePositions";
		const string startPositionIDsKey = "_StartPositionIDs";
		const string endPositionIDsKey = "_EndPositionIDs";
		const string interpolationValuesKey = "_InterpolationValues";
		const string sdfCapsuleRadiusKey = "_CapsuleRadius";
		const string reverseVisibilityKey = "_ReverseVisibility";

		public bool useDurationInsteadOfSpeed = false;
		public bool useSeparateSpeedsForPowerOnOff = false;
		// Just used for NaughtyAttributes
		private bool useSameSpeedsForPowerOnOff => !useSeparateSpeedsForPowerOnOff;
		[ShowIf("useDurationInsteadOfSpeed")]
		public float targetDuration = 3f;
		[ShowIf(EConditionOperator.And, "useDurationInsteadOfSpeed", "useSeparateSpeedsForPowerOnOff")]
		public float targetDurationPowerOff = 3f;
		[HideIf("useDurationInsteadOfSpeed")]
		public float speed = 15f;
		[HideIf(EConditionOperator.Or, "useDurationInsteadOfSpeed", "useSameSpeedsForPowerOnOff")]
		public float speedPowerOff = 15f;
		public float powerTrailRadius = 0.15f;

		public SoundEffectAtLocation sound;

		#region events
		public delegate void PowerTrailAction();
		public event PowerTrailAction OnPowerBegin;
		public event PowerTrailAction OnPowerFinish;
		public event PowerTrailAction OnDepowerBegin;
		public event PowerTrailAction OnDepowerFinish;
		#endregion

		///////////
		// State //
		///////////
		public float duration { get { return maxDistance / speed; } }
		public float durationOff { get { return useSeparateSpeedsForPowerOnOff ? maxDistance / speedPowerOff : duration; } }
		public float distance = 0f;
		public float maxDistance = 0f;
		public bool powerIsOn = false;
		[SerializeField]
		private PowerTrailState _state = PowerTrailState.depowered;
		public PowerTrailState state {
			get { return _state; }
			set {
				if (_state == PowerTrailState.depowered && value == PowerTrailState.partiallyPowered) {
					OnPowerBegin?.Invoke();
				}
				else if (_state == PowerTrailState.partiallyPowered && value == PowerTrailState.powered) {
					OnPowerFinish?.Invoke();
				}
				else if (_state == PowerTrailState.powered && value == PowerTrailState.partiallyPowered) {
					OnDepowerBegin?.Invoke();
				}
				else if (_state == PowerTrailState.partiallyPowered && value == PowerTrailState.depowered) {
					OnDepowerFinish?.Invoke();
				}
				_state = value;
			}
		}
		bool isInitialized = false;

		private void Awake() {
			if (powerNodes == null) {
				powerNodes = GetComponent<NodeSystem>();
			}
			StartCoroutine(InitSound());
			gameObject.layer = LayerMask.NameToLayer("VisibleButNoPlayerCollision");
		}

		void Start() {
			if (renderers == null || renderers.Length == 0) {
				renderers = GetComponents<Renderer>();
			}
			if (colliders == null || colliders.Length == 0) {
				colliders = renderers.Select(r => r.GetComponent<Collider>()).Where(c => c != null).ToArray();
			}
			materials = renderers.Select(r => r.material).ToArray();
			debug = new DebugLogger(this, () => DEBUG);
			PopulateTrailInfo();
			if (useDurationInsteadOfSpeed) {
				speed = maxDistance / targetDuration;
				if (useSeparateSpeedsForPowerOnOff) {
					speedPowerOff = maxDistance / targetDurationPowerOff;
				}
			}

			PopulateStaticGPUInfo();
			StartCoroutine(UpdateAudio());
			SetStartState();
		}

		void SetStartState() {
			isInitialized = false;
			if (state == PowerTrailState.powered) {
				distance = maxDistance;
			}
			else if (state == PowerTrailState.depowered) {
				distance = 0;
			}
		}

		void Update() {
			if (DEBUG && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown("t")) {
				powerIsOn = !powerIsOn;
			}

			float prevDistance = distance;
			float nextDistance = NextDistance();
			if (nextDistance == prevDistance && isInitialized) return;
			isInitialized = true;

			// DEBUG: Remove this from Update after debugging
			//PopulateStaticGPUInfo();

			UpdateInterpolationValues(nextDistance);

			UpdateState(prevDistance, nextDistance);
			distance = nextDistance;
		}

		void PopulateStaticGPUInfo() {
			nodePositions = new Vector4[MAX_NODES];
			interpolationValues = new float[MAX_NODES];
			startNodeIndex = new int[MAX_NODES];
			endNodeIndex = new int[MAX_NODES];

			for (int i = 0; i < MAX_NODES && i < powerNodes.Count; i++) {
				Node nodeAtIndex = powerNodes.allNodes[i];
				nodePositions[i] = transform.TransformPoint(nodeAtIndex.pos);
			}
			foreach (var material in materials) {
				material.SetVectorArray(nodePositionsKey, nodePositions);
			}

			for (int i = 0; i < MAX_NODES && i < trailInfo.Count; i++) {
				NodeTrailInfo trailInfoAtIndex = trailInfo[i];

				startNodeIndex[i] = powerNodes.allNodes.IndexOf(trailInfoAtIndex.parent);
				endNodeIndex[i] = powerNodes.allNodes.IndexOf(trailInfoAtIndex.thisNode);
			}

			foreach (var material in materials) {
				material.SetFloatArray(startPositionIDsKey, startNodeIndex.Select(i => (float)i).ToArray());
				material.SetFloatArray(endPositionIDsKey, endNodeIndex.Select(i => (float)i).ToArray());
				material.SetFloat(sdfCapsuleRadiusKey, powerTrailRadius);
			}
		}

		void PopulateTrailInfo() {
			PopulateTrailInfoRecursively(powerNodes.rootNode, 0);
		}

		void PopulateTrailInfoRecursively(Node curNode, float curDistance) {
			if (!curNode.isRootNode) {
				Node parentNode = curNode.parent;
				float endDistance = parentNode.zeroDistanceToChildren ? curDistance : curDistance + (curNode.pos - parentNode.pos).magnitude;
				// If there is a parent node, add trail info here
				NodeTrailInfo info = new NodeTrailInfo {
					parent = parentNode,
					thisNode = curNode,
					startDistance = curDistance,
					endDistance = endDistance
				};
				trailInfo.Add(info);

				// Update maxDistance as you add new trail infos
				if (info.endDistance > maxDistance) {
					maxDistance = info.endDistance;
				}

				// Recurse for each child
				if (!curNode.isLeafNode) {
					foreach (Node child in curNode.children) {
						PopulateTrailInfoRecursively(child, info.endDistance);
					}
				}
			}
			// Base case of root parent node
			else {
				foreach (Node child in curNode.children) {
					PopulateTrailInfoRecursively(child, curDistance);
				}
			}
		}

		void UpdateInterpolationValues(float newDistance) {
			if (reverseVisibility) {
				newDistance = maxDistance - newDistance;
			}
			for (int i = 0; i < MAX_NODES && i < trailInfo.Count; i++) {
				NodeTrailInfo infoAtIndex = trailInfo[i];
				interpolationValues[i] = Mathf.Clamp01(Mathf.InverseLerp(infoAtIndex.startDistance, infoAtIndex.endDistance, newDistance));
			}
			foreach (var material in materials) {
				material.SetInt(reverseVisibilityKey, reverseVisibility ? 1 : 0);
				material.SetFloatArray(interpolationValuesKey, interpolationValues);
			}
		}

		void UpdateState(float prevDistance, float nextDistance) {
			if (powerIsOn) {
				if (prevDistance == 0 && nextDistance > 0) {
					sound.Play();
					state = PowerTrailState.partiallyPowered;
				}
				else if (prevDistance < maxDistance && nextDistance == maxDistance) {
					state = PowerTrailState.powered;
				}
			}
			else if (!powerIsOn) {
				if (prevDistance == maxDistance && nextDistance < maxDistance) {
					state = PowerTrailState.partiallyPowered;
				}
				else if (prevDistance > 0 && nextDistance == 0) {
					sound.Stop();
					state = PowerTrailState.depowered;
				}
			}
		}

		float NextDistance() {
			float effectiveSpeed = useSeparateSpeedsForPowerOnOff && !powerIsOn ? speedPowerOff : speed;
			if (powerIsOn && distance < maxDistance) {
				return Mathf.Min(maxDistance, distance + Time.deltaTime * effectiveSpeed);
			}
			else if (!powerIsOn && distance > 0) {
				return Mathf.Max(0, distance - Time.deltaTime * effectiveSpeed);
			}
			else return distance;
		}
#region Audio
		IEnumerator InitSound() {
			if (sound == null) {
				sound = gameObject.AddComponent<SoundEffectAtLocation>();
				sound.location = transform.position;
				yield return new WaitUntil(() => sound.audioSource != null);
				sound.audioSource.loop = true;
				sound.audioSource.playOnAwake = false;
				sound.audioSource.clip = Resources.Load<AudioClip>("Audio/Sounds/Objects/Electrical/ElectricalHum2Looping");
				sound.audioSource.spatialBlend = 1f;
				sound.audioSource.dopplerLevel = 0.125f;
				sound.audioSource.pitch = 0.5f;
			}
		}
		IEnumerator UpdateAudio() {
			float minVolume = 0.15f;
			float maxVolume = 1f;
			WaitForSeconds shortWait = new WaitForSeconds(.025f);
			WaitForSeconds longWait = new WaitForSeconds(.5f);
			while (true) {
				float maxSoundDistance = 30f;
				if (state == PowerTrailState.depowered || sound.audioSource == null) {
					//Debug.Log($"{gameObject.name} is off.");
					yield return longWait;
					continue;
				}

				Vector3 closestPoint = Vector3.zero;
				float minDistance = maxSoundDistance + 1f;
				// If the player is within maxSoundDistance from any collider of this PowerTrail
				if (Physics.OverlapSphere(Player.instance.transform.position, maxSoundDistance, 1 << gameObject.layer).Where(c => colliders.Contains(c)).Any()) {
					//Debug.Log($"PLAYER CLOSE TO {gameObject.name}");
					for (int i = 0; i < MAX_NODES && i < trailInfo.Count; i++) {
						if (interpolationValues[i] == 0) continue;

						int startIndex = startNodeIndex[i];
						int endIndex = endNodeIndex[i];
						Vector3 startPoint = nodePositions[startIndex];
						Vector3 endPoint = nodePositions[endIndex];
						if (interpolationValues[i] < 1) {
							endPoint = Vector3.Lerp(startPoint, endPoint, interpolationValues[i]);
						}

						Vector3 nearestPointOnLine = FindNearestPointOnLine(startPoint, endPoint, EpitaphScreen.instance.playerCamera.transform.position);
						float distanceToNearestPointOnLine = (EpitaphScreen.instance.playerCamera.transform.position - nearestPointOnLine).magnitude;

						if (distanceToNearestPointOnLine < minDistance) {
							minDistance = distanceToNearestPointOnLine;
							closestPoint = nearestPointOnLine;
						} 
					}
				}

				if (minDistance < maxSoundDistance) {
					//debug.Log($"PLAYER IS {minDistance} FROM {gameObject.name}");
					sound.location = closestPoint;
					sound.audioSource.volume = maxVolume * (distance / maxDistance);
					sound.pitch = 0.5f * (distance / maxDistance);
					//sound.audioSource.volume = Mathf.Lerp(minVolume, maxVolume, Mathf.InverseLerp(0f, maxSoundDistance, minDistance));
				}
				else {
					sound.audioSource.volume = 0f;
				}

				yield return shortWait;
			}
		}

		private Vector3 FindNearestPointOnLine(Vector3 start, Vector3 end, Vector3 point) {
			//Get heading
			Vector3 heading = (end - start);
			float magnitudeMax = heading.magnitude;
			heading.Normalize();

			//Do projection from the point but clamp it
			Vector3 lhs = point - start;
			float dotP = Vector3.Dot(lhs, heading);
			dotP = Mathf.Clamp(dotP, 0f, magnitudeMax);
			return start + heading * dotP;
		}
#endregion
#region EditorGizmos
		bool editorGizmosEnabled = false;
		public static float gizmoSphereSize = 0.15f;
		private void OnDrawGizmos() {
			if (powerNodes == null) {
				powerNodes = GetComponent<NodeSystem>();
			}

			if (powerNodes == null || powerNodes.rootNode == null || !editorGizmosEnabled) return;

			DrawGizmosRecursively(powerNodes.rootNode);
		}

		Color unselectedColor = new Color(.15f, .85f, .25f);
		Color selectedColor = new Color(.95f, .95f, .15f);
		void DrawGizmosRecursively(Node curNode) {
			Gizmos.color = (curNode == powerNodes.selectedNode) ? selectedColor : unselectedColor;

			foreach (Node child in curNode.children) {
				if (child != null) {
					DrawWireBox(curNode.pos, child.pos);
				}
			}
			foreach (Node child in curNode.children) {
				if (child != null) {
					DrawGizmosRecursively(child);
				}
			}
		}

		void DrawWireBox(Vector3 n1, Vector3 n2) {
			float halfBoxSize = powerTrailRadius / 2f;
			Vector3 diff = n2 - n1;
			Vector3 absDiff = new Vector3(Mathf.Abs(diff.x), Mathf.Abs(diff.y), Mathf.Abs(diff.z));

			Vector3 bl, br, tl, tr;
			if (absDiff.x > absDiff.y && absDiff.x > absDiff.z) {
				bl = new Vector3(0, -1, -1);
				br = new Vector3(0, -1, 1);
				tl = new Vector3(0, 1, -1);
				tr = new Vector3(0, 1, 1);
			}
			else if (absDiff.y > absDiff.x && absDiff.y > absDiff.z) {
				bl = new Vector3(-1, 0, -1);
				br = new Vector3(-1, 0, 1);
				tl = new Vector3(1, 0, -1);
				tr = new Vector3(1, 0, 1);
			}
			else {
				bl = new Vector3(-1, -1, 0);
				br = new Vector3(-1, 1, 0);
				tl = new Vector3(1, -1, 0);
				tr = new Vector3(1, 1, 0);
			}

			Vector3[] from = new Vector3[4] {
			n1 - bl * halfBoxSize,
			n1 - br * halfBoxSize,
			n1 - tl * halfBoxSize,
			n1 - tr * halfBoxSize
		};
			Vector3[] to = new Vector3[4] {
			n2 - bl * halfBoxSize,
			n2 - br * halfBoxSize,
			n2 - tl * halfBoxSize,
			n2 - tr * halfBoxSize
		};

			Vector3 direction = diff.normalized;
			for (int i = 0; i < 4; i++) {
				Gizmos.DrawLine(from[i], to[i]);
			}
		}
#endregion
	}
}