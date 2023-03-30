using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LevelManagement;
using SuperspectiveUtils;
using UnityEngine;

namespace Saving {
    public class AutosaveManager : Singleton<AutosaveManager> {
        public float autosaveInterval => Convert.ToSingle((Settings.Autosave.AutosaveInterval.dropdownSelection.selection.Datum));
        public int maxNumberOfAutosaves => (int)Settings.Autosave.NumAutosaves;

#if UNITY_EDITOR
        public bool canMakeAutosave => false;
#else
        public float minTimeBetweenAutosavesSec => 10;
        public bool canMakeAutosave => Settings.Autosave.AutosaveEnabled && timeSinceLastAutosave > minTimeBetweenAutosavesSec;
#endif

        public float timeSinceLastAutosave = 0f;

        private const float delayAfterLevelLoadToSave = 1f;
        private const float autosaveDisabledAfterLoadDelay = 30f;

        private void Start() {
            StartCoroutine(RegularAutosave());

            LevelManager.instance.OnActiveSceneChange += () => {
                if (Settings.Autosave.AutosaveOnLevelChange && SaveManager.realtimeSinceLastLoad > autosaveDisabledAfterLoadDelay) {
                    StartCoroutine(TryDoAutosaveDelayed(delayAfterLevelLoadToSave));
                }
            };
        }

        IEnumerator TryDoAutosaveDelayed(float delay) {
            yield return new WaitForSeconds(delay);
            
            TryDoAutosave();
        }

        IEnumerator RegularAutosave() {
            yield return new WaitForSeconds(autosaveInterval);
            
            while (true) {
                if (Settings.Autosave.AutosaveOnTimer) {
                    TryDoAutosave();
                }

                yield return new WaitForSeconds(autosaveInterval);
            }
        }

        public void TryDoAutosave() {
            if (canMakeAutosave) {
                DoAutosave();
            }
        }

        public void DoAutosave() {
            SaveManager.Save(CreateAutosave());
            timeSinceLastAutosave = 0;

            // Delete autosaves until we're at maxNumberOfAutosaves
            SaveFileUtils.ReadAllSavedMetadata();
            Stack<SaveMetadataWithScreenshot> autosaves = new Stack<SaveMetadataWithScreenshot>(SaveFileUtils.allSavesMetadataCache
                .Where(kv => !SaveFileUtils.playerSaveMetadataCache.ContainsKey(kv.Key))
                .ToDictionary()
                .Values
                .OrderByDescending(m => m.metadata.saveTimestamp)
                .ToList());
            int initialCount = autosaves.Count;
            for (int i = maxNumberOfAutosaves; i < initialCount; i++) {
                SaveFileUtils.DeleteSave(autosaves.Pop().metadata.saveFilename);
            }
        }

        public SaveMetadataWithScreenshot CreateAutosave() {
            // Fills in the lowest available # that's not already taken by a save
            string GetNewAutosaveFileDisplayName() {
                DateTime now = DateTime.Now;
                string date = now.ToShortDateString();
                string time = now.ToShortTimeString();
                string desiredSaveFileDisplayName = $"Autosave {date} {time}";
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