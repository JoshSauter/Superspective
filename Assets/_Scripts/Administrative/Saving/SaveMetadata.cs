using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEngine;

namespace Saving {
    [Serializable]
    class Header {
        public int jsonMetadataByteSize; // Tells us where to stop reading the SaveFileMetadata JSON bytes and start reading the screenshot bytes
    }
    
    public class SaveMetadataWithScreenshot {
        public SaveMetadata metadata;
        public Texture2D screenshot;

        public SaveMetadataWithScreenshot() { }

        public SaveMetadataWithScreenshot(byte[] data, string saveFilename) {
            // Probably not the most efficient to copy this to an array and a List... needed right now for GetRange (only exists for Lists)
            List<byte> allBytes = new List<byte>(data);
            int bytePointer = 0;
            // Reads the data from pointer to pointer+count, increments the pointer by count, and returns the bytes read
            byte[] ReadBytes(int count) {
                if (bytePointer + count > allBytes.Count) {
                    Debug.Log($"About to read bytes from {bytePointer} to {bytePointer + count} out of {allBytes.Count} bytes for {saveFilename}");
                }
                List<byte> bytes = allBytes.GetRange(bytePointer, count);
                bytePointer += count;
                return bytes.ToArray();
            }
            
            ushort headerSize = BitConverter.ToUInt16(ReadBytes(2));
            byte[] headerBytes = ReadBytes(headerSize);
            string headerJson = Encoding.Unicode.GetString(headerBytes.ToArray());
            Header header = JsonUtility.FromJson<Header>(headerJson);

            if (header == null) {
                throw new Exception($"Failed to load save metadata from {saveFilename}, header was null (Save file may be corrupted)");
            }

            byte[] jsonMetadataBytes = ReadBytes(header.jsonMetadataByteSize);
            string jsonMetadata = Encoding.Unicode.GetString(jsonMetadataBytes);
            SaveMetadata metadata = JsonUtility.FromJson<SaveMetadata>(jsonMetadata);

            byte[] screenshotBytes = ReadBytes(data.Length - bytePointer);
            Texture2D screenshotTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false); // TODO: Make consistent with format of screenshot taken. Edit: This doesn't seem to matter?
            screenshotTexture.LoadImage(screenshotBytes);

            this.metadata = metadata;
            this.screenshot = screenshotTexture;
        }
    }
    
    /// <summary>
    /// Metadata containing the relevant information for a save file and a path to its data, to be serialized with a screenshot
    /// </summary>
    [Serializable]
    public class SaveMetadata {
        public string saveFilename; // Name of the save file as it appears on disk. Used in reconstructing the file paths to read metadata/data from
        public string displayName; // Name of the save as presented to the user
        public long saveTimestamp; // DateTime the save was made, in DateTime.Ticks, stored as a long and only used for sorting
        public long lastLoadedTimestamp; // DateTime the save was last loaded, in DateTime.Ticks, stored as a long and only used for autoloading
        public string saveDate; // Date the save was made, stored as simple string, e.g. 11/29/2022
        public string saveTime; // Time the save was made, stored as simple string, e.g. 9:32 PM
        public string levelName; // Display name of the level the player was on when they made the save
        public string version; // Build version of the game when the save was made

        public SaveMetadata() { }

        public SaveMetadata(byte[] data, string saveFilename) {
            List<byte> allBytes = new List<byte>(data);
            int bytePointer = 0;
            // Reads the data from pointer to pointer+count, increments the pointer by count, and returns the bytes read
            byte[] ReadBytes(int count) {
                if (bytePointer + count > allBytes.Count) {
                    Debug.Log($"About to read bytes from {bytePointer} to {bytePointer + count} out of {allBytes.Count} bytes for {saveFilename}");
                }
                List<byte> bytes = allBytes.GetRange(bytePointer, count);
                bytePointer += count;
                return bytes.ToArray();
            }
            
            ushort headerSize = BitConverter.ToUInt16(ReadBytes(2));
            byte[] headerBytes = ReadBytes(headerSize);
            string headerJson = Encoding.Unicode.GetString(headerBytes.ToArray());
            Header header = JsonUtility.FromJson<Header>(headerJson);

            if (header == null) {
                throw new Exception($"Failed to load save metadata from {saveFilename}, header was null (Save file may be corrupted)");
            }

            byte[] jsonMetadataBytes = ReadBytes(header.jsonMetadataByteSize);
            string jsonMetadata = Encoding.Unicode.GetString(jsonMetadataBytes);
            JsonUtility.FromJsonOverwrite(jsonMetadata, this);
        }
    }
}
