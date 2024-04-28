using System;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using System.Linq;
using NaughtyAttributes;
using Audio;
using Saving;
using PoweredObjects;
using StateUtils;
using UnityEngine.Events;
using UnityEngine.Serialization;
using static Audio.AudioManager;

namespace PowerTrailMechanics {
	public class NodeTrailInfo {
		public Node parent;
		public Node thisNode;
		public float startDistance;
		public float endDistance;
	}

	[RequireComponent(typeof(UniqueId), typeof(PoweredObject))]
	public class PowerTrail : SaveableObject<PowerTrail, PowerTrail.PowerTrailSave>, CustomAudioJob {
		[Label("Power")]
		[SerializeField]
		private PoweredObject _pwr;
		public PoweredObject pwr {
			get {
				if (_pwr == null) {
					_pwr = this.GetOrAddComponent<PoweredObject>();
				}

				return _pwr;
			}
			set => _pwr = value;
		}
		public Renderer[] renderers;
		Collider[] colliders;
		Material[] materials;
		public NodeSystem powerNodes;
		public List<NodeTrailInfo> trailInfo = new List<NodeTrailInfo>();
		public List<NodeTrailInfo> simplePath = new List<NodeTrailInfo>();
		[ShowNativeProperty]
		public int simplePathNodes => simplePath.Count;

		public int numAudioSources = 4; // Max one per simplePath segment
		// trail segment -> audioJob.uniqueIdentifier
		private readonly TwoWayDictionary<NodeTrailInfo, string> audioSegments = new TwoWayDictionary<NodeTrailInfo, string>();

		// Data to be sent to the GPU. Positions are not repeated. Index order matches node ID
		const int MAX_NODES = 512;
		Vector4[] nodePositions;        // Positions of each node. w value is unused and ignored
		int[] endNodeIndex;
		int[] startNodeIndex;
		float[] interpolationValues;    // [0-1] interpolation value between startPosition and endPosition for each trail. Only GPU data that changes at runtime
		[FormerlySerializedAs("reverseVisibility")]
		public bool reverseDirection = false;
		const string NODE_POSITIONS_KEY = "_NodePositions";
		const string START_POSITION_IDS_KEY = "_StartPositionIDs";
		const string END_POSITION_IDS_KEY = "_EndPositionIDs";
		const string INTERPOLATION_VALUES_KEY = "_InterpolationValues";
		const string SDF_CAPSULE_RADIUS_KEY = "_CapsuleRadius";
		const string HIDDEN_POWER_TRAIL_KEY = "_HiddenPowerTrail";
		const string USE_CYLINDER_KEY = "_UseCylinder";
		const string REVERSE_VISIBILITY_KEY = "_ReverseVisibility";
		const string POWER_TRAIL_KEYWORD = "POWER_TRAIL_OBJECT";

		public bool useDurationInsteadOfSpeed = false;
		public bool useSeparateSpeedsForPowerOnOff = false;
		// Just used for NaughtyAttributes
		bool useSameSpeedsForPowerOnOff => !useSeparateSpeedsForPowerOnOff;
		[ShowIf(nameof(useDurationInsteadOfSpeed))]
		public float targetDuration = 3f;
		[ShowIf(EConditionOperator.And, nameof(useDurationInsteadOfSpeed), nameof(useSeparateSpeedsForPowerOnOff))]
		public float targetDurationPowerOff = 3f;
		[HideIf(nameof(useDurationInsteadOfSpeed))]
		public float speed = 15f;
		[HideIf(EConditionOperator.Or, nameof(useDurationInsteadOfSpeed), nameof(useSameSpeedsForPowerOnOff))]
		public float speedPowerOff = 15f;
		public float powerTrailRadius = 0.15f;
		public bool skipStartupShutdownSounds = false;
		public bool objectMoves = false;
		public bool hiddenPowerTrail = false;
		[Header("Cylinders can look better for straight-line segments, but worse on staircases or other complex paths")]
		public bool useCylinderInsteadOfCapsule = false;
		[ShowIf(nameof(hiddenPowerTrail))]
		public bool revealAfterPowering = false;

		private bool _pauseRendering = false;
		public bool PauseRendering {
			get => _pauseRendering;
			set {
				foreach (var material in materials) {
					if (!material.IsKeywordEnabled(POWER_TRAIL_KEYWORD) && !value) {
						material.EnableKeyword(POWER_TRAIL_KEYWORD);
					}
					else if (material.IsKeywordEnabled(POWER_TRAIL_KEYWORD) && value) {
						material.DisableKeyword(POWER_TRAIL_KEYWORD);
					}
				}

				_pauseRendering = value;
			}
		}

