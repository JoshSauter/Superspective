using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GrowShrink;
using LevelManagement;
using MagicTriggerMechanics;
using MagicTriggerMechanics.TriggerConditions;
using Sirenix.OdinInspector;
using SuperspectiveUtils;
using UnityEngine;

namespace PortalMechanics {
    public enum PortalPhysicsMode : byte {
        Normal = 0, // Player and PortalableObjects will pass through the portal as normal
        Wall = 1,   // All objects will collide with the portal as if it were a wall
        None = 2    // All objects will pass through the portal as if it were not there
    }
    
    public partial class Portal {
	    private const float PORTAL_TRIGGER_THICKNESS = 1.55f;
        private const int GLOBAL_FRAMES_TO_WAIT_AFTER_TELEPORT = 5;
        
        [SerializeField]
        [TabGroup("Physics"), GUIColor(0.95f, 0.55f, .55f)]
        private PortalPhysicsMode _physicsMode = PortalPhysicsMode.Normal;
        public PortalPhysicsMode PhysicsMode {
            get => _physicsMode;
            set {
                _physicsMode = value;
                ApplyPortalPhysicsModeToColliders();
            }
        }
        
        [OnValueChanged(nameof(SetScaleFactor))]
        [TabGroup("Physics"), GUIColor(0.95f, 0.55f, .55f)]
        public bool changeScale = false;
        [ShowIf(nameof(changeScale))]
        [OnValueChanged(nameof(SetScaleFactor))]
        [Tooltip("Multiply the player size by this value when passing through this Portal (and inverse for the other Portal)")]
        [Range(1f/64f, 64f)]
        [SerializeField]
        [TabGroup("Physics"), GUIColor(0.95f, 0.55f, .55f)]
        private float scaleFactor = 1;
        public float ScaleFactor => changeScale ? scaleFactor : 1f;
        
        [Tooltip("Largest size GrowShrinkObject that is allowed to pass through this portal")]
        [TabGroup("Physics"), GUIColor(0.95f, 0.55f, .55f)]
        public float maxScaleAllowed = 6f;
        private float MaxScaleAllowed => otherPortal != null ? Mathf.Max(maxScaleAllowed, otherPortal.maxScaleAllowed) : maxScaleAllowed;
        
        // Colliders are the infinitely thin planes on the Portal layer that interact with raycasts
        [TabGroup("Physics"), GUIColor(0.95f, 0.55f, .55f)]
        public Collider[] colliders;
        // Trigger colliders are the trigger zones that the CompositeMagicTriggers operate within to teleport the player/objects
        [TabGroup("Physics"), GUIColor(0.95f, 0.55f, .55f)]
        public Collider[] triggerColliders;
        
        [TabGroup("Physics"), GUIColor(0.95f, 0.55f, .55f)]
        public CompositeMagicTrigger trigger;
        
        private bool teleportingPlayer = false;
        [TabGroup("Physics"), GUIColor(0.95f, 0.55f, .55f)]
        public bool disableColliderWhilePaused = false;
        public readonly HashSet<PortalableObject> objectsInPortal = new HashSet<PortalableObject>();

        [ShowInInspector]
        [TabGroup("Physics"), GUIColor(0.95f, 0.55f, .55f)]
        public bool PortalLogicIsEnabled => otherPortal != null && PhysicsMode == PortalPhysicsMode.Normal && gameObject.activeSelf;
        public bool PlayerRemainsInPortal => PlayerIsInThisPortal || (otherPortal != null && otherPortal.PlayerIsInThisPortal);
        private bool PlayerIsInThisPortal => trigger.playerIsInTriggerZone;
        private float PortalTriggerThickness => PORTAL_TRIGGER_THICKNESS / (changeScale ? ScaleFactor : 1f);
        
        private Vector3 lastPlayerPositionProcessed;
		
        private static int globalLastTeleportedFrame = 0;
        private float lastTeleportedTime = 0f;

