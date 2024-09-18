using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using SuperspectiveUtils;
using LevelManagement;
using NaughtyAttributes;
using UnityEngine;

namespace Saving {
    public abstract class SaveableObject : MonoBehaviour, ISaveableObject {
        [SerializeField]
        protected UniqueId _id;
        protected UniqueId id {
            get {
                if (_id == null && this != null && this.gameObject != null) {
                    _id = this.GetComponent<UniqueId>();
                }
                return _id;
            }
        }
        public virtual string ID {
            get {
                if (id == null || string.IsNullOrEmpty(id.uniqueId)) {
                    throw new Exception($"{gameObject.name}.{GetType().Name} in {gameObject.scene.name} doesn't have a uniqueId set");
                }
                

                string suffix = id != null ? $"_{id.uniqueId}" : "";
                return $"{GetType().Name}{suffix}";
            }
        }

        public virtual SerializableSaveObject GetSaveObject() {
            return new SerializableSaveObject(this);
        }

        public virtual void RestoreStateFromSave(SerializableSaveObject savedObject) {}
        
        // Anything that doesn't specify types for associated SerializableSaveObject shouldn't have anything to actually save to disk
        // Parameterized version SaveableObject<T, S> overrides this to be false
        public virtual bool SkipSave {
            get => true;
            set { }
        }
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
        
        public bool DEBUG = false;
        DebugLogger _debug;
        public DebugLogger debug => _debug ??= new DebugLogger(gameObject, () => DEBUG);

        // Registration is triggered off an event from SaveManagerForScene that happens after Awake but before Start
        // This allows modifications to IDs (such as with MultiDimensionCube) to take effect before registration
        public virtual void Register() {
            if (hasRegistered || string.IsNullOrEmpty(SceneName) || !Application.isPlaying) {
                return;
            }
            
            if (!(SaveManager.GetOrCreateSaveManagerForScene(SceneName)?.RegisterSaveableObject(this) ?? false)) {
                debug.LogError($"Failed to register SaveableObject in scene {SceneName}, destroying self.", true);
                Destroy(gameObject);
            }
            else {
                debug.Log($"Registered SaveableObject with id {ID} in scene {SceneName}");
                hasRegistered = true;
            }
        }

        // Unregister can be explicitly called to tell the SaveManager we shouldn't attempt to save this
        public virtual void Unregister() {
            if (hasRegistered && !string.IsNullOrEmpty(SceneName) && Application.isPlaying) {
                SaveManager.GetOrCreateSaveManagerForScene(SceneName)?.UnregisterSaveableObject(ID);
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
            yield return new WaitUntil(() => GameManager.instance?.gameHasLoaded ?? false);
            DoInitIdempotent();
        }

        void InitAfterSceneRestoreState(string sceneName) {
            if (sceneName == SceneName) {
                DoInitIdempotent();

                LevelManager.instance.AfterSceneRestoreState -= InitAfterSceneRestoreState;
            }
        }

        private void DoInitIdempotent() {
            if (!hasInitialized) {
                Init();
                hasInitialized = true;
            }
        }

        protected virtual void OnEnable() {
            StartCoroutine(InitCoroutine());
        }

        /// <summary>
        /// Register this SaveableObject with the SaveManagerForScene when the scene changes, then stop listening for scene changes
        /// </summary>
        /// <param name="scene">Scene switched to, compared against this.SceneName to know when to trigger</param>
        void RegisterOnLevelChangeEvents(string scene) {
            if (hasRegistered) return;
            
            if (scene == SceneName) {
                Register();
                // Only do this once
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

        protected virtual void OnValidate() {
            if (id == null) {
                // If T has a RequireComponent attribute for UniqueId, add it if it doesn't exist
                var requireUniqueIdAttribute = typeof(T).GetCustomAttributes(typeof(RequireComponent), true).Select(att => att as RequireComponent);
                var uniqueIdType = typeof(UniqueId);
                bool requiresUniqueId = requireUniqueIdAttribute.Any(att => att?.m_Type0 == uniqueIdType || att?.m_Type1 == uniqueIdType || att?.m_Type2 == uniqueIdType);
                if (requiresUniqueId) {
                    _id = gameObject.GetOrAddComponent<UniqueId>();
                }
            }
        }
        
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