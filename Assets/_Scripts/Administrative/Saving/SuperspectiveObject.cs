using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SuperspectiveUtils;
using LevelManagement;
using Sirenix.OdinInspector;
using SuperspectiveAttributes;
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
        [DoNotSave]
        public bool gameObjectStartsInactive = false;
        [DoNotSave]
        public bool scriptStartsDisabled = false;

        [HideInInspector, DoNotSave]
        public Vector3 _startPosition;
        [HideInInspector, DoNotSave]
        public Quaternion _startRotation;
        
        [DoNotSave]
        protected bool hasInitialized = false;
        [DoNotSave]
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
        
        [DoNotSave]
        public bool DEBUG = false;
        DebugLogger _debug;
        public DebugLogger debug => _debug ??= new DebugLogger(gameObject, ID, () => DEBUG);
        
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
                return $"{GetType().GetReadableTypeName()}{suffix}";
            }
        }
        
        public virtual string AssociationID {
            get {
                string lastPart = ID.Split('_').Last();
                return lastPart.IsGuid() ? lastPart : ID;
            }
        }
        
        // Yeah you shouldn't rely on exception handling for logic flow, but this is the least intrusive way I can add support needed at this moment.
        public virtual bool HasValidId {
            get {
                try {
                    // Attempt to retrieve the ID
                    var id = ID;
                    return true;
                }
                catch {
                    // Safely handle invalid ID
                    return false;
                }
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
            
            transform.localRotation = save.localRotation;
            if (transform is RectTransform rectTransform) {
                rectTransform.anchoredPosition = save.anchoredPosition;
                rectTransform.localScale = save.localScale;
            }
            else {
                debug.Log($"Loading local position {save.localPosition}. Current local position is {transform.localPosition}.");
                transform.localPosition = save.localPosition;
            }

            // (Serialized Type name, field FieldInfo)
            List<(string, FieldInfo)> fieldsUpdatedImplicitly = new List<(string, FieldInfo)>();
            foreach (var kv in save.implicitlySavedFields.Dictionary) {
                string fieldName = kv.Key;
                object data = kv.Value;
                
                FieldInfo field = this.GetType()
                    .TraverseTypeHierarchy()
                    .Select(t => t.GetField(fieldName, SaveSerializationUtils.FIELD_TAGS))
                    .FirstOrDefault(f => f != null);

                if (field == null) {
                    debug.LogError($"Field {fieldName} not found on {GetType().GetReadableTypeName()}", true);
                    continue;
                }
                
                object value = field.GetValue(this);
                if (!SaveSerializationUtils.TryGetDeserializedData(data, field.FieldType, ref value)) {
                    debug.LogError($"Field {fieldName} ({field.FieldType.GetReadableTypeName()}) not deserialized from {GetType().GetReadableTypeName()}", true);
                }
                field.SetValue(this, value);
                fieldsUpdatedImplicitly.Add((data?.GetType().GetReadableTypeName() ?? "(null)", field));
            }
            debug.Log($"{fieldsUpdatedImplicitly.Count} implicitly loaded fields:\n{GetDebugStringForImplicitlyLoadedFields(fieldsUpdatedImplicitly)}");
        }

        private string GetDebugStringForImplicitlyLoadedFields(List<(string, FieldInfo)> fields) {
            return string.Join("\n", fields.Select(f => {
                string serializedTypeName = f.Item1;
                FieldInfo field = f.Item2;
                Type fieldType = field.FieldType;
                string fieldTypeName = fieldType.GetReadableTypeName();

                string typeDetails = (serializedTypeName == fieldTypeName || serializedTypeName == "(null)") ? fieldTypeName : $"{serializedTypeName} -> {fieldTypeName}";
                string valueDetails = field.GetValue(this)?.ToString() ?? "(null)";
                return $"{field.Name} ({typeDetails}) = {valueDetails}";
            }));
        }

        [DoNotSave]
        public virtual bool SkipSave {
            get => !this;
            set { }
        }
        
        /// <summary>
        /// Init is similar to Awake() and Start(), but it is only called after the LevelManager is done loading scenes
        /// Or, on SceneManagerForScene.AfterSceneRestoreState, whichever one happens first
        /// </summary>
        protected virtual void Init() {
            SaveManager.BeforeLoad += ResetState;
        }

        protected virtual void OnDisable() {
            SaveManager.BeforeLoad -= ResetState;
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
        /// This is called after a save is loaded, so that even objects which do not have an entry in the save file can be reset.
        /// </summary>
        public virtual void ResetState() {
            if (this == null || gameObject == null) return;
            
            debug.Log("ResetState for " + gameObject.name + $"\nInitial pos: {_startPosition}, current pos: {transform.position}\nInitial rot: {_startRotation}, current rot: {transform.rotation}");
            gameObject.SetActive(!gameObjectStartsInactive);
            enabled = !scriptStartsDisabled;
            transform.position = _startPosition;
            transform.rotation = _startRotation;
        }
        
        private string TypeName => this.GetType().GetReadableTypeName();

        // Registration is triggered off an event from SaveManagerForScene that happens after Awake but before Start
        // This allows modifications to IDs (such as with MultiDimensionCube) to take effect before registration
        public virtual void Register() {
            if (hasRegistered || string.IsNullOrEmpty(SceneName) || !Application.isPlaying) {
                return;
            }
            
            if (!SaveManager.Register(this)) {
                debug.LogError($"Failed to register {TypeName} with id {ID} in scene {SceneName}, destroying self.", true);
                Destroy(gameObject);
            }
            else {
                debug.Log($"Registered {TypeName} with id {ID} in scene {SceneName}");
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

            UpdateInitialStates();

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

        /// <summary>
        /// Register this SuperspectiveObject with the SaveManagerForScene when the scene changes, then stop listening for scene changes
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
        /// The similarly named LoadSave is just provided so that inheriting classes can override it to load their own save data, and is invoked from this method.
        /// Thus, this is the method that should be called by SaveManager, not LoadSave.
        /// </summary>
        /// <param name="savedObject"></param>
        public override void LoadFromSave(SaveObject savedObject) {
            base.LoadFromSave(savedObject);
            
            if (savedObject is not S save) {
                debug.LogError($"SaveObject is not of type {typeof(S).GetReadableTypeName()}");
                return;
            }

            LoadSave(save);
        }
        
        // Type-specific method to load the save data from the SaveObject
        public abstract void LoadSave(S save);

        [DoNotSave]
        public override bool SkipSave { get; set; }
    }
}