        private void PhysicsAwake() {
	        if (colliders == null || colliders.Length == 0) {
		        if (compositePortal) {
			        colliders = GetComponentsInChildren<Collider>();
		        }
		        else {
			        colliders = new Collider[] { GetComponent<Collider>() };
		        }
	        }

	        if (colliders.Length == 0) {
		        Debug.LogError("No Colliders found in this object or its children", gameObject);
		        enabled = false;
		        return;
	        }

	        triggerColliders = colliders.Select(c => {
		        string triggerName = $"{c.name} Trigger";
		        GameObject triggerColliderGO = new GameObject(triggerName);
		        triggerColliderGO.transform.SetParent(c.transform, false);
		        Collider triggerCollider = null;
		        switch (c) {
			        case BoxCollider boxCollider:
				        // Copy the collider's properties to the trigger collider
				        triggerCollider = triggerColliderGO.PasteComponent(boxCollider);
				        triggerCollider.isTrigger = true;

				        boxCollider.size = boxCollider.size.WithZ(PortalTriggerThickness);
				        break;
			        case MeshCollider meshCollider:
				        // Copy the collider's properties to the trigger collider
				        triggerCollider = triggerColliderGO.PasteComponent(meshCollider);
				        triggerCollider.isTrigger = true;
						
				        meshCollider.convex = false;
				        break;
			        default:
				        Debug.LogError("Unsupported collider type for portal trigger collider, only BoxCollider and MeshCollider are supported!");
				        break;
		        }

		        triggerColliderGO.name = triggerName;

		        return triggerCollider;
	        }).Where(c => c != null).ToArray();
	        
			colliders.ToList().ForEach(c => {
				string raycastBlockerName = $"{c.name} Raycast Blocker";
				GameObject raycastBlockerGO = new GameObject(raycastBlockerName);
				raycastBlockerGO.transform.SetParent(c.transform, false);
				Collider raycastBlocker = null;
				// Make the raycast blocker collider --infinitely thin-- to work with raycasts starting inside the portal
				switch (c) {
					case BoxCollider boxCollider:
						// Copy the collider's properties to the trigger collider
						raycastBlocker = raycastBlockerGO.PasteComponent(boxCollider);
						raycastBlocker.isTrigger = true;
						break;
					case MeshCollider meshCollider:
						// Copy the collider's properties to the trigger collider
						raycastBlocker = raycastBlockerGO.PasteComponent(meshCollider);
						raycastBlocker.isTrigger = true;

						meshCollider.convex = true;
						break;
					default:
						Debug.LogError("Unsupported collider type for portal trigger collider, only BoxCollider and MeshCollider are supported!");
						break;
				}
				raycastBlockerGO.transform.localScale = raycastBlockerGO.transform.localScale.WithZ(0f);
				// Move it just so slightly back to avoid player being slightly past the portal plane
				raycastBlockerGO.transform.position += IntoPortalVector * 0.1f;

				raycastBlockerGO.name = raycastBlockerName;
				raycastBlockerGO.layer = SuperspectivePhysics.PortalLayer;
			});
			
			CreateCompositeTrigger();
			InitializeCompositeTrigger();

			foreach (var c in triggerColliders) {
				c.gameObject.layer = SuperspectivePhysics.TriggerZoneLayer;
				if (c.gameObject != this.gameObject) {
					// PortalColliders handle non-player objects passing through portals
					c.gameObject.AddComponent<NonPlayerPortalTriggerZone>().portal = this;
				}
				
				// Give the trigger colliders some thickness so the player can't phase through them with lag
				if (c is BoxCollider boxCollider) {
					var size = boxCollider.size;
					boxCollider.size = size.WithZ(PortalTriggerThickness);
				}
			}
        }
        
