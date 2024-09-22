using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using GrowShrink;
using LevelManagement;
using MagicTriggerMechanics;
using PortalMechanics;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using Random = UnityEngine.Random;

namespace TheEntity {
    [RequireComponent(typeof(UniqueId))]
    public class TheEntity_GrowShrinkIntro : SingletonSaveableObject<TheEntity_GrowShrinkIntro, TheEntity_GrowShrinkIntro.TheEntity_GrowShrinkIntroSave>, AudioJobOnGameObject {
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

        public enum ResetPlayerState {
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
        private const float BLINK_TIME = .125f;
        private const float MIN_TIME_BETWEEN_BLINKS = 2.5f;
        private const float MAX_TIME_BETWEEN_BLINKS = 9f;
        private const float CLOSE_ENOUGH_DISTANCE = .125f; // To transition from GoingToSetPath -> SetPath, SetPath -> Watching

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
            state.Set(State.NotSpawned);
            MiniatureMaze.instance.state.AddTrigger(MiniatureMaze.State.MazeSolved, () => state.Set(State.Following));

            StartCoroutine(BlinkController());
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
                NoiseScrambleOverlay.instance.SetNoiseScrambleOverlayValue(1);
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

            // These variables are used in the lambda closure below to keep track of state
            float distanceTraveled = 0;
            int currentIndex = 0;
            
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
                transform.position = startPos;
                staticWall.position = staticWall.position.WithX(staticWallStartX);
            });
            
            state.WithUpdate(_ => {
                MoveStaticWall();
                LookAtPlayer();
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
            LookAtPosition(transform.position + SuperspectivePhysics.ShortestVectorPointToPoint(transform.position, Player.instance.transform.position, true));
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

        private IEnumerator BlinkController() {
            while (true) {
                StartCoroutine(BlinkCoroutine());
                
                yield return new WaitForSeconds(Random.Range(MIN_TIME_BETWEEN_BLINKS, MAX_TIME_BETWEEN_BLINKS));
            }
        }

        private IEnumerator BlinkCoroutine() {
            float time = 0;

            // Blink closed
            while (time < BLINK_TIME) {
                time += Time.deltaTime;

                eyeTransform.localScale = eyeTransform.localScale.WithY(1 - Easing.EaseInOut(time / BLINK_TIME));
                
                yield return null;
            }
            eyeTransform.localScale = eyeTransform.localScale.WithY(0);
            
            // Blink open
            time = 0f;
            while (time < BLINK_TIME) {
                time += Time.deltaTime;

                eyeTransform.localScale = eyeTransform.localScale.WithY(Easing.EaseInOut(time / BLINK_TIME));
                
                yield return null;
            }
            eyeTransform.localScale = eyeTransform.localScale.WithY(1);
        }

        void Update() {
            if (GameManager.instance.IsCurrentlyLoading) return;
            
            // For some reason the particle system doesn't scale with the object, so we have to do it manually
            particleSystemTransform.localScale = Vector3.one * Scale;
        }

#region Saving

        [Serializable]
        public class TheEntity_GrowShrinkIntroSave : SerializableSaveObject<TheEntity_GrowShrinkIntro> {
            private StateMachine<State>.StateMachineSave stateSave;

            public TheEntity_GrowShrinkIntroSave(TheEntity_GrowShrinkIntro script) : base(script) {
                this.stateSave = script.state.ToSave();
            }

            public override void LoadSave(TheEntity_GrowShrinkIntro script) {
                script.state.LoadFromSave(this.stateSave);
            }
        }

#endregion

        public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob audioJob) => transform;
    }
}