using System.Collections.Generic;
using LevelManagement;
using SuperspectiveUtils;
using UnityEngine;

namespace Saving {
    public partial class SaveManager {
        // Contains all of the data for the SaveManager, and abstracts away the SaveManager's data structure from the rest of the code
        private class SaveManagerCache {
            // Keeps track of what scene a given SuperspectiveObject ID can be located in
            private readonly Dictionary<string, Levels> levelLookupForId = new Dictionary<string, Levels>();
            private readonly Dictionary<string, Levels> levelLookupForDynamicObjectId = new Dictionary<string, Levels>();
            // Association ID => collection of IDs. Association ID is the unique part of an ID (usually a GUID except for singleton objects which can just be a descriptive string)
            // This is used to connect which scripts are on the same object
            private readonly Dictionary<string, HashSet<string>> associationIds = new Dictionary<string, HashSet<string>>();

#region Add/Remove Methods
            public void AddSuperspectiveObject(SuperspectiveObject superspectiveObj) {
                // Register SuperspectiveObject Level
                string id = superspectiveObj.ID;
                if (levelLookupForId.TryGetValue(id, out Levels alreadyRegisteredLevel) && alreadyRegisteredLevel != superspectiveObj.Level) {
                    Debug.LogError($"ID {id} is already registered in scene {alreadyRegisteredLevel} but is being registered in scene {superspectiveObj.Level}");
                }
                levelLookupForId[id] = superspectiveObj.Level;

                // Register SuperspectiveObject Association ID
                string associationId = superspectiveObj.AssociationID;
                AddAssociationId(associationId, id);
            }

            public void AddDynamicObject(DynamicObject dynamicObj) {
                string id = dynamicObj.ID;
                // Register DynamicObject Level
                if (levelLookupForDynamicObjectId.TryGetValue(id, out Levels alreadyRegisteredLevel) && alreadyRegisteredLevel != dynamicObj.Level) {
                    Debug.LogError($"ID {id} is already registered in scene {alreadyRegisteredLevel} but is being registered in scene {dynamicObj.Level}");
                }
                levelLookupForDynamicObjectId[id] = dynamicObj.Level;
                
                // Register DynamicObject Association ID
                string associationId = dynamicObj.AssociationID;
                AddAssociationId(associationId, id);
            }
            
            public bool RemoveSuperspectiveObject(string id) {
                string associationId = id.GetAssociationId();
                return levelLookupForId.Remove(id) && RemoveAssociationId(associationId, id);
            }
            
            public bool RemoveDynamicObject(string id) {
                string associationId = id.GetAssociationId();
                return levelLookupForDynamicObjectId.Remove(id) && RemoveAssociationId(associationId, id);
            }

            public void Clear() {
                levelLookupForId.Clear();
                levelLookupForDynamicObjectId.Clear();
                associationIds.Clear();
            }

            // Helper methods for adding and removing association IDs
            private void AddAssociationId(string associationId, string id) {
                if (associationIds.TryGetValue(associationId, out HashSet<string> associatedIds)) {
                    associatedIds.Add(id);
                }
                else {
                    associationIds.Add(associationId, new HashSet<string> { id });
                }
            }

            private bool RemoveAssociationId(string associationId, string id) {
                if (associationIds.TryGetValue(associationId, out HashSet<string> associatedIds)) {
                    bool idWasRemoved = associatedIds.Remove(id);
                    if (associatedIds.Count == 0) {
                        associationIds.Remove(associationId);
                    }

                    return idWasRemoved;
                }

                return false;
            }
#endregion
            
            public Levels GetLevelForSuperspectiveObject(string id) {
                return levelLookupForId.TryGetValue(id, out Levels level) ? level : Levels.InvalidLevel;
            }

            public Levels GetLevelForDynamicObject(string id) {
                return levelLookupForDynamicObjectId.TryGetValue(id, out Levels level) ? level : Levels.InvalidLevel;
            }

            public void SetLevelForDynamicObject(string id, Levels level) {
                if (levelLookupForDynamicObjectId.TryGetValue(id, out Levels alreadyRegisteredLevel) && alreadyRegisteredLevel == level) {
                    Debug.LogError($"ID {id} is already registered in scene {alreadyRegisteredLevel}, no need to set it to the same.");
                }
                
                levelLookupForDynamicObjectId[id] = level;
                // DynamicObjects are SuperspectiveObjects too
                levelLookupForId[id] = level;
                
                // Update the level of all associated IDs
                HashSet<string> associatedIds = GetAssociatedIds(id);
                foreach (string associatedId in associatedIds) {
                    levelLookupForId[associatedId] = level;
                }
            }
            
            public HashSet<string> GetAssociatedIds(string id) {
                string associationId = id.GetAssociationId();
                return associationIds.TryGetValue(associationId, out HashSet<string> ids) ? ids : new HashSet<string>();
            }
        }
    }
}