        private void FixedUpdate() {
			foreach (Collider c in colliders) {
				c.isTrigger = PortalLogicIsEnabled;
				if (disableColliderWhilePaused) {
					c.enabled = PortalLogicIsEnabled;
				}
			}

			// If the player moves to the backside of a double-sided portal, rotate the portals to match
			transform.rotation = !isFlipped ? flippedRotation : startRotation; // Pretend the Portal were already flipped
			bool wouldTeleportPlayersIfPortalWereFlipped = WouldTeleportPlayer; // If the Portal were flipped, would we be teleporting the player?
			transform.rotation = isFlipped ? flippedRotation : startRotation; // Restore rotation
			if (doubleSidedPortals && PortalLogicIsEnabled && !PlayerRemainsInPortal && (Time.frameCount - globalLastTeleportedFrame > GLOBAL_FRAMES_TO_WAIT_AFTER_TELEPORT) && !wouldTeleportPlayersIfPortalWereFlipped) {
				Vector3 closestPoint = ClosestPoint(playerCamera.position, true, true);
				Vector3 closestPointOtherPortal = otherPortal.ClosestPoint(playerCamera.position, true, true);
				
				Vector3 portalToPlayer = playerCamera.position - closestPoint;
				Vector3 otherPortalToPlayer = playerCamera.position - closestPointOtherPortal;
				
				if (portalToPlayer.magnitude <= otherPortalToPlayer.magnitude) {
					Vector3 intoPortal = IntoPortalVector;
					Vector3 outOfPortal = -intoPortal;
					float outDot = Vector3.Dot(outOfPortal, portalToPlayer);
					float inDot = Vector3.Dot(intoPortal, portalToPlayer);
					bool playerIsOnOtherSide = outDot < inDot;
					if (playerIsOnOtherSide && !PlayerRemainsInPortal) {
						debug.LogWarning($"Out: {outDot}, In: {inDot}, portalToPlayer: {portalToPlayer:F3}");
						FlipPortal();
						if (transform != otherPortal.transform) {
							otherPortal.FlipPortal();
						}
					}
				}
			}

			// This is a low-frame rate edge case bugfix to check the player's trajectory to see if it passed through the portal
			// If it did, we need to teleport them (even if they didn't end up in the trigger zone on any frame)
			if (ShouldRaycastTeleportPlayer()) {
				debug.Log("Teleporting player due to Raycast check! Usually this implies a low frame rate.");
				TeleportPlayer(Player.instance.transform);
			}
			lastPlayerPositionProcessed = Player.instance.transform.position;
		}
        
	    public void OnPortalTriggerEnter(Collider other) {
			if (!PortalLogicIsEnabled || other.isTrigger) return;
			
			// Player teleports are handled through a CompositeMagicTrigger to make it easier to ensure they are
			// in the right position and moving the correct direction before triggering teleport
			if (other.TaggedAsPlayer()) return;
			
			// Don't deal with objects that are over the max scale allowed
			GrowShrinkObject growShrinkObject = other.gameObject.FindInParentsRecursively<GrowShrinkObject>();
			if (growShrinkObject != null && growShrinkObject.CurrentScale > MaxScaleAllowed) {
				return;
			}
			
			PortalableObject portalableObj = other.gameObject.FindInParentsRecursively<PortalableObject>();
			if (portalableObj != null && (!portalableObj.IsInPortal || portalableObj.Portal != this)) {
				portalableObj.EnterPortal(this);
			}
		}

		public void OnPortalTriggerExit(Collider other) {
			if (!PortalLogicIsEnabled || other.isTrigger) return;
			
			// Player teleports are handled through a CompositeMagicTrigger to make it easier to ensure they are
			// in the right position and moving the correct direction before triggering teleport
			if (other.TaggedAsPlayer()) return;
			
			// Don't deal with objects that are over the max scale allowed
			GrowShrinkObject growShrinkObject = other.gameObject.FindInParentsRecursively<GrowShrinkObject>();
			if (growShrinkObject != null && growShrinkObject.CurrentScale > MaxScaleAllowed) {
				return;
			}
			
			PortalableObject portalableObj = other.gameObject.FindInParentsRecursively<PortalableObject>();
			if (portalableObj != null && !portalableObj.teleportedThisFixedUpdate) {
				if (objectsInPortal.Contains(portalableObj)) {
					portalableObj.ExitPortal(this);
				}
			}
		}

