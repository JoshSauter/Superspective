using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using LevelManagement;
using ObjectSerializationUtils;
using SuperspectiveUtils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Saving {
    
    /// <summary>
    /// Utility class for handling saving and loading game data to and from disk.
    /// </summary>
    public static class SaveFileUtils {
        // Paths for saving metadata and data
        private static string SavePath => $"{Application.persistentDataPath}/Saves";
        private static string SaveMetadataPath => $"{SavePath}/Metadata";
        public static string SaveDataPath => $"{SavePath}/Data";

        // Delegate for notifying changes in saves
        public delegate void SaveUpdateAction();
        public static SaveUpdateAction OnSavesChanged;

        // Caches for filename -> save metadata and screenshots
        public static Dictionary<string, SaveMetadata> allSavesMetadataCache = new Dictionary<string, SaveMetadata>();
        public static Dictionary<string, SaveMetadata> playerSaveMetadataCache = new Dictionary<string, SaveMetadata>();
        public static Dictionary<string, SaveMetadata> autosavesMetadataCache = new Dictionary<string, SaveMetadata>();
        
        public static Dictionary<string, SaveMetadataWithScreenshot> playerSaveScreenshotCache = new Dictionary<string, SaveMetadataWithScreenshot>();
        public static Dictionary<string, SaveMetadataWithScreenshot> autosaveScreenshotCache = new Dictionary<string, SaveMetadataWithScreenshot>();
        public static Dictionary<string, SaveMetadataWithScreenshot> allSavesScreenshotCache = new Dictionary<string, SaveMetadataWithScreenshot>();
        
        // File extensions for metadata and data files
        private static string MetadataExtension = ".metadata";
        private static string DataExtension = ".save";

        private static string MetadataFilePath(string saveFilename) {
            // If it already ends in .metadata, just return the filename as is
            return saveFilename.EndsWith(MetadataExtension) ?
                saveFilename :
                $"{SaveMetadataPath}/{saveFilename}{MetadataExtension}";
        }

        private static string DataFilePath(string saveFilename) {
            // If it already ends in .data, just return the filename as is
            return saveFilename.EndsWith(DataExtension) ?
                saveFilename :
                $"{SaveDataPath}/{saveFilename}{DataExtension}";
        }
        
        /// <summary>
        /// Sanitizes a string to make it suitable for use as a filename by replacing illegal characters.
        /// </summary>
        public static string SanitizeString(string input) {
            return input
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(":", "_")
                .Replace(" ", "_");
        }

        public static JobHandle WriteSaveToDisk(SaveMetadataWithScreenshot saveMetadataWithScreenshot) {
            try {
                string saveFilename = saveMetadataWithScreenshot.metadata.saveFilename;
                // Handle data save construction and saving to disk
                SaveData saveData = SaveData.CreateSaveDataFromCurrentState();
                if (!saveData.managerScene.serializedSaveObjects.ContainsKey("Player")) {
                    Debug.LogError("Player not found in serialized save objects! Here are the serialized save objects:" + string.Join(", ", saveData.managerScene.serializedSaveObjects.Keys));
                }

                JobHandle writeSaveToDiskJob = WriteToDisk(saveFilename, saveData);

                return WriteMetadataToDisk(saveMetadataWithScreenshot, writeSaveToDiskJob);
            }
            catch (Exception e) {
                Debug.LogError($"Error writing save to disk: {e}");
                return default;
            }
        }

        private static JobHandle WriteToDisk(string saveFileName, SaveData saveData) {
            FixedString512Bytes saveFileNameFixed = new FixedString128Bytes($"{SaveFileUtils.SaveDataPath}/{saveFileName}.save");
            BinaryFormatter bf = new BinaryFormatter();
            byte[] serializedData = saveData.SerializeToByteArray();
            NativeArray<byte> nativeSerializedData = new NativeArray<byte>(serializedData.Length, Allocator.Persistent);
            nativeSerializedData.CopyFrom(serializedData);
            WriteSaveDataJob writeSaveDataJob = new WriteSaveDataJob {
                saveDataBytes = nativeSerializedData,
                saveDataFile = saveFileNameFixed
            };

            JobHandle jobHandle = writeSaveDataJob.Schedule();
            LevelManager.instance.StartCoroutine(WriteDataJobCompleteCallback(jobHandle, nativeSerializedData));
            return jobHandle;
        }

        public static SaveMetadataWithScreenshot CreateNewSaveMetadataFromCurrentState(string saveFilename, string defaultDisplayName) {
            // Handle metadata construction
            DateTime now = DateTime.Now;
            string date = now.ToShortDateString();
            string time = now.ToShortTimeString();
            
            SaveMetadata metadata = new SaveMetadata() {
                saveFilename = saveFilename,
                displayName = defaultDisplayName,
                saveTimestamp = now.Ticks,
                lastLoadedTimestamp = now.Ticks,
                saveDate = date,
                saveTime = time,
                levelName = LevelManager.instance.activeSceneName.ToLevel().ToDisplayName(),
                version = Application.version
            };
            
            Texture2D screenshot = ScreenshotOfPlayerCameraView();
            return new SaveMetadataWithScreenshot() {
                metadata = metadata,
                screenshot = screenshot
            };
        }
        
        public static Texture2D ScreenshotOfPlayerCameraView() {
            Camera cam = Player.instance.PlayerCam;
            int width = 600;
            int height = 400;
            RenderTexture screenTexture = new RenderTexture(width, height, 16);
            RenderTexture prevCamTexture = cam.targetTexture;
            cam.targetTexture = screenTexture;
            RenderTexture.active = screenTexture;
            cam.Render();
            Texture2D renderedTexture = new Texture2D(width, height);
            renderedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            renderedTexture.Apply();
            RenderTexture.active = null;
            cam.targetTexture = prevCamTexture;

            return renderedTexture;
        }

        public static JobHandle DeleteSave(string saveFileName) {
            FixedString512Bytes metadataFilePathFixed = new FixedString512Bytes(MetadataFilePath(saveFileName));
            FixedString512Bytes dataFilePathFixed = new FixedString512Bytes(DataFilePath(saveFileName));
            
            DeleteSaveJob deleteSaveJob = new DeleteSaveJob {
                saveMetadataFile = metadataFilePathFixed,
                saveDataFile = dataFilePathFixed
            };
            
            JobHandle jobHandle = deleteSaveJob.Schedule();
            LevelManager.instance.StartCoroutine(DeleteSaveJobCompleteCallback(jobHandle, saveFileName));
            return jobHandle;
        }

        /// <summary>
        /// Writes only the metadata to the disk, useful for just updating save file display names, or as part of writing a whole save to disk.
        /// Schedules a job to write the metadata to disk, and then starts a coroutine to handle the completion of the job.
        /// Accepts a dependsOn JobHandle to depend on e.g. "Finish writing the data before writing the metadata"
        /// </summary>
        /// <param name="metadataWithScreenshot">Contains the saveFilename used for creating the file path for saving</param>
        /// <param name="dependsOn">JobHandle to depend on e.g. "Finish writing the data before writing the metadata"</param>
        public static JobHandle WriteMetadataToDisk(SaveMetadataWithScreenshot metadataWithScreenshot, JobHandle dependsOn) {
            WriteSaveMetadataJob writeSaveMetadataJob = CreateWriteMetadataToDiskJob(metadataWithScreenshot);
            JobHandle metadataWriteJobHandle = writeSaveMetadataJob.Schedule(dependsOn);
            LevelManager.instance.StartCoroutine(WriteMetadataJobCompleteCallback(metadataWriteJobHandle, writeSaveMetadataJob.metadataBytes));
            return metadataWriteJobHandle;
        }

        /// <summary>
        /// Writes only the metadata to the disk, useful for just updating save file display names, or as part of writing a whole save to disk
        /// Schedules a job to write the metadata to disk, and then starts a coroutine to handle the completion of the job.
        /// </summary>
        /// <param name="metadataWithScreenshot">Contains the saveFilename used for creating the file path for saving</param>
        public static JobHandle WriteMetadataToDisk(SaveMetadataWithScreenshot metadataWithScreenshot) {
            WriteSaveMetadataJob writeSaveMetadataJob = CreateWriteMetadataToDiskJob(metadataWithScreenshot);
            JobHandle metadataWriteJobHandle = writeSaveMetadataJob.Schedule();
            LevelManager.instance.StartCoroutine(WriteMetadataJobCompleteCallback(metadataWriteJobHandle, writeSaveMetadataJob.metadataBytes));
            return metadataWriteJobHandle;
        }

        // Common functionality whether we depend on another job to finish or not
        private static WriteSaveMetadataJob CreateWriteMetadataToDiskJob(SaveMetadataWithScreenshot metadataWithScreenshot) {
            SaveMetadata metadata = metadataWithScreenshot.metadata;
            Texture2D screenshot = metadataWithScreenshot.screenshot;
            
            byte[] jsonMetadataByteArray = Encoding.Unicode.GetBytes(JsonUtility.ToJson(metadata));
            byte[] screenshotByteArray = screenshot.EncodeToPNG();

            Header header = new Header() {
                jsonMetadataByteSize = jsonMetadataByteArray.Length
            };
            string headerJson = JsonUtility.ToJson(header);
            byte[] headerJsonByteArray = Encoding.Unicode.GetBytes(headerJson);

            ushort headerSize = (ushort)headerJsonByteArray.Length;
            byte[] headerSizeByteArray = BitConverter.GetBytes(headerSize);

            List<byte> bytesToSave = new List<byte>();
            bytesToSave.AddRange(headerSizeByteArray);
            bytesToSave.AddRange(headerJsonByteArray);
            bytesToSave.AddRange(jsonMetadataByteArray);
            bytesToSave.AddRange(screenshotByteArray);
            
            string saveMetadataFile = MetadataFilePath(metadataWithScreenshot.metadata.saveFilename);
            
            FixedString512Bytes saveMetadataFileFixed = new FixedString512Bytes(saveMetadataFile);
            NativeArray<byte> nativeBytesToSave = new NativeArray<byte>(bytesToSave.ToArray(), Allocator.Persistent);
            return new WriteSaveMetadataJob {
                metadataBytes = nativeBytesToSave,
                saveMetadataFile = saveMetadataFileFixed
            };
        }
        
        /// <summary>
        /// Calls ReadMetadataFromDisk for every file in the MetadataPath with the appropriate extension and makes a list out the result
        /// </summary>
        /// <returns></returns>
        public static JobHandle ReadAllSavedMetadataWithScreenshot(JobHandle dependsOn, Action<List<SaveMetadataWithScreenshot>> onReadComplete) {
            // Create the directory if it does not yet exist (idempotent)
            Directory.CreateDirectory(SaveMetadataPath);
            string[] files = Directory.GetFiles(SaveMetadataPath, $"*{MetadataExtension}");

            List<SaveMetadataWithScreenshot> result = new List<SaveMetadataWithScreenshot>();
            NativeArray<JobHandle> jobs = new NativeArray<JobHandle>(files.Select(f => ReadMetadataWithScreenshotFromDisk(dependsOn, f, (SaveMetadataWithScreenshot metadataWithScreenshot) => {
                result.Add(metadataWithScreenshot);
            })).ToArray(), Allocator.Persistent);
            
            JobHandle allJobs = JobHandle.CombineDependencies(jobs);
            LevelManager.instance.StartCoroutine(ReadAllMetadataJobCompleteCallback(allJobs, () => {
                ClearCachedTextures(allSavesScreenshotCache.Values.Select(m => m.screenshot).ToList());
                
                allSavesScreenshotCache = result.ToDictionary(sm => sm.metadata.saveFilename, sm => sm);
                playerSaveScreenshotCache = result.Where(sm => !sm.metadata.saveFilename.StartsWith("Autosave")).ToDictionary(sm => sm.metadata.saveFilename, sm => sm);
                autosaveScreenshotCache = result.Where(sm => sm.metadata.saveFilename.StartsWith("Autosave")).ToDictionary(sm => sm.metadata.saveFilename, sm => sm);

                allSavesMetadataCache = allSavesScreenshotCache.MapValues(sm => sm.metadata);
                playerSaveMetadataCache = playerSaveScreenshotCache.MapValues(sm => sm.metadata);
                autosavesMetadataCache = autosaveScreenshotCache.MapValues(sm => sm.metadata);
                onReadComplete(result);
            }));

            return allJobs;
        }
        
        public static JobHandle ReadAllSavedMetadataWithScreenshot(Action<List<SaveMetadataWithScreenshot>> onReadComplete) {
            // Create the directory if it does not yet exist (idempotent)
            Directory.CreateDirectory(SaveMetadataPath);
            string[] files = Directory.GetFiles(SaveMetadataPath, $"*{MetadataExtension}");

            // Create a list of MetadataWithScreenshot objects to store the results as they come in
            List<SaveMetadataWithScreenshot> result = new List<SaveMetadataWithScreenshot>();
            NativeArray<JobHandle> jobs = new NativeArray<JobHandle>(files.Select(f => ReadMetadataWithScreenshotFromDisk(f, (SaveMetadataWithScreenshot metadataWithScreenshot) => {
                result.Add(metadataWithScreenshot);
            })).ToArray(), Allocator.Persistent);
            
            JobHandle allJobs = JobHandle.CombineDependencies(jobs);
            LevelManager.instance.StartCoroutine(ReadAllMetadataJobCompleteCallback(allJobs, () => {
                ClearCachedTextures(allSavesScreenshotCache.Values.Select(m => m.screenshot).ToList());
                
                allSavesScreenshotCache = result.ToDictionary(sm => sm.metadata.saveFilename, sm => sm);
                playerSaveScreenshotCache = result.Where(sm => !sm.metadata.saveFilename.StartsWith("Autosave")).ToDictionary(sm => sm.metadata.saveFilename, sm => sm);
                autosaveScreenshotCache = result.Where(sm => sm.metadata.saveFilename.StartsWith("Autosave")).ToDictionary(sm => sm.metadata.saveFilename, sm => sm);

                allSavesMetadataCache = allSavesScreenshotCache.MapValues(sm => sm.metadata);
                playerSaveMetadataCache = playerSaveScreenshotCache.MapValues(sm => sm.metadata);
                autosavesMetadataCache = autosaveScreenshotCache.MapValues(sm => sm.metadata);
                onReadComplete(result);
            }));

            return allJobs;
        }

        public static JobHandle ReadAllSavedMetadata(JobHandle dependsOn, Action<List<SaveMetadata>> onReadComplete) {
            // Create the directory if it does not yet exist (idempotent)
            Directory.CreateDirectory(SaveMetadataPath);
            string[] files = Directory.GetFiles(SaveMetadataPath, $"*{MetadataExtension}");

            // Create a list of MetadataWithScreenshot objects to store the results as they come in
            List<SaveMetadata> result = new List<SaveMetadata>();
            NativeArray<JobHandle> jobs = new NativeArray<JobHandle>(files.Select(f => ReadMetadataFromDisk(dependsOn, f, (SaveMetadata metadata) => {
                result.Add(metadata);
            })).ToArray(), Allocator.Persistent);
            
            JobHandle allJobs = JobHandle.CombineDependencies(jobs);
            LevelManager.instance.StartCoroutine(ReadAllMetadataJobCompleteCallback(allJobs, () => {
                foreach (SaveMetadata metadata in result) {
                    if (allSavesScreenshotCache.ContainsKey(metadata.saveFilename)) {
                        allSavesScreenshotCache[metadata.saveFilename].metadata = metadata;
                    }
                    if (playerSaveScreenshotCache.ContainsKey(metadata.saveFilename)) {
                        playerSaveScreenshotCache[metadata.saveFilename].metadata = metadata;
                    }
                    if (autosaveScreenshotCache.ContainsKey(metadata.saveFilename)) {
                        autosaveScreenshotCache[metadata.saveFilename].metadata = metadata;
                    }
                }
                
                allSavesMetadataCache = result.ToDictionary(sm => sm.saveFilename, sm => sm);
                playerSaveMetadataCache = result.Where(sm => !sm.saveFilename.StartsWith("Autosave")).ToDictionary(sm => sm.saveFilename, sm => sm);
                autosavesMetadataCache = result.Where(sm => sm.saveFilename.StartsWith("Autosave")).ToDictionary(sm => sm.saveFilename, sm => sm);
                onReadComplete(result);
            }));

            return allJobs;
        }
        
        public static JobHandle ReadAllSavedMetadata(Action<List<SaveMetadata>> onReadComplete) {
            // Create the directory if it does not yet exist (idempotent)
            Directory.CreateDirectory(SaveMetadataPath);
            string[] files = Directory.GetFiles(SaveMetadataPath, $"*{MetadataExtension}");
            
            List<SaveMetadata> result = new List<SaveMetadata>();
            NativeArray<JobHandle> jobs = new NativeArray<JobHandle>(files.Select(f => ReadMetadataFromDisk(f, (SaveMetadata metadata) => {
                result.Add(metadata);
            })).ToArray(), Allocator.Persistent);
            JobHandle allJobs = JobHandle.CombineDependencies(jobs);
            LevelManager.instance.StartCoroutine(ReadAllMetadataJobCompleteCallback(allJobs, () => {
                foreach (SaveMetadata metadata in result) {
                    if (allSavesScreenshotCache.ContainsKey(metadata.saveFilename)) {
                        allSavesScreenshotCache[metadata.saveFilename].metadata = metadata;
                    }
                    if (playerSaveScreenshotCache.ContainsKey(metadata.saveFilename)) {
                        playerSaveScreenshotCache[metadata.saveFilename].metadata = metadata;
                    }
                    if (autosaveScreenshotCache.ContainsKey(metadata.saveFilename)) {
                        autosaveScreenshotCache[metadata.saveFilename].metadata = metadata;
                    }
                }
                
                allSavesMetadataCache = result.ToDictionary(sm => sm.saveFilename, sm => sm);
                playerSaveMetadataCache = result.Where(sm => !sm.saveFilename.StartsWith("Autosave")).ToDictionary(sm => sm.saveFilename, sm => sm);
                autosavesMetadataCache = result.Where(sm => sm.saveFilename.StartsWith("Autosave")).ToDictionary(sm => sm.saveFilename, sm => sm);
                onReadComplete(result);
            }));

            return allJobs;
        }

        private static JobHandle ReadMetadataFromDisk(string saveFilename, Action<SaveMetadata> onComplete) {
            ReadMetadataJob readMetadataJob = CreateReadMetadataJob(saveFilename);
            JobHandle readMetadataJobHandle = readMetadataJob.Schedule();
            LevelManager.instance.StartCoroutine(ReadMetadataJobCompleteCallback(readMetadataJobHandle, readMetadataJob.metadataBytes, saveFilename, onComplete));
            return readMetadataJobHandle;
        }

        private static JobHandle ReadMetadataFromDisk(JobHandle dependsOn, string saveFilename, Action<SaveMetadata> onComplete) {
            ReadMetadataJob readMetadataJob = CreateReadMetadataJob(saveFilename);
            JobHandle readMetadataJobHandle = readMetadataJob.Schedule(dependsOn);
            LevelManager.instance.StartCoroutine(ReadMetadataJobCompleteCallback(readMetadataJobHandle, readMetadataJob.metadataBytes, saveFilename, onComplete));
            return readMetadataJobHandle;
        }
        

        /// <summary>
        /// Reads only the metadata from the disk, useful for populating the SaveMenu, or as part of loading a whole save from disk
        /// </summary>
        /// <param name="saveFilename"></param>
        /// <returns>SaveMetadataWithScreenshot containing the metadata and screenshot saved at the appropriate metadata path + saveFilename</returns>
        private static JobHandle ReadMetadataWithScreenshotFromDisk(JobHandle dependsOn, string saveFilename, Action<SaveMetadataWithScreenshot> onComplete) {
            ReadMetadataJob readMetadataJob = CreateReadMetadataJob(saveFilename);
            JobHandle readMetadataJobHandle = readMetadataJob.Schedule(dependsOn);
            LevelManager.instance.StartCoroutine(ReadMetadataJobWithScreenshotCompleteCallback(readMetadataJobHandle, readMetadataJob.metadataBytes, saveFilename, onComplete));
            return readMetadataJobHandle;
        }

        private static JobHandle ReadMetadataWithScreenshotFromDisk(string saveFilename, Action<SaveMetadataWithScreenshot> onComplete) {
            ReadMetadataJob readMetadataJob = CreateReadMetadataJob(saveFilename);
            JobHandle readMetadataJobHandle = readMetadataJob.Schedule();
            LevelManager.instance.StartCoroutine(ReadMetadataJobWithScreenshotCompleteCallback(readMetadataJobHandle, readMetadataJob.metadataBytes, saveFilename, onComplete));
            return readMetadataJobHandle;
        }

        private static ReadMetadataJob CreateReadMetadataJob(string saveFilename) {
            string saveMetadataFilePath = MetadataFilePath(saveFilename);
            FileInfo fileInfo = new FileInfo(saveMetadataFilePath);
            
            // Warning: This will break for files > 256 MB since we're converting the long into int here
            NativeArray<byte> metadataBytes = new NativeArray<byte>((int)fileInfo.Length, Allocator.Persistent);
            return new ReadMetadataJob {
                metadataBytes = metadataBytes,
                saveMetadataPathFixed = new FixedString512Bytes(saveMetadataFilePath)
            };
        }

        private static void ClearCachedTextures(List<Texture2D> screenshotTextures) {
            LevelManager.instance.StartCoroutine(ClearCachedTexturesAfterDelay(screenshotTextures, 1f));
        }

        private static IEnumerator ClearCachedTexturesAfterDelay(List<Texture2D> screenshotTextures, float delay) {
            yield return new WaitForSeconds(delay);

            screenshotTextures.ForEach(UnityEngine.Object.Destroy);
        }

        public static bool IsCompatibleWith(string version1, string version2) {
            if (version1 == null || version2 == null) return false;
            
            return version1.Equals(version2);
        }

        private static IEnumerator WriteDataJobCompleteCallback(JobHandle jobHandle, NativeArray<byte> dataBytes) {
            yield return new WaitUntil(() => jobHandle.IsCompleted);
            
            jobHandle.Complete();
            dataBytes.Dispose();
        }
        
        private static IEnumerator WriteMetadataJobCompleteCallback(JobHandle jobHandle, NativeArray<byte> metadataBytes) {
            yield return new WaitUntil(() => jobHandle.IsCompleted);
            
            jobHandle.Complete();
            metadataBytes.Dispose();
            
            OnSavesChanged?.Invoke();
        }
        
        private static IEnumerator DeleteSaveJobCompleteCallback(JobHandle jobHandle, string saveFileName) {
            yield return new WaitUntil(() => jobHandle.IsCompleted);
            
            jobHandle.Complete();
            
            Debug.Log($"Deleted save {saveFileName}");
            OnSavesChanged?.Invoke();
        }

        private static IEnumerator ReadMetadataJobCompleteCallback(JobHandle jobHandle, NativeArray<byte> metadataBytes, string saveFilename, Action<SaveMetadata> callback) {
            yield return new WaitUntil(() => jobHandle.IsCompleted);
            
            jobHandle.Complete();

            if (callback != null) {
                try {
                    byte[] allBytesArray = metadataBytes.ToArray();
                    SaveMetadata metadata = new SaveMetadata(allBytesArray, saveFilename);

                    callback.Invoke(metadata);
                }
                catch (Exception e) {
                    Debug.LogError($"Error reading metadata from disk: {e}");
                }
            }

            if (metadataBytes.IsCreated) {
                metadataBytes.Dispose();
            }
        }
        
        private static IEnumerator ReadMetadataJobWithScreenshotCompleteCallback(JobHandle jobHandle, NativeArray<byte> metadataBytes, string saveFilename, Action<SaveMetadataWithScreenshot> callback) {
            yield return new WaitUntil(() => jobHandle.IsCompleted);
            
            jobHandle.Complete();

            if (callback != null) {
                try {
                    byte[] allBytesArray = metadataBytes.ToArray();
                    SaveMetadataWithScreenshot metadataWithScreenshot = new SaveMetadataWithScreenshot(allBytesArray, saveFilename);

                    callback.Invoke(metadataWithScreenshot);
                }
                catch (Exception e) {
                    Debug.LogError($"Error reading metadata from disk: {e}");
                }
            }
            
            if (metadataBytes.IsCreated) {
                metadataBytes.Dispose();
            }
        }

        private static IEnumerator ReadAllMetadataJobCompleteCallback(JobHandle combinedJobHandle, Action callback) {
            yield return new WaitUntil(() => combinedJobHandle.IsCompleted);
            
            combinedJobHandle.Complete();
            
            callback.Invoke();
        }
    }
    
    struct WriteSaveDataJob : IJob {
        public NativeArray<byte> saveDataBytes;
        public FixedString512Bytes saveDataFile;

        public void Execute() {
            string saveDataFilePath = saveDataFile.ConvertToString();
            string saveDataDirectory = string.Join("/", saveDataFilePath.Split("/").SkipLast(1));
            Directory.CreateDirectory(saveDataDirectory);
            using FileStream file = File.Create(saveDataFilePath);
            // Write the bytes from the NativeArray to the file
            file.Write(saveDataBytes.ToArray(), 0, saveDataBytes.Length);
        }
    }

    struct WriteSaveMetadataJob : IJob {
        public NativeArray<byte> metadataBytes;
        public FixedString512Bytes saveMetadataFile;

        public void Execute() {
            string saveMetadataFilePath = saveMetadataFile.ConvertToString();
            string saveMetadataDirectory = string.Join("/", saveMetadataFilePath.Split("/").SkipLast(1));
                
            // Create the directory if it does not yet exist (idempotent)
            Directory.CreateDirectory(saveMetadataDirectory);
            File.WriteAllBytes(saveMetadataFilePath, metadataBytes.ToArray());
        }
    }
    
    struct DeleteSaveJob : IJob {
        public FixedString512Bytes saveDataFile;
        public FixedString512Bytes saveMetadataFile;

        public void Execute() {
            string saveDataFilePath = saveDataFile.ConvertToString();
            string saveMetadataFilePath = saveMetadataFile.ConvertToString();
            File.Delete(saveDataFilePath);
            File.Delete(saveMetadataFilePath);
        }
    }

    struct ReadMetadataJob : IJob {
        public FixedString512Bytes saveMetadataPathFixed;
        public NativeArray<byte> metadataBytes;

        public void Execute() {
            string saveMetadataPath = saveMetadataPathFixed.ConvertToString();
            byte[] allBytesArray = File.ReadAllBytes(saveMetadataPath);
            metadataBytes.CopyFrom(allBytesArray);
        }
    }
}