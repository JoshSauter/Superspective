using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using System.Net.Mime;
using System.Linq;
using System.IO;
using NaughtyAttributes;
using Audio;
using Saving;
using System.Runtime.Serialization.Formatters.Binary;
using static Audio.AudioManager;

namespace PowerTrailMechanics {
	public class NodeTrailInfo {
		public Node parent;
		public Node thisNode;
		public float startDistance;
		public float endDistance;
	}

	[RequireComponent(typeof(UniqueId))]
	public class PowerTrail : SaveableObject<PowerTrail, PowerTrail.PowerTrailSave>, CustomAudioJob {
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
		bool useSameSpeedsForPowerOnOff => !useSeparateSpeedsForPowerOnOff;
		[ShowIf("useDurationInsteadOfSpeed")]
		public float targetDuration = 3f;
		[ShowIf(EConditionOperator.And, "useDurationInsteadOfSpeed", "useSeparateSpeedsForPowerOnOff")]
		public float targetDurationPowerOff = 3f;
		[HideIf("useDurationInsteadOfSpeed")]
		public float speed = 15f;
		[HideIf(EConditionOperator.Or, "useDurationInsteadOfSpeed", "useSameSpeedsForPowerOnOff")]
		public float speedPowerOff = 15f;
		public float powerTrailRadius = 0.15f;

		//public SoundEffectAtLocation sound;

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
		public bool fullyPowered => distance >= maxDistance;
		[SerializeField]
		PowerTrailState _state = PowerTrailState.depowered;
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

		protected override void Awake() {
			base.Awake();
			if (powerNodes == null) {
				powerNodes = GetComponent<NodeSystem>();
			}
			gameObject.layer = LayerMask.NameToLayer("VisibleButNoPlayerCollision");
		}

		protected override void Start() {
			base.Start();
			if (renderers == null || renderers.Length == 0) {
				renderers = GetComponents<Renderer>();
			}
			if (colliders == null || colliders.Length == 0) {
				colliders = renderers.Select(r => r.GetComponent<Collider>()).Where(c => c != null).ToArray();
			}
			materials = renderers.Select(r => r.material).ToArray();
			PopulateTrailInfo();
			if (useDurationInsteadOfSpeed) {
				speed = maxDistance / targetDuration;
				if (useSeparateSpeedsForPowerOnOff) {
					speedPowerOff = maxDistance / targetDurationPowerOff;
				}
			}

			PopulateStaticGPUInfo();
			AudioManager.instance.PlayWithUpdate(AudioName.PowerTrailHum, ID, this);
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
			Material[] nowMaterials = renderers.Select(r => r.material).ToArray();
			if (materials != null && nowMaterials != null && !materials.SequenceEqual(nowMaterials)) {
				materials = nowMaterials;
				PopulateStaticGPUInfo();
			}
			

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
		public void UpdateAudio(AudioJob audioJob) {
			if (this == null || gameObject == null) {
				audioJob.Stop();
				return;
			}

			float shortWait = .025f;
			float longWait = .5f;
			float timeElapsed = audioJob.timeRunning;
			if (state == PowerTrailState.depowered || audioJob.audio.volume == 0) {
				// Only check once every longWait for this expensive calculation
				if (timeElapsed < longWait) {
					return;
				}
				else {
					audioJob.timeRunning = timeElapsed % longWait;
				}
			}
			// Only check once every shortWait for this expensive calculation
			else if (timeElapsed < shortWait) {
				return;
			}
			else {
				audioJob.timeRunning = timeElapsed % shortWait;
			}

			float maxSoundDistance = 30f;
			Vector3 closestPoint = Vector3.zero;
			float minDistance = maxSoundDistance + 1f;
			// If the player is within maxSoundDistance from any collider of this PowerTrail
			if (Physics.OverlapSphere(Player.instance.transform.position, maxSoundDistance, 1 << gameObject.layer).Any(c => colliders.Contains(c))) {
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

					Vector3 nearestPointOnLine = FindNearestPointOnLine(startPoint, endPoint, SuperspectiveScreen.instance.playerCamera.transform.position);
					float distanceToNearestPointOnLine = (SuperspectiveScreen.instance.playerCamera.transform.position - nearestPointOnLine).magnitude;

					if (distanceToNearestPointOnLine < minDistance) {
						minDistance = distanceToNearestPointOnLine;
						closestPoint = nearestPointOnLine;
					}
				}
			}

			if (minDistance < maxSoundDistance) {
				//debug.Log($"PLAYER IS {minDistance} FROM {gameObject.name}");
				audioJob.audio.transform.position = closestPoint;
				audioJob.audio.volume = distance / maxDistance;
				audioJob.audio.pitch = 0.5f * (distance / maxDistance);
			}
			else {
				audioJob.audio.volume = 0f;
			}
		}

		Vector3 FindNearestPointOnLine(Vector3 start, Vector3 end, Vector3 point) {
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
#region Saving
		
		[Serializable]
		public class PowerTrailSave : SerializableSaveObject<PowerTrail> {
			public bool reverseVisibility;
			public bool useDurationInsteadOfSpeed;
			public bool useSeparateSpeedsForPowerOnOff;
			public float targetDuration;
			public float targetDurationPowerOff;
			public float speed;
			public float speedPowerOff;
			public float powerTrailRadius;
			public float distance;
			public float maxDistance;
			public bool powerIsOn;
			public int state;

			public PowerTrailSave(PowerTrail powerTrail) : base(powerTrail) {
				this.reverseVisibility = powerTrail.reverseVisibility;
				this.useDurationInsteadOfSpeed = powerTrail.useDurationInsteadOfSpeed;
				this.useSeparateSpeedsForPowerOnOff = powerTrail.useSeparateSpeedsForPowerOnOff;
				this.targetDuration = powerTrail.targetDuration;
				this.targetDurationPowerOff = powerTrail.targetDurationPowerOff;
				this.speed = powerTrail.speed;
				this.speedPowerOff = powerTrail.speedPowerOff;
				this.powerTrailRadius = powerTrail.powerTrailRadius;
				this.distance = powerTrail.distance;
				this.maxDistance = powerTrail.maxDistance;
				this.powerIsOn = powerTrail.powerIsOn;
				this.state = (int)powerTrail._state;
			}

			public override void LoadSave(PowerTrail powerTrail) {
				powerTrail.reverseVisibility = this.reverseVisibility;
				powerTrail.useDurationInsteadOfSpeed = this.useDurationInsteadOfSpeed;
				powerTrail.useSeparateSpeedsForPowerOnOff = this.useSeparateSpeedsForPowerOnOff;
				powerTrail.targetDuration = this.targetDuration;
				powerTrail.targetDurationPowerOff = this.targetDurationPowerOff;
				powerTrail.speed = this.speed;
				powerTrail.speedPowerOff = this.speedPowerOff;
				powerTrail.powerTrailRadius = this.powerTrailRadius;
				powerTrail.distance = this.distance;
				powerTrail.maxDistance = this.maxDistance;
				// If we're at min or max distance, bring us in ever so slightly so that it re-finishes the animation
				if (powerTrail.distance == powerTrail.maxDistance) {
					powerTrail.distance -= 0.00001f;
				}
				else if (powerTrail.distance == 0) {
					powerTrail.distance += 0.00001f;
				}
				powerTrail.powerIsOn = this.powerIsOn;
				powerTrail._state = (PowerTrailState)this.state;
			}
		}
#endregion
#region EditorGizmos
		bool editorGizmosEnabled = false;
		public static float gizmoSphereSize = 0.15f;

		void OnDrawGizmos() {
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