		///////////
		// State //
		///////////
		public float duration => maxDistance / speed;
		public float durationOff => useSeparateSpeedsForPowerOnOff ? maxDistance / speedPowerOff : duration;
		[Header("Current State")]
		public float distance = 0f;
		public float maxDistance = 0f;
		public float targetFillAmount = 1f; // 0-1 value representing how much distance/maxDistance to allow the PowerTrail to fill

		[ShowNativeProperty]
		public bool IsFullyPowered => distance >= maxDistance;
		[ShowNativeProperty]
		public bool IsFullyDepowered => distance <= 0;
		bool isInitialized = false;

		private DimensionObject thisDimensionObject;
		// If there's no parent dimension object, set the layer dynamically based on the player's size
		private int EffectiveLayer {
			get {
				// Don't change the layer if there is a parent dimension object
				if (thisDimensionObject != null) {
					return gameObject.layer;
				}

				// UI only layer that doesn't collide w/ anything right now
				if (hiddenPowerTrail) {
					return LayerMask.NameToLayer("UI");
				}
				
				return LayerMask.NameToLayer(Player.instance.Scale < 1 ? "Default" : "VisibleButNoPlayerCollision");
			}
		}

		protected override void Awake() {
			base.Awake();
			if (powerNodes == null) {
				powerNodes = GetComponent<NodeSystem>();
			}

			thisDimensionObject = gameObject.FindDimensionObjectRecursively<DimensionObject>();
			
			if (renderers == null || renderers.Length == 0) {
				renderers = GetComponents<Renderer>();
			}
		}

