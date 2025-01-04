using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;
using SuperspectiveUtils;
using LevelManagement;
using Library.Functional;
using Unity.Jobs;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Saving {
    public static partial class SaveManager {
        
        public static bool DEBUG = false;
        public static bool isCurrentlyLoadingSave = false;
        public static readonly Dictionary<Levels, SaveManagerForLevel> saveManagers = new Dictionary<Levels, SaveManagerForLevel>();
        private static SaveManagerCache cache = new SaveManagerCache();

        public delegate void SaveAction();

        public static event SaveAction BeforeSave;
        public static event SaveAction BeforeLoad;

        private static float realTimeOfLastLoad = 0f;
        public static float RealtimeSinceLastLoad => Time.realtimeSinceStartup - realTimeOfLastLoad;
        
        private static float timeOfLastLoad = 0f;
        public static float TimeSinceLastLoad => Time.time - timeOfLastLoad;

        private static DebugLogger _debug;
        private static DebugLogger debug => _debug ??= new DebugLogger(LevelManager.instance, () => DEBUG);

        public static SaveManagerForLevel GetOrCreateSaveManagerForLevel(Levels level) {
            if (!level.IsValid()) {
                return null;
			}

            if (!saveManagers.ContainsKey(level)) {
                saveManagers.Add(level, new SaveManagerForLevel(level));
            }
            
            return saveManagers[level];
		}
        
#region Save/Load
        /// <summary>
        /// Starts the process of asynchronously saving the current state of the game to disk.
        /// </summary>
        /// <param name="saveMetadataWithScreenshot">Metadata for the save</param>
        /// <returns>JobHandle for the asynchronous Save process</returns>
        public static JobHandle Save(SaveMetadataWithScreenshot saveMetadataWithScreenshot) {
            Debug.Log($"--- Saving Save File: {saveMetadataWithScreenshot.metadata.displayName} ---");
            BeforeSave?.Invoke();
            return SaveFileUtils.WriteSaveToDisk(saveMetadataWithScreenshot);
        }

        /// <summary>
        /// Asynchronously loads the save file from disk and restores the game state.
        /// </summary>
        /// <param name="saveMetadata">Metadata for the save file to be loaded</param>
        public static async void Load(SaveMetadataWithScreenshot saveMetadata) {
            string saveName = saveMetadata.metadata.saveFilename;
            Debug.Log($"--- Loading Save File: {saveName} ---");
            
            // Updating this at beginning and end of Load to make sure loading knows that a load just happened as well as mark the finish time
            realTimeOfLastLoad = Time.realtimeSinceStartup;
            timeOfLastLoad = Time.time;
            isCurrentlyLoadingSave = true;
            
            if (LevelManager.instance.IsCurrentlySwitchingScenes || LevelManager.instance.isCurrentlySwitchingScenes) {
                Debug.Log("Waiting for in-progress scene loading to finish before starting load...");
                await TaskEx.WaitUntil(() => !LevelManager.instance.IsCurrentlySwitchingScenes && !LevelManager.instance.isCurrentlySwitchingScenes);
            }

            BeforeLoad?.Invoke();
            MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.On;
            Time.timeScale = 0f;

            // Clear out existing DynamicObjects so they don't stick around when the new save file is loaded
            DynamicObjectManager.DeleteAllExistingDynamicObjectsAndClearState();
            
            // Get the save file from disk
            SaveData save = SaveData.RetrieveSaveDataFromDisk(saveName);
            
            // Clear state of levels that don't exist in the save file
            List<Levels> levelsNotInSaveFile = saveManagers.Keys.Where(level => level is not Levels.ManagerScene && !save.levels.ContainsKey(level)).ToList();
            foreach (var levelNotInSaveFile in levelsNotInSaveFile) {
                saveManagers[levelNotInSaveFile].ClearAllStateForScene();
                saveManagers.Remove(levelNotInSaveFile);
            }
            
            // Create a SaveManager for ManagerScene and restore its state
            SaveManagerForLevel saveManagerForManagerLevel = GetOrCreateSaveManagerForLevel(Levels.ManagerScene);
            saveManagerForManagerLevel.LoadSuperspectiveObjectsStateFromSaveFile(save.managerLevel);

            Debug.Log("Waiting for scenes to be loaded...");
            await TaskEx.WaitUntil(() => !LevelManager.instance.IsCurrentlySwitchingScenes);
            Debug.Log("All scenes loaded into memory, loading save...");

            // Load records of all DynamicObjects from disk
            save.dynamicObjects.LoadSaveFile();

            // Load all DynamicObjects for each scene
            foreach (var kv in save.levels) {
                Levels level = kv.Key;
                SaveDataForLevel saveDataForLevel = kv.Value;
                SaveManagerForLevel saveManager = GetOrCreateSaveManagerForLevel(level);
                saveManager.LoadDynamicObjectsStateFromSaveFile(saveDataForLevel);
            }
            
            Dictionary<Levels, SaveDataForLevel> loadedLevels = save.levels
                .Where(kv => LevelManager.instance.loadedLevels.Contains(kv.Key))
                .ToDictionary();
            Dictionary<Levels, SaveDataForLevel> unloadedLevels = save.levels
                .Except(loadedLevels)
                .ToDictionary();
            
            Debug.Log($"Loaded levels: {string.Join(", ", loadedLevels.Keys)}");
            Debug.Log($"Unloaded levels: {string.Join(", ", unloadedLevels.Keys)}");
            
            // Restore state for all unloaded scenes from the save file
            foreach (var kv in unloadedLevels) {
                Levels level = kv.Key;
                SaveDataForLevel saveDataForLevel = kv.Value;
                SaveManagerForLevel saveManager = GetOrCreateSaveManagerForLevel(level);
                saveManager.LoadSuperspectiveObjectsStateFromSaveFile(saveDataForLevel);
            }

            // Load data for every object in each loaded scene (starting with the ManagerScene)
            saveManagerForManagerLevel.LoadSuperspectiveObjectsStateFromSaveFile(save.managerLevel);
            foreach (var kv in loadedLevels) {
                Levels level = kv.Key;
                SaveDataForLevel saveDataForLevel = kv.Value;
                SaveManagerForLevel saveManager = GetOrCreateSaveManagerForLevel(level);
                saveManager.LoadSuperspectiveObjectsStateFromSaveFile(saveDataForLevel);
            }
            
            // Update the lastLoadedTime for this save
            DateTime now = DateTime.Now;
            saveMetadata.metadata.lastLoadedTimestamp = now.Ticks;
            SaveFileUtils.WriteMetadataToDisk(saveMetadata);

            // Play the level change banner and remove the black overlay
            LevelChangeBanner.instance.PlayBanner(LevelManager.instance.ActiveScene);
            Time.timeScale = GameManager.timeScale;
            MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.FadingOut;
            realTimeOfLastLoad = Time.realtimeSinceStartup;
            isCurrentlyLoadingSave = false;
        }

        private static string SettingsFilePath => $"{Application.persistentDataPath}/settings.ini";
        
        /// <summary>
        /// Tells the settings to write themselves to disk.
        /// </summary>
        public static void SaveSettings() {
            Settings.instance.WriteSettings(SettingsFilePath);
        }

        /// <summary>
        /// Tells the settings to load themselves from disk.
        /// </summary>
        public static void LoadSettings() {
            Settings.instance.LoadSettings(SettingsFilePath);
        }
        
#endregion

        /// <summary>
        /// Clears all state for all scenes and deletes all existing DynamicObjects.
        /// </summary>
        public static void ClearAllState() {
            DynamicObjectManager.DeleteAllExistingDynamicObjectsAndClearState();
            saveManagers.Clear();
            cache.Clear();
        }
#region Superspective Object Management

        /// <summary>
        /// Registers a SuperspectiveObject with its SaveManagerForLevel, and caches its level and association ID.
        /// </summary>
        /// <param name="superspectiveObj">SuperspectiveObject to register.</param>
        /// <returns>True if the SuperspectiveObject was registered successfully, false otherwise.</returns>
        public static bool Register(SuperspectiveObject superspectiveObj) {
            cache.AddSuperspectiveObject(superspectiveObj);
            
            return GetOrCreateSaveManagerForLevel(superspectiveObj.Level).RegisterSuperspectiveObjectInScene(superspectiveObj);
        }
        
        /// <summary>
        /// Registers a DynamicObject with its SaveManagerForLevel, and caches its level and association ID.
        /// </summary>
        /// <param name="dynamicObject">DynamicObject to register.</param>
        /// <returns>True if the DynamicObject was registered successfully, false otherwise.</returns>
        public static bool RegisterDynamicObject(DynamicObject dynamicObject) {
            cache.AddDynamicObject(dynamicObject);
            
            return GetOrCreateSaveManagerForLevel(dynamicObject.Level).RegisterDynamicObjectInScene(dynamicObject);
        }
        
        /// <summary>
        /// Unregisters a SuperspectiveObject with its SaveManagerForLevel, and removes its level and association ID from the cache.
        /// </summary>
        /// <param name="id">ID of the SuperspectiveObject to unregister.</param>
        /// <returns>True if the SuperspectiveObject was unregistered successfully, false otherwise.</returns>
        public static bool Unregister(string id) {
            Levels level = cache.GetLevelForSuperspectiveObject(id);
            if (level.IsValid() && cache.RemoveSuperspectiveObject(id)) {
                return GetOrCreateSaveManagerForLevel(level).UnregisterSuperspectiveObjectInScene(id);
            }
            
            debug.LogError($"No valid scene found for SuperspectiveObject with id {id}. Was this object already unregistered?", true);
            return false;
        }
        
        /// <summary>
        /// Unregisters a DynamicObject with its SaveManagerForLevel, and removes its level and association ID from the cache.
        /// </summary>
        /// <param name="id">ID of the DynamicObject to unregister.</param>
        /// <returns>True if the DynamicObject was unregistered successfully, false otherwise.</returns>
        public static bool UnregisterDynamicObject(string id) {
            Levels level = cache.GetLevelForDynamicObject(id);
            if (level.IsValid() && cache.RemoveDynamicObject(id)) {
                return GetOrCreateSaveManagerForLevel(level).UnregisterDynamicObjectInScene(id);
            }
            
            debug.LogError($"No valid scene found for DynamicObject with id {id}. Was this object already unregistered?", true);
            return false;
        }

        /// <summary>
        /// Unregisters a DynamicObject from the old level and registers it in the new level.
        /// Also migrates all associated SuperspectiveObjects to the new level.
        /// </summary>
        /// <param name="dynamicObj">DynamicObject to change the level of</param>
        /// <param name="oldLevel">Level the DynamicObject is moving from</param>
        /// <param name="newLevel">Level the DynamicObject is moving to</param>
        /// <returns></returns>
        public static void ChangeDynamicObjectLevel(DynamicObject dynamicObj, Levels oldLevel, Levels newLevel) {
            cache.SetLevelForDynamicObject(dynamicObj.ID, newLevel);
            
            SaveManagerForLevel oldLevelSaveManager = GetOrCreateSaveManagerForLevel(oldLevel);
            SaveManagerForLevel newLevelSaveManager = GetOrCreateSaveManagerForLevel(newLevel);
            
            // Unregister the DynamicObject from the old scene and register it in the new scene
            oldLevelSaveManager.UnregisterDynamicObjectInScene(dynamicObj.ID);
            newLevelSaveManager.RegisterDynamicObjectInScene(dynamicObj);
            // DynamicObjects are also SuperspectiveObjects
            oldLevelSaveManager.UnregisterSuperspectiveObjectInScene(dynamicObj.ID);
            newLevelSaveManager.RegisterSuperspectiveObjectInScene(dynamicObj);

            // Migrate all associated SuperspectiveObjects to the new scene
            List<SuperspectiveObject> associatedSuperspectiveObjects = GetAllAssociatedSuperspectiveObjects(dynamicObj.AssociationID);
            foreach (SuperspectiveObject associatedSuperspectiveObject in associatedSuperspectiveObjects) {
                oldLevelSaveManager.UnregisterSuperspectiveObjectInScene(associatedSuperspectiveObject.ID);
                newLevelSaveManager.RegisterSuperspectiveObjectInScene(associatedSuperspectiveObject);
            }
        }
        
        /// <summary>
        /// Returns all associated IDs for a given associationId.
        /// </summary>
        /// <param name="associationId">Unique part of an ID (usually a GUID except for singleton objects which can just be a descriptive string)</param>
        /// <returns>All registered SuperspectiveObject IDs whose associationId match the input</returns>
        public static List<string> GetAllAssociatedIds(string associationId) {
            return cache.GetAssociatedIds(associationId).ToList();
        }
        
        /// <summary>
        /// Returns true if a unique ID is associated with any registered SuperspectiveObject.
        /// </summary>
        /// <param name="id">Unique ID to check registered status</param>
        /// <returns>True if any associated IDs are already registered, false otherwise</returns>
        public static bool IsAnyAssociatedObjectRegistered(string id) {
            return GetAllAssociatedIds(id.GetAssociationId()).Count > 0;
        }

        public static bool IsRegistered(string id) {
            return cache.GetLevelForSuperspectiveObject(id) != Levels.InvalidLevel;
        }

        /// <summary>
        /// Returns all associated SuperspectiveObjects for a given associationId.
        /// Since this uses LeftOrDefault, this should only be called when you're sure that all the associated IDs are in loaded scenes.
        /// </summary>
        /// <param name="associationId">Unique part of an ID (usually a GUID except for singleton objects which can just be a descriptive string)</param>
        /// <returns>All registered SuperspectiveObjects whose associationId match the input</returns>
        public static List<SuperspectiveObject> GetAllAssociatedSuperspectiveObjects(string associationId) {
            return GetAllAssociatedIds(associationId).Select(associatedId => {
                Levels levelForAssociatedId = cache.GetLevelForSuperspectiveObject(associatedId);
                return GetOrCreateSaveManagerForLevel(levelForAssociatedId).GetSuperspectiveObjectInScene(associatedId).LeftOrDefault();
            }).ToList();
        }

        public static List<SaveObject> GetAllAssociatedSaveObjects(string associationId) {
            return GetAllAssociatedIds(associationId).Select(associatedId => {
                Levels levelForAssociatedId = cache.GetLevelForSuperspectiveObject(associatedId);
                return GetOrCreateSaveManagerForLevel(levelForAssociatedId).GetSuperspectiveObjectInScene(associatedId).Match(
                    superspectiveObject => new SaveObject(superspectiveObject),
                    saveObject => saveObject
                );
            }).ToList();
        }

        /// <summary>
        /// Returns either the loaded DynamicObject or the serialized DynamicObjectSave by ID.
        /// </summary>
        /// <param name="id">The ID of the DynamicObject to retrieve</param>
        /// <returns>Either the loaded DynamicObject or the serialized DynamicObjectSave</returns>
        public static Either<DynamicObject, DynamicObject.DynamicObjectSave> GetDynamicObjectById(string id) {
            if (!Application.isPlaying) {
                return FindObjectById<DynamicObject>(id);
            }

            Levels level = cache.GetLevelForDynamicObject(id);
            if (!level.IsValid()) {
                DynamicObject objFound = FindObjectById<DynamicObject>(id);
                if (objFound != null) {
                    cache.AddDynamicObject(objFound);
                }

                return objFound;
            }
            
            return GetOrCreateSaveManagerForLevel(level).GetDynamicObject(id);
        }

        /// <summary>
        /// Returns either the loaded SuperspectiveObject or the serialized SaveObject by ID.
        /// </summary>
        /// <param name="id">The ID of the SuperspectiveObject to retrieve</param>
        /// <returns>Either the loaded SuperspectiveObject or the serialized SaveObject</returns>
        public static Either<SuperspectiveObject, SaveObject> GetSuperspectiveObjectById(string id) {
            if (!Application.isPlaying) {
                return FindObjectById<SuperspectiveObject>(id);
            }

            Levels level = cache.GetLevelForSuperspectiveObject(id);
            if (!level.IsValid()) {
                SuperspectiveObject objFound = FindObjectById<SuperspectiveObject>(id);
                if (objFound != null) {
                    cache.AddSuperspectiveObject(objFound);
                }

                return objFound;
            }
            
            return GetOrCreateSaveManagerForLevel(level).GetSuperspectiveObjectInScene(id);
        }
        
        /// <summary>
        /// Looks everywhere for an object with the given ID. This is inefficient and should be avoided where possible.
        /// </summary>
        /// <param name="id"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T FindObjectById<T>(string id) where T : class, ISaveableObject {
            // Log a warning because we want to try to avoid using this function wherever possible (except in the Editor, w/e)
            if (Application.isPlaying) {
                Debug.LogWarning($"FindObjectById called for {id}. This is inefficient and should be avoided where possible.");
            }

            List<MonoBehaviour> matches = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                .OfType<ISaveableObject>()
                .Where(s => s.HasValidId() && s.ID.Contains(id))
                .OfType<MonoBehaviour>()
                .ToList();

            if (matches.Count == 0) {
                Debug.LogError($"No object with id {id} found! Maybe in a scene that's not loaded?");
                return null;
            }
            else if (matches.Count > 1) {
                Debug.LogError($"Multiple objects with id {id} found.");
                return matches[0] as T;
            }
            else {
                return matches[0] as T;
            }
        }
#endregion

#if UNITY_EDITOR
        [MenuItem("Saving/Add UniqueIds where needed (in loaded scenes)")]
        public static void AddUniqueIdsToAllSuperspectiveObjectsNeedingOne() {
            List<MonoBehaviour> test = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                // Find all script instances which require a UniqueId
                .Where(s => s.GetType().GetCustomAttributes<RequireComponent>().Any(a => a.m_Type0 == typeof(UniqueId)))
                // Find all non-dynamic objects which lack a uniqueId
                .Where(s => s.GetComponent<UniqueId>() == null && s.GetComponent<DynamicObject>() == null)
                .ToList();

            int count = 0;
            foreach (var script in test) {
                // Do this check again so we don't double-add for multiple scripts on same GO
                if (script.GetComponent<UniqueId>() == null) {
                    script.gameObject.AddComponent<UniqueId>();
                    EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
                    count++;
				}
			}

            if (count > 0) {
                Debug.Log($"Added UniqueIds to {count} objects:\n{string.Join("\n", test)}");
            }
            else {
                Debug.Log("Nothing to add a UniqueId to. All set.");
			}
		}

        [MenuItem("Saving/Clear Saves")]
        public static void ClearSaves() {
            Directory.Delete($"{Application.persistentDataPath}/Saves/", true);
        }
#endif
    }
}

// TODO: Give this a home
public static class TaskEx {
    /// <summary>
    /// Blocks while condition is true or timeout occurs.
    /// </summary>
    /// <param name="condition">The condition that will perpetuate the block.</param>
    /// <param name="frequency">The frequency at which the condition will be check, in milliseconds.</param>
    /// <param name="timeout">Timeout in milliseconds.</param>
    /// <exception cref="TimeoutException"></exception>
    /// <returns></returns>
    public static async Task WaitWhile(Func<bool> condition, int frequency = 25, int timeout = -1) {
        var waitTask = Task.Run(async () => {
            while (condition()) await Task.Delay(frequency);
        });

        if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
            throw new TimeoutException();
    }

    /// <summary>
    /// Blocks until condition is true or timeout occurs.
    /// </summary>
    /// <param name="condition">The break condition.</param>
    /// <param name="frequency">The frequency at which the condition will be checked.</param>
    /// <param name="timeout">The timeout in milliseconds.</param>
    /// <returns></returns>
    public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1) {
        var waitTask = Task.Run(async () => {
            while (!condition()) await Task.Delay(frequency);
        });

        if (waitTask != await Task.WhenAny(waitTask,
                Task.Delay(timeout)))
            throw new TimeoutException();
    }
}