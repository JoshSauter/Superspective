using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using GrowShrink;
using MagicTriggerMechanics;
using PortalMechanics;
using UnityEngine;
using Saving;
using SerializableClasses;
using StateUtils;
using SuperspectiveUtils;

namespace TheEntity {
    [RequireComponent(typeof(UniqueId))]
    public class TheEntity_GrowShrinkIntro : SingletonSuperspectiveObject<TheEntity_GrowShrinkIntro, TheEntity_GrowShrinkIntro.TheEntity_GrowShrinkIntroSave>, AudioJobOnGameObject {
        [SerializeField]
        private SuperspectiveReference<Portal, Portal.PortalSave> infFallingPortal;
        private float InfFallingRepeatDistance => infFallingPortal.Reference.Match(
            p => Mathf.Abs(p.transform.position.y - p.otherPortal.transform.position.y),
            _ => 0f);
        
        [SerializeField]
        private Transform eyeTransform;
        public Transform particleSystemTransform;
        
        private Collider _collider;
        private Collider Collider => _collider ??= GetComponent<Collider>();

        [SerializeField]
        private GameObject rendererRoot;

        public GameObject invisWall;
        
        public GlobalMagicTrigger despawnMagicTrigger;

        [SerializeField]
        private Transform staticWall;
        private float staticWallStartX;
        private const float STATIC_WALL_END_X = 865.5f;

        private GrowShrinkObject _growShrinkObject;
        private GrowShrinkObject GrowShrinkObject => _growShrinkObject ??= this.GetOrAddComponent<GrowShrinkObject>();
        
        private PortalableObject _portalableObject;
        private PortalableObject PortalableObject => _portalableObject ??= this.GetOrAddComponent<PortalableObject>();
        
        private NoiseScrambleOverlayObject _noiseScrambleOverlayObject;
        private NoiseScrambleOverlayObject NoiseScrambleOverlayObject => _noiseScrambleOverlayObject ??= this.GetOrAddComponent<NoiseScrambleOverlayObject>();

        private float Scale => GrowShrinkObject.CurrentScale;

        public Vector3 desiredPos;
        public NodeSystem nodeSystem;
        public TriggerOverlapZone triggerZoneSetPath;

        public enum State {
            NotSpawned,
            Following,
            GoingToSetPathStart,
            SetPath,
            Watching
        }

        public StateMachine<State> state;
        
        // SetPath state
        float distanceTraveled = 0;
        int currentIndex = 0;

        public enum ResetPlayerState : byte {
            OutOfRange,
            WithinRange
        }
        public StateMachine<ResetPlayerState> resetPlayerState;

        private const string LEVEL_CHANGE_BANNER_KEY = "ItFollows_GrowShrink";
        private const float WATCHING_DESPAWN_DELAY = 5f;
        private const float RESET_PLAYER_DISTANCE = 9f;
        private const float RESET_PLAYER_TIME = 1f;
        private const float MAX_VERTICAL_LOOK_ANGLE = 75f;
        private const float HEIGHT_OFF_GROUND = 1f;
        private const float MOVE_SPEED = 5f;
        private const float CLOSE_ENOUGH_DISTANCE = .125f; // To transition from GoingToSetPath -> SetPath, SetPath -> Watching
        private const float PLAYER_LOOKING_AT_ENTITY_THRESHOLD = 0.75f;
        private const float PLAYER_LOOKING_AT_ENTITY_MAX_DISTANCE = 125f;

        private float ResetPlayerDistance => RESET_PLAYER_DISTANCE * Player.instance.Scale;
        private float HeightOffGround => HEIGHT_OFF_GROUND * Scale;
        private float MoveSpeed => MOVE_SPEED * Scale;
        private float CloseEnoughDistance => CLOSE_ENOUGH_DISTANCE * Scale;

        private List<NodeTrailInfo> setPathTrailInfo;

        private AudioManager.AudioJob audio;

        private Vector3 startPos;

        private bool hasPlayedBanner = false;