		public void OnPortalTriggerStay(Collider other) {
			if (!PortalLogicIsEnabled || other.isTrigger) return;

			// Player teleports are handled through a CompositeMagicTrigger to make it easier to ensure they are
			// in the right position and moving the correct direction before triggering teleport
			if (other.TaggedAsPlayer()) return;
			
			// Don't teleport objects that are over the max scale allowed
			GrowShrinkObject growShrinkObject = other.gameObject.FindInParentsRecursively<GrowShrinkObject>();
			if (growShrinkObject != null && growShrinkObject.CurrentScale > MaxScaleAllowed) {
				return;
			}
			
			Vector3 closestPoint = ClosestPoint(other.transform.position, true, true);
			bool objectShouldBeTeleported = Mathf.Sign(Vector3.Dot(IntoPortalVector, (other.transform.position - closestPoint).normalized)) > 0;
			PortalableObject portalableObj = other.gameObject.FindInParentsRecursively<PortalableObject>();

			if (!objectShouldBeTeleported) {
				if (portalableObj != null && portalableObj.IsInPortal && portalableObj.Portal != this) {
					portalableObj.EnterPortal(this);
				}

				return;
			}
			
			if (portalableObj != null && objectsInPortal.Contains(portalableObj)) {
				debug.Log($"{ID} teleporting object {portalableObj.ID}");
				TeleportObject(portalableObj);

				// Swap state to the other portal
				// The PortalableObject itself takes care of its own state by listening for a teleport event
				objectsInPortal.Remove(portalableObj);
				otherPortal.objectsInPortal.Add(portalableObj);
			}
		}
		
		public void ApplyPortalPhysicsModeToColliders() {
			bool isEnabled = PhysicsMode is not PortalPhysicsMode.None;
			bool isTrigger = PhysicsMode is not PortalPhysicsMode.Wall;
			
			foreach (var c in colliders) {
				c.isTrigger = isTrigger;
				c.enabled = isEnabled;
			}

			foreach (var tc in triggerColliders) {
				tc.isTrigger = isTrigger;
				tc.enabled = isEnabled;
			}
		}
		
		private void CreatePortalTeleporter() {
			foreach (var c in colliders) {
				c.isTrigger = true;
			}
		}
		
		/// <summary>
		/// Does the work of TeleportObject without invoking teleport events
		/// </summary>
		/// <param name="objToTransform"></param>
		/// <param name="transformVelocity"></param>
		public void TransformObject(Transform objToTransform, bool transformVelocity = true) {
			Rigidbody objRigidbody = objToTransform.GetComponent<Rigidbody>();
			// Position & Rotation
			if (objRigidbody == null) {
				objRigidbody.MovePosition(TransformPoint(objToTransform.position));
				objRigidbody.MoveRotation(TransformRotation(objToTransform.rotation));
			}
			else {
				objToTransform.position = TransformPoint(objToTransform.position);
				objToTransform.rotation = TransformRotation(objToTransform.rotation);
			}

			// Velocity?
			if (transformVelocity && objRigidbody != null) {
				objRigidbody.velocity = TransformDirection(objRigidbody.velocity);
			}
		}

		public void TeleportObject(PortalableObject portalableObject, bool transformVelocity = true) {
			debug.Log($"Teleporting {portalableObject.FullPath()}");
			
			TriggerEventsBeforeTeleport(portalableObject.colliders[0]);

			TransformObject(portalableObject.transform, transformVelocity);
			
			TriggerEventsAfterTeleport(portalableObject.colliders[0]);
		}
        
		public void TeleportPlayer(Transform player) {
			StartCoroutine(Co_TeleportPlayer(player));
		}
		
