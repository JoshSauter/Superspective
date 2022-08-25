using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LevelManagement;
using SuperspectiveUtils;
using UnityEngine;

namespace Saving {
    public static class SaveFileUtils {
        private static string SavePath => $"{Application.persistentDataPath}/Saves";
        private static string SaveMetadataPath => $"{SavePath}/Metadata";
        public static string SaveDataPath => $"{SavePath}/Data";

        public delegate void SaveUpdateAction();
        public static SaveUpdateAction OnSavesChanged;

        // saveFilename -> SaveMetadataWithScreenshot, only updated since last ReadAllSavedMetadata call
        public static Dictionary<string, SaveMetadataWithScreenshot> saveMetadataCache = new Dictionary<string, SaveMetadataWithScreenshot>();

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
        
        // File types used for metadata and data save files
        private static string MetadataExtension = ".metadata";
        private static string DataExtension = ".save";

        [Serializable]
        class Header {
            public int jsonMetadataByteSize; // Tells us where to stop reading the SaveFileMetadata JSON bytes and start reading the screenshot bytes
        }

        // /// <summary>
        // /// Takes care of creating/writing the metadata and data to their respective locations
        // /// </summary>
        // /// <param name="saveFilename">Appended to SaveMetadataPath and SaveDataPath to determine the save locations
        // /// of the metadata and data files respectively
        // /// </param>
        // public static void WriteSaveToDisk(string saveFilename, string displayName) {
        //     // Handle data save construction and saving to disk
        //     SaveData.CreateSaveDataFromCurrentState().WriteToDisk(saveFilename);
        //
        //     Option<SaveMetadataWithScreenshot> preExistingMetadata = saveMetadataCache.ContainsKey(saveFilename) ?
        //         Option<SaveMetadataWithScreenshot>.Of(saveMetadataCache[saveFilename]) :
        //         new None<SaveMetadataWithScreenshot>();
        //
        //     // Handle metadata construction and saving it to disk
        //     SaveMetadataWithScreenshot metadataWithScreenshot = CreateNewSaveMetadataFromCurrentState(saveFilename, displayName);
        //     
        //     WriteMetadataToDisk(metadataWithScreenshot);
        // }

        public static void WriteSaveToDisk(SaveMetadataWithScreenshot saveMetadataWithScreenshot) {
            string saveFilename = saveMetadataWithScreenshot.metadata.saveFilename;
            // Handle data save construction and saving to disk
            SaveData.CreateSaveDataFromCurrentState().WriteToDisk(saveFilename);
            
            WriteMetadataToDisk(saveMetadataWithScreenshot);
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
                saveDate = date,
                saveTime = time,
                levelName = LevelManager.instance.activeSceneName.ToLevel().ToDisplayName(),
            };
            
            Texture2D screenshot = ScreenshotOfPlayerCameraView();
            return new SaveMetadataWithScreenshot() {
                metadata = metadata,
                screenshot = screenshot
            };
        }
        
        private static Texture2D ScreenshotOfPlayerCameraView() {
            Camera cam = Player.instance.playerCam;
            int width = 600;
            int height = 400;
            RenderTexture screenTexture = new RenderTexture(width, height, 16);
            RenderTexture prevCamTexture = cam.targetTexture;
            cam.targetTexture = screenTexture;
            RenderTexture.active = screenTexture;
            cam.Render();
            Texture2D renderedTexture = new Texture2D(width, height);
            renderedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = null;
            cam.targetTexture = prevCamTexture;

            return renderedTexture;
        }

        public static void DeleteSave(string saveFileName) {
            File.Delete(MetadataFilePath(saveFileName));
            File.Delete(DataFilePath(saveFileName));
            
            Debug.Log($"Deleted save {saveFileName}");
            
            OnSavesChanged?.Invoke();
        }

        /// <summary>
        /// Writes only the metadata to the disk, useful for just updating save file display names, or as part of writing a whole save to disk
        /// </summary>
        /// <param name="metadataWithScreenshot">Contains the saveFilename used for creating the file path for saving</param>
        public static void WriteMetadataToDisk(SaveMetadataWithScreenshot metadataWithScreenshot) {
            // Create the directory if it does not yet exist (idempotent)
            Directory.CreateDirectory(SaveMetadataPath);
            
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
            File.WriteAllBytes(saveMetadataFile, bytesToSave.ToArray());
            
            OnSavesChanged?.Invoke();
        }

        
        /// <summary>
        /// Calls ReadMetadataFromDisk for every file in the MetadataPath with the appropriate extension and makes a list out the result
        /// </summary>
        /// <returns></returns>
        public static List<SaveMetadataWithScreenshot> ReadAllSavedMetadata() {
            // Create the directory if it does not yet exist (idempotent)
            Directory.CreateDirectory(SaveMetadataPath);
            string[] files = Directory.GetFiles(SaveMetadataPath, $"*{MetadataExtension}");

            List<SaveMetadataWithScreenshot> result = files.Select(ReadMetadataFromDisk).ToList();
            saveMetadataCache = result.ToDictionary(sm => sm.metadata.saveFilename, sm => sm);

            return result;
        }

        /// <summary>
        /// Reads only the metadata from the disk, useful for populating the SaveMenu, or as part of loading a whole save from disk
        /// </summary>
        /// <param name="saveFilename"></param>
        /// <returns>SaveMetadataWithScreenshot containing the metadata and screenshot saved at the appropriate metadata path + saveFilename</returns>
        private static SaveMetadataWithScreenshot ReadMetadataFromDisk(string saveFilename) {
            // Create the directory if it does not yet exist (idempotent)
            Directory.CreateDirectory(SaveMetadataPath);
            string saveMetadataFilePath = MetadataFilePath(saveFilename);
            byte[] allBytesArray = File.ReadAllBytes(saveMetadataFilePath);
            List<byte> allBytes = allBytesArray.ToList();

            int bytePointer = 0;
            // Reads the data from pointer to pointer+count, increments the pointer by count, and returns the bytes read
            byte[] ReadBytes(int count) {
                List<byte> bytes = allBytes.GetRange(bytePointer, count);
                bytePointer += count;
                return bytes.ToArray();
            }
            
            ushort headerSize = BitConverter.ToUInt16(ReadBytes(2));
            byte[] headerBytes = ReadBytes(headerSize);
            string headerJson = Encoding.Unicode.GetString(headerBytes.ToArray());
            Header header = JsonUtility.FromJson<Header>(headerJson);

            byte[] jsonMetadataBytes = ReadBytes(header.jsonMetadataByteSize);
            string jsonMetadata = Encoding.Unicode.GetString(jsonMetadataBytes);
            SaveMetadata metadata = JsonUtility.FromJson<SaveMetadata>(jsonMetadata);

            byte[] screenshotBytes = ReadBytes(allBytesArray.Length - bytePointer);
            Texture2D screenshotTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false); // TODO: Make consistent with format of screenshot taken. Edit: This doesn't seem to matter?
            screenshotTexture.LoadImage(screenshotBytes);

            return new SaveMetadataWithScreenshot() {
                metadata = metadata,
                screenshot = screenshotTexture
            };
        }
    }
}