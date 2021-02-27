using System;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class RotateObject : SaveableObject<RotateObject, RotateObject.RotateObjectSave> {
    public bool importantToSave = false;
    UniqueId _id;

    UniqueId id {
        get {
            if (_id == null) _id = GetComponent<UniqueId>();
            return _id;
        }
    }
    public bool useLocalCoordinates = true;

    public float rotationsPerSecondX;
    public float rotationsPerSecondY;
    public float rotationsPerSecondZ;

    // Update is called once per frame
    void Update() {
        float rotX = Time.deltaTime * rotationsPerSecondX * 360;
        float rotY = Time.deltaTime * rotationsPerSecondY * 360;
        float rotZ = Time.deltaTime * rotationsPerSecondZ * 360;

        transform.Rotate(rotX, rotY, rotZ, useLocalCoordinates ? Space.Self : Space.World);
    }

#region Saving
    // Many usages of RotateObject have no gameplay impact and do not need to be saved. Mark importantToSave as true if you want to save it
    public override bool SkipSave => !importantToSave;
    public override string ID => $"RotateObject_{id.uniqueId}";

    [Serializable]
    public class RotateObjectSave : SerializableSaveObject<RotateObject> {
        readonly bool useLocalCoordinates;
        readonly float rotationsPerSecondX;
        readonly float rotationsPerSecondY;
        readonly float rotationsPerSecondZ;

        readonly SerializableQuaternion rotation;
        public RotateObjectSave(RotateObject rotate) : base(rotate) {
            this.useLocalCoordinates = rotate.useLocalCoordinates;
            this.rotationsPerSecondX = rotate.rotationsPerSecondX;
            this.rotationsPerSecondY = rotate.rotationsPerSecondY;
            this.rotationsPerSecondZ = rotate.rotationsPerSecondZ;
            this.rotation = rotate.useLocalCoordinates ? rotate.transform.localRotation : rotate.transform.rotation;
        }
        public override void LoadSave(RotateObject script) {
            script.useLocalCoordinates = this.useLocalCoordinates;
            script.rotationsPerSecondX = this.rotationsPerSecondX;
            script.rotationsPerSecondY = this.rotationsPerSecondY;
            script.rotationsPerSecondZ = this.rotationsPerSecondZ;
            if (script.useLocalCoordinates) {
                script.transform.localRotation = this.rotation;
            }
            else {
                script.transform.rotation = this.rotation;
            }
        }
    }
#endregion
}