        protected override void Awake() {
            base.Awake();

            startPos = transform.position;
            InitializeStateMachine();
        }

        protected override void Start() {
            base.Start();

            setPathTrailInfo = nodeSystem.GenerateTrailInfo();
            startPos = transform.position;
            staticWallStartX = staticWall.position.x;
        }

        protected override void Init() {
            base.Init();
            
            state.Set(State.NotSpawned);
            MiniatureMaze.instance.state.AddTrigger(MiniatureMaze.State.MazeSolved, () => state.Set(State.Following));

            StartCoroutine(TheEntity.BlinkController(this, eyeTransform));
        }

        private void PlayBanner() {
            if (hasPlayedBanner) return;

            LevelChangeBanner.instance.PlayBanner(LEVEL_CHANGE_BANNER_KEY);
            hasPlayedBanner = true;
        }

        private void InitializeStateMachine() {
            // Reset the maze if the player gets too close to the entity
            resetPlayerState = this.StateMachine(ResetPlayerState.OutOfRange);
            resetPlayerState.AddStateTransition(ResetPlayerState.OutOfRange, ResetPlayerState.WithinRange,
                () => state != State.NotSpawned && SuperspectivePhysics.ShortestDistance(Player.instance.transform.position, transform.position) < ResetPlayerDistance);
            resetPlayerState.AddStateTransition(ResetPlayerState.WithinRange, ResetPlayerState.OutOfRange,
                () => state == State.NotSpawned || SuperspectivePhysics.ShortestDistance(Player.instance.transform.position, transform.position) >= ResetPlayerDistance);
            resetPlayerState.AddTrigger(ResetPlayerState.WithinRange, RESET_PLAYER_TIME, () => {
                NoiseScrambleOverlay.instance.SetNoiseScrambleOverlayValue($"{ID}_EntityWithinRange", 1);
                MiniatureMaze.instance.state.Set(MiniatureMaze.State.ResettingMaze);
                state.Set(State.NotSpawned);
            });
            
            state = this.StateMachine(State.Following);
            
            // Disable collider and renderer when not spawned
            state.OnStateChangeSimple += SetActive;
            
            // Play the banner the first time it starts following
            state.AddTrigger(State.Following, PlayBanner);
            
            // Transition from following to watching when we reach the end of the path
            state.AddStateTransition(State.SetPath, State.Watching, () => SuperspectivePhysics.ShortestDistance(transform.position, nodeSystem.WorldPos(nodeSystem.allNodes.Last())) < CloseEnoughDistance);
            // Despawn when we reach the end of the path and after some time has passed, and when the player is not looking
            state.AddTrigger(State.Watching,
                () => state.Time >= WATCHING_DESPAWN_DELAY && SuperspectivePhysics.ShortestDistance(Player.instance.PlayerCam.transform.position, transform.position) > NoiseScrambleOverlay.MAX_VOLUME_DISTANCE,
                () => despawnMagicTrigger.enabled = true
            );
            
            state.AddStateTransition(State.Following, State.GoingToSetPathStart, () => triggerZoneSetPath.playerInZone);
            state.AddStateTransition(State.GoingToSetPathStart, State.SetPath, () => SuperspectivePhysics.ShortestDistance(transform.position, nodeSystem.WorldPos(nodeSystem.allNodes.First())) < CloseEnoughDistance);
            
            state.WithUpdate(State.Following, _ => {
                // Find the shortest path to the player
                Vector3 direction = SuperspectivePhysics.ShortestVectorPointToPoint(transform.position, Player.instance.transform.position).normalized;
                
                Vector3 desiredPos = transform.position + direction * (MoveSpeed * Time.deltaTime);
                
                Ray raycastRay = new Ray(desiredPos, -transform.up);
                int prevColliderLayer = Collider.gameObject.layer;
                Collider.gameObject.layer = SuperspectivePhysics.IgnoreRaycastLayer;
                if (Physics.Raycast(raycastRay, out RaycastHit hit, HeightOffGround)) {
                    desiredPos = hit.point + HeightOffGround * transform.up;
                }
                Collider.gameObject.layer = prevColliderLayer;
                
                // Move towards the player
                transform.position = Vector3.MoveTowards(transform.position, desiredPos, MoveSpeed * Time.deltaTime);
            });
            
            // Go to the starting point of the path
            state.AddTrigger(State.GoingToSetPathStart, () => Collider.enabled = false);
            state.WithUpdate(State.GoingToSetPathStart, _ => {
                Vector3 direction = SuperspectivePhysics.ShortestVectorPointToPoint(transform.position, nodeSystem.WorldPos(nodeSystem.allNodes.First())).normalized;
                
                LookAtPlayer();
                
                Vector3 desiredPos = transform.position + direction * (MoveSpeed * Time.deltaTime);
                
                // Move towards the player
                transform.position = Vector3.MoveTowards(transform.position, desiredPos, MoveSpeed * Time.deltaTime);
            });
            
            // Follow a set path to the jumping off platform
            state.WithUpdate(State.SetPath, _ => {
                distanceTraveled += Time.deltaTime * MoveSpeed;

                NodeTrailInfo CurNodeInfo() => setPathTrailInfo[currentIndex];
                while (distanceTraveled > CurNodeInfo().endDistance && currentIndex < setPathTrailInfo.Count - 1) {
                    currentIndex++;
                }
                
                // Simulate the entity going through the grow/shrink portal
                if (CurNodeInfo().parent?.parent?.zeroDistanceToChildren ?? false) {
                    // Hardcode scale for end of the path (after the portal/zero distance to children node)
                    GrowShrinkObject.SetScaleDirectly(2f);
                }
                
                float t = Mathf.InverseLerp(CurNodeInfo().startDistance, CurNodeInfo().endDistance, distanceTraveled);
                desiredPos = Vector3.Lerp(nodeSystem.WorldPos(CurNodeInfo().parent), nodeSystem.WorldPos(CurNodeInfo().thisNode), t);

                debug.Log($"Desired Position: {desiredPos}");

                transform.position = desiredPos; // Vector3.MoveTowards(transform.position, desiredPos, MoveSpeed * Time.deltaTime);
            });
            
            // Reset state when not spawned
            state.AddTrigger(State.NotSpawned, () => {
                distanceTraveled = 0;
                currentIndex = 0;
                GrowShrinkObject.SetScaleDirectly(GrowShrinkObject.startingScale);
                transform.position = startPos;
                staticWall.position = staticWall.position.WithX(staticWallStartX);
            });
            
            state.WithUpdate(_ => {
                if (!this.IsInActiveScene()) return;
                
                MoveStaticWall();
                LookAtPlayer();

                if (NoiseScrambleOverlayObject.scramblerState == NoiseScrambleOverlayObject.ScramblerState.Off) return;
                
                // If the player is looking at the entity, set the noise scramble overlay value to some value between 0 and 1
                Transform playerCam = Player.instance.PlayerCam.transform;
                Vector3 shortestVectorPlayerToEntity = SuperspectivePhysics.ShortestVectorPointToPoint(playerCam.position, transform.position);
                float playerLookingAtEntity = Vector3.Dot(Player.instance.PlayerCam.transform.forward, shortestVectorPlayerToEntity.normalized);
                if (playerLookingAtEntity > PLAYER_LOOKING_AT_ENTITY_THRESHOLD) {
                    float t = (playerLookingAtEntity - PLAYER_LOOKING_AT_ENTITY_THRESHOLD) / (1 - PLAYER_LOOKING_AT_ENTITY_THRESHOLD);
                    
                    float distanceMultiplier = Mathf.InverseLerp(PLAYER_LOOKING_AT_ENTITY_MAX_DISTANCE, 0, shortestVectorPlayerToEntity.magnitude / Player.instance.Scale);
                    debug.Log($"PlayerLookingAtEntity: {playerLookingAtEntity}, t: {t}, distanceMultiplier: {distanceMultiplier}");
                    NoiseScrambleOverlay.instance.SetNoiseScrambleOverlayValue($"{ID}_PlayerLookAtEntity", Mathf.Clamp01(t * distanceMultiplier));
                }
            });
        }

