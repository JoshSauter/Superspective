using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LevelManagement;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace Saving {
    public class AutosaveManager : Singleton<AutosaveManager> {
        public float AutosaveInterval => Convert.ToSingle(Settings.Autosave.AutosaveInterval.dropdownSelection.selection.Datum);
        public int MaxNumberOfAutosaves => (int)Settings.Autosave.NumAutosaves;
        private bool IsLoading => GameManager.instance.IsCurrentlyLoading;

#if UNITY_EDITOR
        private bool _canMakeAutosaveInEditor = false;
        public bool ToggleCanMakeAutosaveInEditor() => _canMakeAutosaveInEditor = !_canMakeAutosaveInEditor;
#endif
        private const float MIN_TIME_BETWEEN_AUTOSAVES_SEC = 10f;
        private const float AUTOSAVE_DISABLED_AFTER_LOAD_DELAY = 30f;

        private HashSet<Levels> levelsToSkipAutosaveOnLoad = new HashSet<Levels>() {
            Levels.LocusMinimusBetweenWorlds,
            Levels.LocusMinimusDarkSide
        };

        private bool TimedAutosaveEnabled => Settings.Autosave.AutosaveEnabled && Settings.Autosave.AutosaveOnTimer;
        private bool AutosaveOnLevelLoadEnabled => Settings.Autosave.AutosaveEnabled && Settings.Autosave.AutosaveOnLevelChange;
        
        private bool CanMakeAutosave => !isAutosaving && timeSinceLastAutosave > MIN_TIME_BETWEEN_AUTOSAVES_SEC && !PlayerMovement.instance.PlayerIsAFK;
        public bool CanMakeTimedAutosave => TimedAutosaveEnabled && CanMakeAutosave
        #if UNITY_EDITOR
        && _canMakeAutosaveInEditor
        #endif
        ;
        public bool CanMakeAutosaveOnLevelLoad => AutosaveOnLevelLoadEnabled &&
                                                  CanMakeAutosave &&
                                                  SaveManager.RealtimeSinceLastLoad > AUTOSAVE_DISABLED_AFTER_LOAD_DELAY &&
                                                  !levelsToSkipAutosaveOnLoad.Contains(LevelManager.instance.ActiveScene)
#if UNITY_EDITOR
                                                  && _canMakeAutosaveInEditor
#endif
        ;

        public float timeSinceLastAutosave = 0f;
        bool isAutosaving = false;

        private const float delayAfterLevelLoadToSave = 1f;

        private void Start() {
            StartCoroutine(RegularAutosave());

            LevelManager.instance.OnActiveSceneChange += () => {
                if (CanMakeAutosaveOnLevelLoad) {
                    StartCoroutine(DoAutosaveDelayed(delayAfterLevelLoadToSave));
                }
            };
        }

        IEnumerator DoAutosaveDelayed(float delay) {
            yield return new WaitForSeconds(delay);
            yield return new WaitUntil(() => !IsLoading && CanMakeAutosaveOnLevelLoad);
            
            DoAutosave();
        }

        IEnumerator RegularAutosave() {
            yield return new WaitForSeconds(AutosaveInterval);
            
            while (true) {
                if (CanMakeTimedAutosave) {
                    DoAutosave();
                }

                yield return new WaitForSeconds(AutosaveInterval);
            }
        }

        public SaveMetadataWithScreenshot DoAutosave(string nameOverride = "") {
            isAutosaving = true;
            LoadingIcon.instance.ShowLoadingIcon();
            SaveMetadataWithScreenshot saveMetadata = CreateAutosave(nameOverride);
            JobHandle saveJob = SaveManager.Save(saveMetadata);
            timeSinceLastAutosave = 0;

            DeleteExtraAutosaves(saveJob);
            return saveMetadata;
        }

        // Delete autosaves until we're at maxNumberOfAutosaves
        public void DeleteExtraAutosaves(JobHandle dependsOn) {
            JobHandle readAllSavedMetadataJob = SaveFileUtils.ReadAllSavedMetadata(dependsOn, (allMetadata => {
                NativeArray<JobHandle> deleteJobHandles = new NativeArray<JobHandle>(allMetadata
                    .OrderByDescending(m => m.saveTimestamp)
                    .TakeLast((allMetadata.Count > MaxNumberOfAutosaves) ? allMetadata.Count - MaxNumberOfAutosaves : 0)
                    .Select(m => SaveFileUtils.DeleteSave(m.saveFilename))
                    .ToArray(),
                    Allocator.Persistent);
                
                JobHandle combinedJobHandle = JobHandle.CombineDependencies(deleteJobHandles);
                
                StartCoroutine(JobCompleteCallback(combinedJobHandle));
            }));
        }
        
        IEnumerator JobCompleteCallback(JobHandle jobHandle) {
            yield return new WaitUntil(() => jobHandle.IsCompleted);
            
            jobHandle.Complete();
            isAutosaving = false;
        }

        public SaveMetadataWithScreenshot CreateAutosave(string nameOverride = "") {
            bool hasNameOverride = !string.IsNullOrEmpty(nameOverride);
            // Fills in the lowest available # that's not already taken by a save
            string GetNewAutosaveFileDisplayName() {
                DateTime now = DateTime.Now;
                string date = now.ToShortDateString();
                string time = now.ToShortTimeString();
                string desiredSaveFileDisplayName = hasNameOverride ? nameOverride : $"Autosave {date} {time}";
                string desiredSaveFileName = SaveFileUtils.SanitizeString(desiredSaveFileDisplayName);

                int incrementor = 1;
                // If there's already a save file name at this date/time, add a number to the end to make it unique
                while (SaveFileUtils.playerSaveMetadataCache.ContainsKey(desiredSaveFileName)) {
                    desiredSaveFileDisplayName = $"{desiredSaveFileDisplayName}_{incrementor}";
                    desiredSaveFileName = $"{desiredSaveFileName}_{incrementor}";
                    incrementor++;
                }

                return desiredSaveFileDisplayName;
            }

            string saveFileDisplayName = GetNewAutosaveFileDisplayName();
            string saveFilename = SaveFileUtils.SanitizeString(saveFileDisplayName);

            return SaveFileUtils.CreateNewSaveMetadataFromCurrentState(saveFilename, saveFileDisplayName);
        }

        private void Update() {
            timeSinceLastAutosave += Time.deltaTime;
        }
    }
}