		/// <summary>
		/// Frame 1: Teleport player and disable this portal's volumetric portal while enabling the otherPortal's volumetric portal
		/// Frame 2: Do nothing (but ensure that this is not called twice)
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		IEnumerator Co_TeleportPlayer(Transform player) {
			if (Time.frameCount - globalLastTeleportedFrame < GLOBAL_FRAMES_TO_WAIT_AFTER_TELEPORT) {
				debug.LogError("Can't teleport so quickly after last teleport!");
				yield break;
			}

			if (!PortalLogicIsEnabled) yield break;
			
			teleportingPlayer = true;

			//if (DEBUG) Debug.Break();
			TriggerEventsBeforeTeleport(player.GetComponent<Collider>());
			
			debug.Log("Teleporting player!");

			// Position
			player.position = TransformPoint(player.position);

			// Rotation
			player.rotation = TransformRotation(player.rotation);

			// Velocity
			Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
			playerRigidbody.velocity = TransformDirection(playerRigidbody.velocity);

			Physics.gravity = Physics.gravity.magnitude * -player.up;

			if (changeCameraEdgeDetection) {
				SwapEdgeDetectionColors();
			}

			if (changeActiveSceneOnTeleport) {
				// TODO: Investigate crash that happens when switching scenes rapidly without the following check
				LevelManager.instance.SwitchActiveScene(otherPortal.Level);
				// TODO: Can't reproduce the crash anymore, but if it starts happening, uncomment the following lines
				// if (LevelManager.instance.IsCurrentlySwitchingScenes) {
				// 	Debug.LogError($"Tried to switch scenes due to {ID} teleport but LevelManager is still loading!");
				// }
				// else {
				// 	LevelManager.instance.SwitchActiveScene(otherPortal.gameObject.scene.name);
				// }
			}

			// If the out portal is also a PillarDimensionObject, update the active pillar's curDimension to match the out portal's Dimension
			if (otherPortal.pillarDimensionObject) {
				DimensionPillar activePillar = otherPortal.pillarDimensionObject.activePillar;
				if (activePillar != null) {
					// Need to recalculate the camQuadrant of the PillarDimensionObject after teleporting the player
					var originalQuadrant = otherPortal.pillarDimensionObject.camQuadrant;
					otherPortal.pillarDimensionObject.DetermineQuadrantForPlayerCam();
					activePillar.curBaseDimension = otherPortal.pillarDimensionObject.Dimension;
					activePillar.dimensionWall.UpdateStateForCamera(Cam.Player, activePillar.dimensionWall.RadsOffsetForDimensionWall(Cam.Player.CamPos()));
					otherPortal.pillarDimensionObject.camQuadrant = originalQuadrant;
				}
			}

			TriggerEventsAfterTeleport(player.GetComponent<Collider>());
			// Replacing with delayed disabling of VP
			// DisableVolumetricPortal();
			otherPortal.EnableVolumetricPortal();
			if (otherPortal.turnOnNormalPortalRenderingWhenPlayerTeleports) {
				otherPortal.SetPortalModes(PortalRenderMode.Normal, PortalPhysicsMode.Normal);
			}
			yield return null;
			// Sometimes teleporting leaves the hasTriggeredOnStay state as true so we explicitly reset state here
			trigger.ResetHasTriggeredOnStayState();

			globalLastTeleportedFrame = Time.frameCount;
			lastTeleportedTime = Time.time;
			
			teleportingPlayer = false;
		}
		
