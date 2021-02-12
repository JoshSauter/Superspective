using System;
using System.Collections;
using EpitaphUtils;
using NaughtyAttributes;
using UnityEngine;

namespace Saving {
    public abstract class SaveableObject : MonoBehaviour { }

    /// <summary>
    /// A SaveableObject has a unique identifier, and methods for getting, saving, and loading some Serializable save class S
    /// Example declaration: PlayerLook : SaveableObject<PlayerLook, PlayerLookSave>
    /// </summary>
    /// <typeparam name="T">Type of class whose state should be saved</typeparam>
    /// <typeparam name="S">Type of the serializable Save class</typeparam>
    public abstract class SaveableObject<T, S> : SaveableObject, ISaveableObject
        where T : SaveableObject, ISaveableObject
        where S : SerializableSaveObject<T> {
        
        [SerializeField]
        protected bool DEBUG = false;

        DebugLogger _debug;

        public DebugLogger debug => _debug ??= new DebugLogger(gameObject, () => false);

        string SceneName => (this != null && gameObject.scene != null) ? gameObject.scene.name : "";

        protected bool hasInitialized = false;
        bool hasRegistered = false;
        
        [Button("Copy SceneName to clipboard")]
        public void CopySceneName() {
            GUIUtility.systemCopyBuffer = SceneName;
        }
        
        [Button("Copy ID to clipboard")]
        public void CopyUniqueId() {
            GUIUtility.systemCopyBuffer = ID;
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
            LevelManager.instance.BeforeSceneSaveState += RegisterOnLevelChangeEvents;
            LevelManager.instance.AfterSceneRestoreState += InitAfterSceneRestoreState;
        }

        protected virtual void Start() {
            // We register on Start as well because RegisterOnLevelChangeEvents won't fire when the game is first started
            // This call is idempotent so it should not matter if it is called after already registering
            Register();
            StartCoroutine(InitCoroutine());
        }

        IEnumerator InitCoroutine() {
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
        
        /// <summary>
        /// Init is similar to Awake() and Start(), but it is only called after the LevelManager is done loading scenes
        /// Or, on SceneManagerForScene.AfterSceneRestoreState, whichever one happens first
        /// </summary>
        protected virtual void Init() { }

        void RegisterOnLevelChangeEvents(string scene) {
            if (hasRegistered) return;
            
            if (scene == SceneName) {
                Register();
                LevelManager.instance.BeforeSceneRestoreState -= RegisterOnLevelChangeEvents;
                LevelManager.instance.BeforeSceneSaveState -= RegisterOnLevelChangeEvents;
            }
        }

        // Registration is triggered off an event from SaveManagerForScene that happens after Awake but before Start
        // This allows modifications to IDs (such as with MultiDimensionCube) to take effect before registration
        public void Register() {
            if (hasRegistered || string.IsNullOrEmpty(SceneName) || !Application.isPlaying) {
                return;
            }
            
            if (!(SaveManager.GetSaveManagerForScene(SceneName)?.RegisterSaveableObject(this) ?? false)) {
                debug.LogError($"Failed to register SaveableObject in scene {SceneName}, destroying self.");
                Destroy(gameObject);
            }
            else {
                debug.Log($"Registered SaveableObject with id {ID} in scene {SceneName}");
                hasRegistered = true;
            }
        }

        protected virtual void OnDestroy() {
            if (LevelManager.instance != null) {
                LevelManager.instance.BeforeSceneRestoreState -= RegisterOnLevelChangeEvents;
                LevelManager.instance.BeforeSceneSaveState -= RegisterOnLevelChangeEvents;
                LevelManager.instance.AfterSceneRestoreState -= InitAfterSceneRestoreState;
            }
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return;
            }
#endif
            if (!LevelManager.instance.loadedSceneNames.Contains(gameObject.scene.name)) {
                return;
            }
            
            if (!(SaveManager.GetSaveManagerForScene(SceneName, false)?.UnregisterSaveableObject(this) ?? true)) {
                debug.LogError($"Failed to unregister DynamicObject upon OnDestroy, state may be incorrect for SaveManagerForScene: {SceneName}");
            }
        }

        public abstract string ID { get; }

        // GetSaveObject and LoadFromSavedObject are virtual so they can be overridden to target inherited save objects instead of base save objects
        public virtual object GetSaveObject() {
            return (S)Activator.CreateInstance(typeof(S), new object[] { this });
        }

        public virtual void RestoreStateFromSave(object savedObject) {
            S save = savedObject as S;

            save?.LoadSave(this as T);
        }

        public virtual bool SkipSave { get; set; }
    }
}