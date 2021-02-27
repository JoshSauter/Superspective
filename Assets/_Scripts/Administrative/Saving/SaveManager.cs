using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static Saving.SaveManagerForScene;
using System.Reflection;
using EpitaphUtils;
using LevelManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Saving {
    public static class SaveManager {
        public static bool DEBUG = false;
        public static bool isCurrentlyLoadingSave = false;
        public static readonly Dictionary<string, SaveManagerForScene> saveManagers = new Dictionary<string, SaveManagerForScene>();

        public static SaveManagerForScene GetOrCreateSaveManagerForScene(string sceneName) {
            if (string.IsNullOrEmpty(sceneName)) {
                return null;
			}

            if (!saveManagers.ContainsKey(sceneName)) {
                saveManagers.Add(sceneName, new SaveManagerForScene(sceneName));
            }
            
            return saveManagers[sceneName];
		}

		static string SavePath(string saveFileName) {
            return $"{Application.persistentDataPath}/Saves/{saveFileName}";
        }

        public static void Save(string saveName) {
            Debug.Log($"--- Saving Save File: {saveName} ---");
            SaveFile.CreateSaveFileFromCurrentState(saveName).WriteToDisk();
        }

        public static async void Load(string saveName) {
            Debug.Log($"--- Loading Save File: {saveName} ---");
            
            isCurrentlyLoadingSave = true;

            MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.On;
            Time.timeScale = 0f;

            // Clear out existing DynamicObjects so they don't stick around when the new save file is loaded
            DynamicObjectManager.DeleteAllExistingDynamicObjectsAndClearState();
            
            // Get the save file from disk
            SaveFile save = SaveFile.RetrieveSaveFileFromDisk(saveName);
            
            // Create a SaveManager for ManagerScene and restore its state
            SaveManagerForScene saveManagerForManagerScene = GetOrCreateSaveManagerForScene(LevelManager.ManagerScene);
            saveManagerForManagerScene.LoadSaveableObjectsStateFromSaveFile(save.managerScene);

            Debug.Log("Waiting for scenes to be loaded...");
            await TaskEx.WaitUntil(() => !LevelManager.instance.IsCurrentlyLoadingScenes);
            Debug.Log("All scenes loaded into memory, loading save...");

            // Load records of all DynamicObjects from disk
            save.dynamicObjects.LoadSaveFile();

            // Load all DynamicObjects for each scene
            foreach (var kv in save.scenes) {
                string sceneName = kv.Key;
                SaveFileForScene saveFileForScene = kv.Value;
                SaveManagerForScene saveManager = GetOrCreateSaveManagerForScene(sceneName);
                saveManager.LoadDynamicObjectsStateFromSaveFile(saveFileForScene);
            }
            
            Dictionary<string, SaveFileForScene> loadedScenes = save.scenes
                .Where(kv => LevelManager.instance.loadedSceneNames.Contains(kv.Key))
                .ToDictionary();
            Dictionary<string, SaveFileForScene> unloadedScenes = save.scenes
                .Except(loadedScenes)
                .ToDictionary();
            
            // Restore state for all unloaded scenes from the save file
            foreach (var kv in unloadedScenes) {
                string sceneName = kv.Key;
                SaveFileForScene saveFileForScene = kv.Value;
                SaveManagerForScene saveManager = GetOrCreateSaveManagerForScene(sceneName);
                saveManager.LoadSaveableObjectsStateFromSaveFile(saveFileForScene);
            }

            // Load data for every object in each loaded scene (starting with the ManagerScene)
            saveManagerForManagerScene.LoadSaveableObjectsStateFromSaveFile(save.managerScene);
            foreach (var kv in loadedScenes) {
                string sceneName = kv.Key;
                SaveFileForScene saveFileForScene = kv.Value;
                SaveManagerForScene saveManager = GetOrCreateSaveManagerForScene(sceneName);
                saveManager.LoadSaveableObjectsStateFromSaveFile(saveFileForScene);
            }

            // Play the level change banner and remove the black overlay
            LevelChangeBanner.instance.PlayBanner(LevelManager.instance.ActiveScene);
            Time.timeScale = 1f;
            MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.FadingOut;
            isCurrentlyLoadingSave = false;
        }

        public static void DeleteSave(string saveName) {
            string path = SavePath(saveName);
            if (Directory.Exists(path)) {
                Directory.Delete(path, true);
            }
        }

        static void CopyDirectory(string sourcePath, string targetPath) {
            if (Directory.Exists(sourcePath)) {
                Directory.CreateDirectory(targetPath);
                string[] files = Directory.GetFiles(sourcePath);

                // Copy the files and overwrite destination files if they already exist.
                foreach (string s in files) {
                    // Use static Path methods to extract only the file name from the path.
                    string fileName = Path.GetFileName(s);
                    string destFile = Path.Combine(targetPath, fileName);
                    File.Copy(s, destFile, true);
                }
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