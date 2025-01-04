using System;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class RotateObject : SuperspectiveObject<RotateObject, RotateObject.RotateObjectSave> {
    public bool importantToSave = false;
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

    public override void LoadSave(RotateObjectSave save) {
        useLocalCoordinates = save.useLocalCoordinates;
        rotationsPerSecondX = save.rotationsPerSecondX;
        rotationsPerSecondY = save.rotationsPerSecondY;
        rotationsPerSecondZ = save.rotationsPerSecondZ;
        if (useLocalCoordinates) {
            transform.localRotation = save.rotation;
        }
        else {
            transform.rotation = save.rotation;
        }
    }

    // Many usages of RotateObject have no gameplay impact and do not need to be saved. Mark importantToSave as true if you want to save it
    public override bool SkipSave => !importantToSave;

    [Serializable]
    public class RotateObjectSave : SaveObject<RotateObject> {
        public SerializableQuaternion rotation;
        public float rotationsPerSecondX;
        public float rotationsPerSecondY;
        public float rotationsPerSecondZ;
        public bool useLocalCoordinates;

        public RotateObjectSave(RotateObject rotate) : base(rotate) {
            this.useLocalCoordinates = rotate.useLocalCoordinates;
            this.rotationsPerSecondX = rotate.rotationsPerSecondX;
            this.rotationsPerSecondY = rotate.rotationsPerSecondY;
            this.rotationsPerSecondZ = rotate.rotationsPerSecondZ;
            this.rotation = rotate.useLocalCoordinates ? rotate.transform.localRotation : rotate.transform.rotation;
        }
    }
#endregion
}