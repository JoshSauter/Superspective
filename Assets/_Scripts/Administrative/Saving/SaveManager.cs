using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static Saving.SaveManagerForScene;
using System.Reflection;
using SuperspectiveUtils;
using LevelManagement;
using Library.Functional;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Saving {
    public static class SaveManager {
        public static bool DEBUG = false;
        public static bool isCurrentlyLoadingSave = false;
        public static readonly Dictionary<string, SaveManagerForScene> saveManagers = new Dictionary<string, SaveManagerForScene>();
        // Keeps track of what scene a given SaveableObject ID can be located in
        public static readonly Dictionary<string, string> sceneLookupForId = new Dictionary<string, string>();

        public delegate void SaveAction();

        public static event SaveAction BeforeSave;
        public static event SaveAction BeforeLoad;

        private static float timeOfLastLoad = 0f;
        public static float realtimeSinceLastLoad => Time.realtimeSinceStartup - timeOfLastLoad;

        public static SaveManagerForScene GetOrCreateSaveManagerForScene(string sceneName) {
            if (string.IsNullOrEmpty(sceneName)) {
                return null;
			}

            if (!saveManagers.ContainsKey(sceneName)) {
                saveManagers.Add(sceneName, new SaveManagerForScene(sceneName));
            }
            
            return saveManagers[sceneName];
		}

        private static string SettingsFilePath => $"{Application.persistentDataPath}/settings.ini";

        public static void Save(SaveMetadataWithScreenshot saveMetadataWithScreenshot) {
            Debug.Log($"--- Saving Save File: {saveMetadataWithScreenshot.metadata.displayName} ---");
            BeforeSave?.Invoke();
            SaveFileUtils.WriteSaveToDisk(saveMetadataWithScreenshot);
        }

        public static async void Load(SaveMetadataWithScreenshot saveMetadata) {
            string saveName = saveMetadata.metadata.saveFilename;
            Debug.Log($"--- Loading Save File: {saveName} ---");
            
            // Updating this at beginning and end of Load to make sure loading knows that a load just happened as well as mark the finish time
            timeOfLastLoad = Time.realtimeSinceStartup;
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
            List<string> levelsNotInSaveFile = saveManagers.Keys.Where(level => !save.scenes.ContainsKey(level)).ToList();
            foreach (var levelNotInSaveFile in levelsNotInSaveFile) {
                saveManagers[levelNotInSaveFile].ClearAllState();
                saveManagers.Remove(levelNotInSaveFile);
            }
            
            // Create a SaveManager for ManagerScene and restore its state
            SaveManagerForScene saveManagerForManagerScene = GetOrCreateSaveManagerForScene(LevelManager.ManagerScene);
            saveManagerForManagerScene.LoadSaveableObjectsStateFromSaveFile(save.managerScene);

            Debug.Log("Waiting for scenes to be loaded...");
            await TaskEx.WaitUntil(() => !LevelManager.instance.IsCurrentlySwitchingScenes);
            Debug.Log("All scenes loaded into memory, loading save...");

            // Load records of all DynamicObjects from disk
            save.dynamicObjects.LoadSaveFile();

            // Load all DynamicObjects for each scene
            foreach (var kv in save.scenes) {
                string sceneName = kv.Key;
                SaveDataForScene saveDataForScene = kv.Value;
                SaveManagerForScene saveManager = GetOrCreateSaveManagerForScene(sceneName);
                saveManager.LoadDynamicObjectsStateFromSaveFile(saveDataForScene);
            }
            
            Dictionary<string, SaveDataForScene> loadedScenes = save.scenes
                .Where(kv => LevelManager.instance.loadedSceneNames.Contains(kv.Key))
                .ToDictionary();
            Dictionary<string, SaveDataForScene> unloadedScenes = save.scenes
                .Except(loadedScenes)
                .ToDictionary();
            
            Debug.Log($"Loaded scenes: {string.Join(", ", loadedScenes.Keys)}");
            Debug.Log($"Unloaded scenes: {string.Join(", ", unloadedScenes.Keys)}");
            
            // Restore state for all unloaded scenes from the save file
            foreach (var kv in unloadedScenes) {
                string sceneName = kv.Key;
                SaveDataForScene saveDataForScene = kv.Value;
                SaveManagerForScene saveManager = GetOrCreateSaveManagerForScene(sceneName);
                saveManager.LoadSaveableObjectsStateFromSaveFile(saveDataForScene);
            }

            // Load data for every object in each loaded scene (starting with the ManagerScene)
            saveManagerForManagerScene.LoadSaveableObjectsStateFromSaveFile(save.managerScene);
            foreach (var kv in loadedScenes) {
                string sceneName = kv.Key;
                SaveDataForScene saveDataForScene = kv.Value;
                SaveManagerForScene saveManager = GetOrCreateSaveManagerForScene(sceneName);
                saveManager.LoadSaveableObjectsStateFromSaveFile(saveDataForScene);
            }
            
            // Update the lastLoadedTime for this save
            DateTime now = DateTime.Now;
            saveMetadata.metadata.lastLoadedTimestamp = now.Ticks;
            SaveFileUtils.WriteMetadataToDisk(saveMetadata);

            // Play the level change banner and remove the black overlay
            LevelChangeBanner.instance.PlayBanner(LevelManager.instance.ActiveScene);
            Time.timeScale = 1f;
            MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.FadingOut;
            timeOfLastLoad = Time.realtimeSinceStartup;
            isCurrentlyLoadingSave = false;
        }

        public static void ClearAllState() {
            DynamicObjectManager.DeleteAllExistingDynamicObjectsAndClearState();
            saveManagers.Clear();
            sceneLookupForId.Clear();
        }

        public static void SaveSettings() {
            Settings.instance.WriteSettings(SettingsFilePath);
        }

        public static void LoadSettings() {
            Settings.instance.LoadSettings(SettingsFilePath);
        }

        public static List<string> GetAllAssociatedIds(string associationId) {
            List<string> associatedIds = new List<string>();
            foreach (string id in sceneLookupForId.Keys) {
                string lastPart = id.Split('_').Last();
                string curAssociationId = lastPart.IsGuid() ? lastPart : id;

                if (curAssociationId == associationId) {
                    associatedIds.Add(id);
                }
            }

            return associatedIds;
        }

        public static Either<DynamicObject, DynamicObject.DynamicObjectSave> GetDynamicObjectById(string id) {
            if (!Application.isPlaying) {
                return FindObjectById<DynamicObject>(id);
            }
            else if (!sceneLookupForId.ContainsKey(id)) {
                DynamicObject objFound = FindObjectById<DynamicObject>(id);
                if (objFound != null) {
                    sceneLookupForId[id] = objFound.gameObject.scene.name;
                }

                return objFound;
            }
            else {
                return GetOrCreateSaveManagerForScene(sceneLookupForId[id]).GetDynamicObject(id);
            }
        }

        public static Either<SaveableObject, SerializableSaveObject> GetSaveableObjectById(string id) {
            if (!Application.isPlaying) {
                return FindObjectById<SaveableObject>(id);
            }
            else if (!sceneLookupForId.ContainsKey(id)) {
                SaveableObject objFound = FindObjectById<SaveableObject>(id);
                if (objFound != null) {
                    sceneLookupForId[id] = objFound.gameObject.scene.name;
                }

                return objFound;
            }
            else {
                return GetOrCreateSaveManagerForScene(sceneLookupForId[id]).GetSaveableObject(id);
            }
        }
        
        public static T FindObjectById<T>(string id) where T : class, ISaveableObject {
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
                Debug.LogWarning($"Multiple objects with id {id} found.");
                return matches[0] as T;
            }
            else {
                return matches[0] as T;
            }
        }

#if UNITY_EDITOR
        [MenuItem("Saving/Add UniqueIds where needed (in loaded scenes)")]
        public static void AddUniqueIdsToAllSaveableObjectsLackingOne() {
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