        private void MoveStaticWall() {
            staticWall.position = staticWall.position.WithX(Mathf.Clamp(transform.position.x,STATIC_WALL_END_X, staticWallStartX));

            // float t = Mathf.InverseLerp(staticWallStartX, STATIC_WALL_END_X, staticWall.position.x);
            // float xzScale = Mathf.Lerp(1, 0.1f, t);
            //
            // staticWall.localScale = staticWall.localScale.WithX(xzScale);
        }

        // Called by GlobalMagicTrigger's unity event
        public void Despawn() {
            state.Set(State.NotSpawned);
        }

        private void SetActive() {
            bool active = state != State.NotSpawned;
            Collider.enabled = active;
            invisWall.SetActive(active);
            rendererRoot.SetActive(active);
            particleSystemTransform.gameObject.SetActive(active);
            staticWall.gameObject.SetActive(active);
            NoiseScrambleOverlayObject.enabled = active;
            if (active) {
                NoiseScrambleOverlayObject.TurnOn();
                audio = AudioManager.instance.PlayOnGameObject(AudioName.EmptyVoid_8152358, ID, this, false, settings => {
                    settings.audio.spatialBlend = 1;
                });
            }
            else {
                NoiseScrambleOverlayObject.TurnOff();
                audio?.Stop();
            }
        }

