using System;
using System.Collections;
using EpitaphUtils;
using LevelManagement;
using NaughtyAttributes;
using UnityEngine;

namespace Saving {
    public abstract class SaveableObject : MonoBehaviour, ISaveableObject {
        public abstract string ID { get; }
        public abstract SerializableSaveObject GetSaveObject();
        public abstract void RestoreStateFromSave(SerializableSaveObject savedObject);
        public abstract bool SkipSave { get; set; }
        /// <summary>
        /// Init is similar to Awake() and Start(), but it is only called after the LevelManager is done loading scenes
        /// Or, on SceneManagerForScene.AfterSceneRestoreState, whichever one happens first
        /// </summary>
        protected virtual void Init() { }

        [ContextMenu("Copy SceneName to clipboard")]
        [Button("Copy SceneName to clipboard")]
        public void CopySceneName() {
            GUIUtility.systemCopyBuffer = SceneName;
        }
        
        [ContextMenu("Copy ID to clipboard")]
        [Button("Copy ID to clipboard")]
        public void CopyUniqueId() {
            GUIUtility.systemCopyBuffer = ID;
        }
        
        string SceneName => (this != null && gameObject.scene != null) ? gameObject.scene.name : "";
        
        protected bool hasInitialized = false;
        bool hasRegistered = false;
        
        [SerializeField]
        protected bool DEBUG = false;
        DebugLogger _debug;
        public DebugLogger debug => _debug ??= new DebugLogger(gameObject, () => DEBUG);

        // Registration is triggered off an event from SaveManagerForScene that happens after Awake but before Start
        // This allows modifications to IDs (such as with MultiDimensionCube) to take effect before registration
        public void Register() {
            if (hasRegistered || string.IsNullOrEmpty(SceneName) || !Application.isPlaying) {
                return;
            }
            
            if (!(SaveManager.GetOrCreateSaveManagerForScene(SceneName)?.RegisterSaveableObject(this) ?? false)) {
                debug.LogError($"Failed to register SaveableObject in scene {SceneName}, destroying self.");
                Destroy(gameObject);
            }
            else {
                debug.Log($"Registered SaveableObject with id {ID} in scene {SceneName}");
                hasRegistered = true;
            }
        }
        
        protected virtual void Awake() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return;
            }
#endif
            if (LevelManager.instance == null ||
                !(LevelManager.instance.loadedSceneNames.Contains(gameObject.scene.name) ||
                  LevelManager.instance.currentlyLoadingSceneNames.Contains(gameObject.scene.name))) {
                return;
            }

            LevelManager.instance.BeforeSceneRestoreState += RegisterOnLevelChangeEvents;
            LevelManager.instance.BeforeSceneSerializeState += RegisterOnLevelChangeEvents;
            LevelManager.instance.AfterSceneRestoreState += InitAfterSceneRestoreState;
        }

        protected virtual void Start() {
            StartCoroutine(RegisterOnceGameIsLoaded());
            StartCoroutine(InitCoroutine());
        }

        IEnumerator RegisterOnceGameIsLoaded() {
            yield return new WaitUntil(() => GameManager.instance.gameHasLoaded);
            
            if (gameObject == null || !gameObject.IsInLoadedScene()) yield break;
            
            // We register here as well because RegisterOnLevelChangeEvents won't fire when the game is first started
            // This call is idempotent so it should not matter if it is called after already registering
            Register();
        }

        IEnumerator InitCoroutine() {
            yield return new WaitUntil(() => GameManager.instance.gameHasLoaded);
            yield return new WaitWhile(() => LevelManager.instance.IsCurrentlyLoadingScenes);
            if (!hasInitialized) {
                Init();
                hasInitialized = true;
            }
        }

        void InitAfterSceneRestoreState(string sceneName) {
            if (sceneName == SceneName) {
                if (!hasInitialized) {
                    Init();
                    hasInitialized = true;
                }

                LevelManager.instance.AfterSceneRestoreState -= InitAfterSceneRestoreState;
            }
        }

        void RegisterOnLevelChangeEvents(string scene) {
            if (hasRegistered) return;
            
            if (scene == SceneName) {
                Register();
                LevelManager.instance.BeforeSceneRestoreState -= RegisterOnLevelChangeEvents;
                LevelManager.instance.BeforeSceneSerializeState -= RegisterOnLevelChangeEvents;
            }
        }

        protected virtual void OnDestroy() {
            if (LevelManager.instance != null) {
                LevelManager.instance.BeforeSceneRestoreState -= RegisterOnLevelChangeEvents;
                LevelManager.instance.BeforeSceneSerializeState -= RegisterOnLevelChangeEvents;
                LevelManager.instance.AfterSceneRestoreState -= InitAfterSceneRestoreState;
            }
        }
    }

    /// <summary>
    /// A SaveableObject has a unique identifier, and methods for getting, saving, and loading some Serializable save class S
    /// Example declaration: PlayerLook : SaveableObject<PlayerLook, PlayerLookSave>
    /// </summary>
    /// <typeparam name="T">Type of class whose state should be saved</typeparam>
    /// <typeparam name="S">Type of the serializable Save class</typeparam>
    public abstract class SaveableObject<T, S> : SaveableObject
        where T : SaveableObject
        where S : SerializableSaveObject<T> {
        // GetSaveObject and LoadFromSavedObject are virtual so they can be overridden to target inherited save objects instead of base save objects
        public override SerializableSaveObject GetSaveObject() {
            return (S)Activator.CreateInstance(typeof(S), new object[] { this });
        }

        public override void RestoreStateFromSave(SerializableSaveObject savedObject) {
            S save = savedObject as S;

            save?.LoadSave(this as T);
        }

        public override bool SkipSave { get; set; }
    }
}