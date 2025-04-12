using System;
using System.Collections;
using System.Linq;
using SuperspectiveUtils;
using LevelManagement;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Saving {
    /// <summary>
    /// Abstract base class for SuperspectiveObjects, which are objects in the scenes that can be saved and loaded.
    /// Contrast with SaveObjects, which are serialized data objects that are saved to disk.
    /// This class handles the common ID and registration logic for all saveable objects, as well as the DebugLogger.
    /// It also handles saving and restoring the active state of the GameObject and the enabled state of itself for saving and loading.
    /// </summary>
    public class SuperspectiveObject : MonoBehaviour, ISaveableObject {
        public string SceneName => (this != null) ? gameObject.scene.name : "";
        public Levels Level => !string.IsNullOrEmpty(SceneName) ? LevelManager.enumToSceneName[SceneName] : Levels.InvalidLevel;
        
        [OnInspectorGUI(nameof(UpdateInitialStates))]
        public bool gameObjectStartsInactive = false;
        public bool scriptStartsDisabled = false;

        [HideInInspector]
        public Vector3 _startPosition;
        [HideInInspector]
        public Quaternion _startRotation;
        
        protected bool hasInitialized = false;
        protected bool hasRegistered = false;

        /// <summary>
        /// Sets the gameObjectStartsInactive and scriptStartsDisabled values to the current state of the GameObject and the script.
        /// Also sets the _startPosition and _startRotation to the current position and rotation of the GameObject.
        /// This is necessary because if a script is not part of a loaded save file (e.g. it was never enabled at the time of the save),
        /// but is loaded currently, it still needs to reset its state to the initial state even though the save file doesn't know about it.
        /// This is done by having every SuperspectiveObject hook into the BeforeLoad event of the SaveManager to reset its state.
        /// </summary>
        private void UpdateInitialStates() {
            if (Application.isPlaying) return;
            
            if (!gameObject) return;
            
            // ReSharper disable RedundantCheckBeforeAssignment
            // Disabling the warning because I don't want to dirty the scene if the values are already correct
            if (gameObjectStartsInactive != !gameObject.activeSelf) {
                gameObjectStartsInactive = !gameObject.activeSelf;
            }
            if (scriptStartsDisabled != !enabled) {
                scriptStartsDisabled = !enabled;
            }

            if (transform.position != _startPosition || transform.rotation != _startRotation) {
                _startPosition = transform.position;
                _startRotation = transform.rotation;
            }
        }
        
        public bool DEBUG = false;
        DebugLogger _debug;
        public DebugLogger debug => _debug ??= new DebugLogger(gameObject, () => DEBUG);
        
        [SerializeField]
        protected UniqueId _id;
        public UniqueId id {
            get {
                if (_id == null && this != null && this.gameObject != null) {
                    _id = this.GetComponent<UniqueId>();
                }
                return _id;
            }
            set => _id = value;
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
        
        public virtual string AssociationID {
            get {
                string lastPart = ID.Split('_').Last();
                return lastPart.IsGuid() ? lastPart : ID;
            }
        }

        /// <summary>
        /// Creates a SaveObject with the current state of the SuperspectiveObject.
        /// </summary>
        /// <returns></returns>
        public virtual SaveObject CreateSave() {
            return new SaveObject(this);
        }

        /// <summary>
        /// Restore the state of the SuperspectiveObject from a SaveObject.
        /// </summary>
        /// <param name="save">SaveObject data to restore state to.</param>
        public virtual void LoadFromSave(SaveObject save) {
            gameObject.SetActive(save.isGameObjectActive);
            enabled = save.isScriptEnabled;
            transform.position = save.position;
            transform.rotation = save.rotation;
        }

        // Anything that doesn't specify types for associated SaveObject shouldn't have anything to actually save to disk
        // unless the active or enabled state has been modified
        public virtual bool SkipSave {
            // We only want to skip the save if the active state of the GameObject and the enabled state of the script are the same as the initial state
            get => !this || (!gameObjectStartsInactive == gameObject.activeSelf && !scriptStartsDisabled == enabled);
            set { }
        }
        
        /// <summary>
        /// Init is similar to Awake() and Start(), but it is only called after the LevelManager is done loading scenes
        /// Or, on SceneManagerForScene.AfterSceneRestoreState, whichever one happens first
        /// </summary>
        protected virtual void Init() {
            SaveManager.BeforeLoad += ResetState;
        }

        [ContextMenu("Copy SceneName to clipboard")]
        [NaughtyAttributes.Button("Copy SceneName to clipboard")]
        public void CopySceneName() {
            GUIUtility.systemCopyBuffer = SceneName;
        }
        
        [ContextMenu("Copy ID to clipboard")]
        [NaughtyAttributes.Button("Copy ID to clipboard")]
        public void CopyUniqueId() {
            GUIUtility.systemCopyBuffer = ID;
        }

        /// <summary>
        /// Resets the state of the SuperspectiveObject to its initial state.
        /// Inheritors can override this method to reset their own state.
        ///
        /// This is called before a save is loaded, so that even objects which do not have an entry in the save file can be reset.
        /// </summary>
        public virtual void ResetState() {
            if (this == null || gameObject == null) return;
            
            debug.Log("ResetState for " + gameObject.name);
            gameObject.SetActive(!gameObjectStartsInactive);
            enabled = !scriptStartsDisabled;
            transform.position = _startPosition;
            transform.rotation = _startRotation;
        }

        // Registration is triggered off an event from SaveManagerForScene that happens after Awake but before Start
        // This allows modifications to IDs (such as with MultiDimensionCube) to take effect before registration
        public virtual void Register() {
            if (hasRegistered || string.IsNullOrEmpty(SceneName) || !Application.isPlaying) {
                return;
            }
            
            if (!SaveManager.Register(this)) {
                debug.LogError($"Failed to register SaveableObject with id {ID} in scene {SceneName}, destroying self.", true);
                Destroy(gameObject);
            }
            else {
                debug.Log($"Registered SaveableObject with id {ID} in scene {SceneName}");
                hasRegistered = true;
            }
        }

        // Unregister can be explicitly called to tell the SaveManager we shouldn't attempt to save this
        public virtual void Unregister() {
            if (hasRegistered && !string.IsNullOrEmpty(SceneName) && Application.isPlaying && SaveManager.IsRegistered(ID)) {
                SaveManager.Unregister(ID);
            }
        }

        /// <summary>
        /// Should be called instead of Destroy(script) to ensure that the SaveManagerForScene is aware of the object's destruction.
        /// As opposed to DynamicObject.Destroy, this method does not destroy the GameObject itself, only the script.
        /// </summary>
        public virtual void Destroy() {
            Unregister();
            Destroy(this);
        }

#region Registration

        protected virtual void Awake() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return;
            }
#endif
            if (LevelManager.instance == null ||
                !(LevelManager.instance.loadedLevels.Contains(Level) ||
                  LevelManager.instance.currentlyLoadingLevels.Contains(Level))) {
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
            
            if (!gameObject || !gameObject.IsInLoadedScene()) yield break;
            
            // We register here as well because RegisterOnLevelChangeEvents won't fire when the game is first started
            // This call is idempotent so it should not matter if it is called after already registering
            Register();
        }

        IEnumerator InitCoroutine() {
            yield return new WaitUntil(() => GameManager.instance?.gameHasLoaded ?? false);
            DoInitIdempotent();
        }

        void InitAfterSceneRestoreState(Levels levelLoaded) {
            if (levelLoaded == Level) {
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

        protected virtual void OnDisable() {
            SaveManager.BeforeLoad -= ResetState;
        }

        /// <summary>
        /// Register this SaveableObject with the SaveManagerForScene when the scene changes, then stop listening for scene changes
        /// </summary>
        /// <param name="level">Level switched to, compared against this.Level to know when to trigger</param>
        void RegisterOnLevelChangeEvents(Levels levelSwitchedTo) {
            if (hasRegistered) return;
            
            if (levelSwitchedTo == Level) {
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
#endregion
    }

    /// <summary>
    /// Abstract generically-typed base class for SuperspectiveObjects, which are objects in the scenes that can be saved and loaded.
    /// This contains the generic type parameters for the SuperspectiveObject (self-type) and the SaveObject (serializable save data type).
    /// Most of the behavior is in the abstract base class SuperspectiveObject, but this class is necessary to allow for the generic type parameters.
    /// </summary>
    /// <typeparam name="T">Self type of the inheriting script</typeparam>
    /// <typeparam name="S">Type of the SaveObject for the script</typeparam>
    public abstract class SuperspectiveObject<T, S> : SuperspectiveObject
        where T : SuperspectiveObject
        where S : SaveObject<T> {

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
        
        // Generically call the constructor of whatever type S is, with a reference to this object as the only parameter
        public override SaveObject CreateSave() {
            return (S)Activator.CreateInstance(typeof(S), new object[] { this });
        }

        /// <summary>
        /// Generic method to load the save data from the SaveObject.
        /// This is the method that should be called by SaveManager, not LoadSave.
        /// LoadSave is just provided so that inheriting classes can override it to load their own save data, and is invoked from this method.
        /// </summary>
        /// <param name="savedObject"></param>
        public override void LoadFromSave(SaveObject savedObject) {
            base.LoadFromSave(savedObject);
            
            if (savedObject is not S save) {
                debug.LogError($"SaveObject is not of type {typeof(S).Name}");
                return;
            }

            LoadSave(save);
        }
        
        // Type-specific method to load the save data from the SaveObject
        public abstract void LoadSave(S save);

        public override bool SkipSave { get; set; }
    }
}