        private void LookAtPlayer() {
            if (GameManager.instance.IsCurrentlyLoading) return;

            // If the player is this amount underneath the entity, the entity should look up through the portal at the player
            const float PLAYER_SHOULD_BE_CONSIDERED_ABOVE_THRESHOLD = 20f;
            Vector3 playerPos = Player.instance.transform.position;

            // As soon as the player falls past the entity, it should look up at the player through the portal
            // Otherwise the player has to pass through the portal before it will look up, and it will snap upwards
            if (playerPos.y < transform.position.y - PLAYER_SHOULD_BE_CONSIDERED_ABOVE_THRESHOLD) {
                playerPos += Vector3.up * InfFallingRepeatDistance;
            }

            Vector3 offset = state == State.SetPath ? SuperspectivePhysics.ShortestVectorPointToPoint(transform.position, playerPos) : playerPos - transform.position;
            
            LookAtPosition(transform.position + offset);
        }

        private void LookAtPosition(Vector3 pos) {
            // Helper function to clamp angles correctly
            float ClampAngle(float angle, float min, float max) {
                if (angle > 180f)
                    angle -= 360f;
                return Mathf.Clamp(angle, min, max);
            }
            
            // Create a rotation that looks at the player
            transform.LookAt(pos);

            // Clamp the pitch (vertical rotation) to the specified range
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.WithX(ClampAngle(transform.rotation.eulerAngles.x, -MAX_VERTICAL_LOOK_ANGLE, MAX_VERTICAL_LOOK_ANGLE)));
        }

        void Update() {
            if (GameManager.instance.IsCurrentlyLoading) return;
            
            // For some reason the particle system doesn't scale with the object, so we have to do it manually
            particleSystemTransform.localScale = Vector3.one * Scale;
        }

#region Saving

        public override void LoadSave(TheEntity_GrowShrinkIntroSave save) {
            particleSystemTransform.localScale = save.particleSystemTransformLocalScale;

            SetActive();
        }

        [Serializable]
        public class TheEntity_GrowShrinkIntroSave : SaveObject<TheEntity_GrowShrinkIntro> {
            public SerializableVector3 particleSystemTransformLocalScale;

            public TheEntity_GrowShrinkIntroSave(TheEntity_GrowShrinkIntro script) : base(script) {
                this.particleSystemTransformLocalScale = script.particleSystemTransform.localScale;
            }
        }

#endregion

        public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob audioJob) => transform;
    }
}