	    /// <summary>
		/// There exists an edge case bug with Portals with significant frame lag at the right time (such as an autosave).
		/// The player can pass through the portal trigger zone without being teleported, potentially leading to OOB or softlocks.
		///	We need to track player's position, draw a line between position last frame and position this frame, and if it intersects the trigger zone, teleport the player.
		/// </summary>
		/// <returns>True if we should teleport the player due to this check.</returns>
		private bool ShouldRaycastTeleportPlayer() {
			// Don't teleport the player if they were just teleported (the last frame position could be mighty different from the current position on the frame that happens)
			if (Time.frameCount - globalLastTeleportedFrame < 2) return false;
			
			Vector3 start = lastPlayerPositionProcessed;
			Vector3 end = Player.instance.transform.position;
			float distance = Vector3.Distance(start, end);
			// If the player moved more than thrice their velocity in one frame, they probably didn't actually move that far
			// They were probably teleported, or we haven't processed their position in a while
			if (distance > 3 * Player.instance.movement.CurVelocity.magnitude * Time.fixedDeltaTime) return false;

			float startDot = Vector3.Dot(start - transform.position, IntoPortalVector);
			float endDot = Vector3.Dot(end - transform.position, IntoPortalVector);
			bool crossesPortalPlane = startDot * endDot < 0; // The two dot products will be different signs if the line crosses the portal plane
			if (!crossesPortalPlane) return false;
			
			Ray unitRay = new Ray(start, (end - start).normalized);
			debug.Log($"Crosses Portal Plane: {crossesPortalPlane}\nLast Frame Pos: {start}\nThis Frame Pos: {end}\nPortal Plane Pos: {transform.position}");
			debug.Log($"Crosses Portal Plane was true, the distance between last frame {start} and this frame {end} is {distance}");
			
			bool DoesLineIntersectTriggerZone(Collider triggerCollider) {
				// Use OverlapCapsule to approximate the trajectory as a capsule
				Vector3 capsuleStart = start;
				Vector3 capsuleEnd = end;
				float radius = Player.instance.CapsuleCollider.radius;

				Collider[] hits = Physics.OverlapCapsule(capsuleStart, capsuleEnd, radius, 1 << SuperspectivePhysics.TriggerZoneLayer, QueryTriggerInteraction.Collide);
				debug.Log($"Number of hits: {hits.Length}\nTriggerColliders:\n{string.Join("\n", triggerColliders.Select(tc => $"{tc.name}: {LayerMask.LayerToName(tc.gameObject.layer)}"))}");
				return hits.Contains(triggerCollider);
			}


			return triggerColliders.Any(DoesLineIntersectTriggerZone);
		}
	    
	    private void CreateCompositeTrigger() {
		    if (trigger == null) {
			    debug.Log("No trigger set, getting or adding a CompositeMagicTrigger");
			    // A CompositeMagicTrigger handles player passing through portals
			    trigger = gameObject.GetOrAddComponent<CompositeMagicTrigger>();
			    TriggerCondition positionCondition = new PlayerInDirectionFromPointCondition() {
				    useLocalCoordinates = true,
				    targetDirection = Vector3.forward,
				    targetPosition = Vector3.zero,
				    triggerThreshold = 0f
			    };
			    TriggerCondition movementCondition = new PlayerMovingDirectionCondition() {
				    useLocalCoordinates = true,
				    targetDirection = Vector3.forward,
				    triggerThreshold = 0f
			    };
			    trigger.triggerConditionsNew = new List<TriggerCondition>() {
				    positionCondition,
				    movementCondition
			    };
		    }
	    }

	    private void InitializeCompositeTrigger() {
		    trigger.colliders = triggerColliders;
		    trigger.OnMagicTriggerStayOneTime += () => {
			    playerCameraFollow.SetLerpSpeed(CameraFollow.desiredLerpSpeed);
			    if (!teleportingPlayer) {
				    TeleportPlayer(Player.instance.transform);
			    }
		    };
	    }
	    
	    private void SetScaleFactor() {
		    if (changeScale) {
			    // In Edit mode, update the other portal's scale factor to be the inverse of this one
			    if (!Application.isPlaying) {
				    Portal other = otherPortal;
#if UNITY_EDITOR
				    other = GetOtherPortal(this);
#endif
				    if (other != null) {
					    other.changeScale = changeScale;
					    other.scaleFactor = 1f / scaleFactor;
				    }
				    else {
					    Debug.LogWarning("No other portal to change scale of");
				    }
			    }
		    }
		    else {
			    scaleFactor = 1;
		    }
	    }
    }
}