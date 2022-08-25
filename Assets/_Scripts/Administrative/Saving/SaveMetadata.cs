using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Saving {
    public class SaveMetadataWithScreenshot {
        public SaveMetadata metadata;
        public Texture2D screenshot;
    }
    
    /// <summary>
    /// Metadata containing the relevant information for a save file and a path to its data, to be serialized with a screenshot
    /// </summary>
    [Serializable]
    public class SaveMetadata {
        public string saveFilename; // Name of the save file as it appears on disk. Used in reconstructing the file paths to read metadata/data from
        public string displayName; // Name of the save as presented to the user
        public long saveTimestamp; // DateTime the save was made, in DateTime.Ticks, stored as a long and only used for sorting
        public string saveDate; // Date the save was made, stored as simple string, e.g. 11/29/2022
        public string saveTime; // Time the save was made, stored as simple string, e.g. 9:32 PM
        public string levelName; // Display name of the level the player was on when they made the save
    }
}