		protected override void Start() {
			base.Start();
			if (pwr == null) {
				Debug.LogError($"{this.FullPath()}: pwr is null. Disabling PowerTrail");
				enabled = false;
				return;
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
			InitializePowerStateMachine();
			SetStartState();

			switch (pwr.parentMultiMode) {
				case MultiMode.Single:
					if (pwr.source != null && pwr.source.GetComponent<PowerTrail>() != null) {
						skipStartupShutdownSounds = true;
					}
					break;
				case MultiMode.Any:
				case MultiMode.All:
					if (pwr.sources != null && pwr.sources.Length > 0 && pwr.sources.ToList().Exists(source => source.GetComponent<PowerTrail>() != null)) {
						skipStartupShutdownSounds = true;
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			Player.instance.growShrink.state.OnStateChangeSimple += SetLayers;
			SetLayers();
		}

		void SetLayers() {
			foreach (var renderer in renderers) {
				renderer.gameObject.layer = EffectiveLayer;
			}
		}

		private void OnDisable() {
			Player.instance.growShrink.state.OnStateChangeSimple -= SetLayers;
		}

		protected override void Init() {
			base.Init();

			InitializeAudioSegments();
		}

		private void InitializePowerStateMachine() {
			// Bootup and shutdown SFX
			pwr.state.OnStateChangeSimple += () => {
				if (!GameManager.instance.gameHasLoaded) return;
				if (!pwr.PartiallyPowered || skipStartupShutdownSounds) return;

				AudioManager.instance.Play(pwr.PowerIsOn ? AudioName.PowerTrailBootup : AudioName.PowerTrailShutdown, ID, true);
			};
			
			// State transitions
			pwr.state.AddStateTransition(PowerState.PartiallyDepowered, PowerState.Depowered, () => IsFullyDepowered);
			pwr.state.AddStateTransition(PowerState.PartiallyPowered, PowerState.Powered, () => IsFullyPowered);
		}

		void InitializeAudioSegments() {
			// TODO: Revert iterator to numAudioSources after debugging slow performance
			for (int i = 0; i < 1/*numAudioSources*/; i++) {
				// Axiom: Audio job's uniqueIdentifier ends with "_<0 to numAudioSources>"
				string audioId = $"{ID}_{i}";
				AudioManager.instance.PlayWithUpdate(AudioName.PowerTrailHum, audioId, this);
			}
		}

		void SetStartState() {
			isInitialized = false;
			if (pwr.FullyPowered) {
				distance = maxDistance;
			}
			else if (pwr.FullyDepowered) {
				distance = 0;
			}
		}

		void Update() {
			if (DEBUG && DebugInput.GetKey(KeyCode.LeftShift) && DebugInput.GetKeyDown("t")) {
				pwr.PowerIsOn = !pwr.PowerIsOn;
			}

			if (objectMoves) {
				PopulateNodePositions();
			}
			
			float prevDistance = distance;
			float nextDistance = NextDistance();
			if (nextDistance == prevDistance && isInitialized) return;
			isInitialized = true;

			if (hiddenPowerTrail && pwr.PowerIsOn && gameObject.layer != SuperspectivePhysics.VisibleButNoPlayerCollisionLayer) {
				foreach (var renderer in renderers) {
					renderer.gameObject.layer = SuperspectivePhysics.VisibleButNoPlayerCollisionLayer;
				}
			}

			if (hiddenPowerTrail && revealAfterPowering && IsFullyPowered) {
				hiddenPowerTrail = false;
				foreach (var material in materials) {
					material.SetInt(HIDDEN_POWER_TRAIL_KEY, 0);
				}
			}

			Material[] nowMaterials = renderers.Select(r => r.material).ToArray();
			if (materials != null && nowMaterials != null && !materials.SequenceEqual(nowMaterials)) {
				materials = nowMaterials;
				PopulateStaticGPUInfo();
			}
			
			distance = nextDistance;
			
			UpdateInterpolationValues(nextDistance);
		}

		void PopulateStaticGPUInfo() {
			interpolationValues = new float[MAX_NODES];
			startNodeIndex = new int[MAX_NODES];
			endNodeIndex = new int[MAX_NODES];

			PopulateNodePositions();

			for (int i = 0; i < MAX_NODES && i < trailInfo.Count; i++) {
				NodeTrailInfo trailInfoAtIndex = trailInfo[i];

				startNodeIndex[i] = powerNodes.allNodes.IndexOf(trailInfoAtIndex.parent);
				endNodeIndex[i] = powerNodes.allNodes.IndexOf(trailInfoAtIndex.thisNode);
			}

			foreach (var material in materials) {
				material.SetFloatArray(START_POSITION_IDS_KEY, startNodeIndex.Select(i => (float)i).ToArray());
				material.SetFloatArray(END_POSITION_IDS_KEY, endNodeIndex.Select(i => (float)i).ToArray());
				material.SetFloat(SDF_CAPSULE_RADIUS_KEY, powerTrailRadius);
				material.SetInt(HIDDEN_POWER_TRAIL_KEY, hiddenPowerTrail ? 1 : 0);
				material.SetInt(USE_CYLINDER_KEY, useCylinderInsteadOfCapsule ? 1 : 0);
			}
		}

		void PopulateNodePositions() {
			nodePositions = new Vector4[MAX_NODES];

			for (int i = 0; i < MAX_NODES && i < powerNodes.Count; i++) {
				Node nodeAtIndex = powerNodes.allNodes[i];
				nodePositions[i] = transform.TransformPoint(nodeAtIndex.pos);
			}
			foreach (var material in materials) {
				material.SetVectorArray(NODE_POSITIONS_KEY, nodePositions);
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

				// Create the simple path as we traverse the nodes
				if (!parentNode.zeroDistanceToChildren) {
					if (parentNode.staircaseSegment) {
						if (curNode.isLeafNode) {
							// End of staircase segment, find start and add segment
							Node end = curNode;
							Node start = parentNode;
							float startDistance = curDistance;
							while (start.parent?.staircaseSegment ?? false) {
								startDistance -= (start.pos - start.parent.pos).magnitude;
								start = start.parent;
							}
							
							// Add just the staircase segment
							NodeTrailInfo staircaseSegment = new NodeTrailInfo {
								parent = start,
								thisNode = end,
								startDistance = startDistance,
								endDistance = curDistance
							};
							simplePath.Add(staircaseSegment);
						}
						else if (!curNode.staircaseSegment) {
							// End of staircase segment, find start and add segment
							Node end = parentNode;
							Node start = parentNode;
							float startDistance = curDistance;
							while (start.parent?.staircaseSegment ?? false) {
								startDistance -= (start.pos - start.parent.pos).magnitude;
								start = start.parent;
							}

							// Add the staircase segment first
							NodeTrailInfo staircaseSegment = new NodeTrailInfo {
								parent = start,
								thisNode = end,
								startDistance = startDistance,
								endDistance = curDistance
							};
							simplePath.Add(staircaseSegment);
							
							// Add this segment as normal as well
							NodeTrailInfo segment = new NodeTrailInfo {
								parent = parentNode,
								thisNode = curNode,
								startDistance = curDistance,
								endDistance = endDistance
							};
							simplePath.Add(segment);
						}
					}
					// parent is not a staircase segment
					else {
						// Add line segment as normal
						NodeTrailInfo segment = new NodeTrailInfo {
							parent = parentNode,
							thisNode = curNode,
							startDistance = curDistance,
							endDistance = endDistance
						};
						simplePath.Add(segment);
					}
				}

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
			if (reverseDirection) {
				newDistance = maxDistance - newDistance;
			}
			for (int i = 0; i < MAX_NODES && i < trailInfo.Count; i++) {
				NodeTrailInfo infoAtIndex = trailInfo[i];
				interpolationValues[i] = Mathf.Clamp01(Mathf.InverseLerp(infoAtIndex.startDistance, infoAtIndex.endDistance, newDistance));
			}
			foreach (var material in materials) {
				material.SetInt(REVERSE_VISIBILITY_KEY, reverseDirection ? 1 : 0);
				material.SetFloatArray(INTERPOLATION_VALUES_KEY, interpolationValues);
				if (PauseRendering) {
					material.DisableKeyword(POWER_TRAIL_KEYWORD);
				}
				else if (!material.IsKeywordEnabled(POWER_TRAIL_KEYWORD) && !IsFullyPowered) {
					material.EnableKeyword(POWER_TRAIL_KEYWORD);
				}
				else if (material.IsKeywordEnabled(POWER_TRAIL_KEYWORD) && IsFullyPowered) {
					material.DisableKeyword(POWER_TRAIL_KEYWORD);
				}
			}
		}

		float NextDistance() {
			float effectiveSpeed = useSeparateSpeedsForPowerOnOff && !pwr.PowerIsOn ? speedPowerOff : speed;
			float max = maxDistance * targetFillAmount;
			if (pwr.PowerIsOn && distance < max) {
				return Mathf.Min(max, distance + Time.deltaTime * effectiveSpeed);
			}
			else if (!pwr.PowerIsOn && distance > 0) {
				return Mathf.Max(0, distance - Time.deltaTime * effectiveSpeed);
			}
			else return distance;
		}
#region Audio
		public void UpdateAudioJob(AudioJob audioJob) {
			if (this == null || gameObject == null) {
				audioJob.Stop();
				return;
			}

			const float shortWait = .025f;
			const float longWait = .5f;
			float timeElapsed = audioJob.timeRunning;
			if (pwr.FullyDepowered || audioJob.audio.volume == 0) {
				// If depowered, only check once every longWait for this expensive calculation
				if (timeElapsed < longWait) {
					return;
				}
				else {
					audioJob.timeRunning = timeElapsed % longWait;
				}
			}
			// If powered, only check once every shortWait for this expensive calculation
			else if (timeElapsed < shortWait) {
				return;
			}
			else {
				audioJob.timeRunning = timeElapsed % shortWait;
			}

			const float maxSoundDistance = 30f;
			List<(NodeTrailInfo, Vector3, float)> simplePathSegmentsByDistance = simplePath.Select(NearestPointOnSegment)
				.Where(tuple => tuple.Item3 < maxSoundDistance) // Filter out segments where closest point is too far
				.OrderBy(tuple => tuple.Item3) // Order by how close a segment is to player cam
				.Take(numAudioSources) // get numAudioSources closest segments
				.ToList();
			// If this audio source is already being used in an audio segment
			if (audioSegments.ContainsValue(audioJob.uniqueIdentifier)) {
				(NodeTrailInfo nodeTrailInfo, Vector3 closestPoint, float distanceToCam) = simplePathSegmentsByDistance.Find(tuple =>
					tuple.Item1 == audioSegments[audioJob.uniqueIdentifier]);
				// If that segment is one of the closest segments, keep it along the same segment
				if (nodeTrailInfo != null) {
					if (!audioJob.audio.isPlaying) {
						audioJob.Play();
					}
					
					audioJob.audio.transform.position = closestPoint;
					audioJob.audio.volume = distance / maxDistance * (1-(distanceToCam / maxSoundDistance));
					audioJob.basePitch = 0.25f + 0.25f * (distance / maxDistance);
				}
				// If that segment is not one of the closest segments anymore, remove it from that segment
				else {
					audioSegments.Remove(audioJob.uniqueIdentifier);
				}
			}
			
			// Check again because we may have just removed an item from the list of audio segments
			if (!audioSegments.ContainsValue(audioJob.uniqueIdentifier)) {
				// This audio source is free to be moved to a new segment
				if (simplePathSegmentsByDistance.Count > 0) {
					// Find the closest segment that doesn't already have an audio source and use that
					(NodeTrailInfo nodeTrailInfo, Vector3 closestPoint, float distanceToCam) = simplePathSegmentsByDistance.Find(
						tuple => !audioSegments.ContainsKey(tuple.Item1));
					if (nodeTrailInfo != null) {
						audioSegments.Add(nodeTrailInfo, audioJob.uniqueIdentifier);

						audioJob.audio.transform.position = closestPoint;
						audioJob.audio.volume = distance / maxDistance * (1 - (distanceToCam / maxSoundDistance));
						audioJob.basePitch = 0.25f + 0.25f * (distance / maxDistance);
					}
					else {
						audioJob.audio.volume = 0f;
					}
				}
			}

			// (Segment, ClosestPoint, Distance from player cam to closest point)
			(NodeTrailInfo, Vector3, float) NearestPointOnSegment(NodeTrailInfo segment) {
				Vector3 cameraPos = SuperspectiveScreen.instance.playerCamera.transform.position;
				
				Vector3 start = transform.TransformPoint(segment.parent.pos);
				Vector3 end = transform.TransformPoint(segment.thisNode.pos);
				Vector3 closestPoint = start;
				if (distance < segment.startDistance) {
					audioJob.audio.volume = 0f;
					closestPoint = Vector3.one * int.MaxValue; // Somewhere very far away (don't use this segment)
				}
				else if (distance < segment.endDistance) {
					float i = (distance - segment.startDistance) / segment.endDistance;
					end = Vector3.Lerp(start, end, i);
					closestPoint = FindNearestPointOnLine(start, end, cameraPos);
				}
				else {
					closestPoint = FindNearestPointOnLine(start, end, cameraPos);
				}
				
				float distanceToCam = (cameraPos - closestPoint).magnitude;

				return (segment, closestPoint, distanceToCam);
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

			public PowerTrailSave(PowerTrail powerTrail) : base(powerTrail) {
				this.reverseVisibility = powerTrail.reverseDirection;
				this.useDurationInsteadOfSpeed = powerTrail.useDurationInsteadOfSpeed;
				this.useSeparateSpeedsForPowerOnOff = powerTrail.useSeparateSpeedsForPowerOnOff;
				this.targetDuration = powerTrail.targetDuration;
				this.targetDurationPowerOff = powerTrail.targetDurationPowerOff;
				this.speed = powerTrail.speed;
				this.speedPowerOff = powerTrail.speedPowerOff;
				this.powerTrailRadius = powerTrail.powerTrailRadius;
				this.distance = powerTrail.distance;
				this.maxDistance = powerTrail.maxDistance;
			}

			public override void LoadSave(PowerTrail powerTrail) {
				powerTrail.reverseDirection = this.reverseVisibility;
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
			}
		}
#endregion
#region EditorGizmos
		bool editorGizmosEnabled => DEBUG;

		void OnDrawGizmos() {
			if (powerNodes == null) {
				powerNodes = GetComponent<NodeSystem>();
			}

			if (powerNodes == null || powerNodes.rootNode == null || !editorGizmosEnabled) return;

			DrawGizmosRecursively(powerNodes.rootNode);
			DrawSimplePath(simplePath);
		}

		private void DrawSimplePath(List<NodeTrailInfo> lineSegments) {
			Color prevColor = Gizmos.color;
			Gizmos.color = Color.yellow;
			foreach (var segment in lineSegments) {
				// slightly offset just so its not obscured
				Vector3 start = transform.TransformPoint(segment.parent.pos + Vector3.up * 0.02f);
				Vector3 end = transform.TransformPoint(segment.thisNode.pos + Vector3.up*0.02f);
				Gizmos.DrawLine(start, end);
			}

			Gizmos.color = prevColor;
		}

		Color unselectedColor = new Color(.15f, .85f, .25f);
		Color selectedColor = new Color(.95f, .95f, .15f);
		void DrawGizmosRecursively(Node curNode) {
			Gizmos.color = (curNode == powerNodes.selectedNode) ? selectedColor : unselectedColor;

			foreach (Node child in curNode.children) {
				if (child != null) {
					DrawWireBox(transform.TransformPoint(curNode.pos), transform.TransformPoint(child.pos));
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