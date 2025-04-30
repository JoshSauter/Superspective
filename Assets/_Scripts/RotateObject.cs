using System;
using Saving;
using SerializableClasses;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(UniqueId))]
public class RotateObject : SuperspectiveObject<RotateObject, RotateObject.RotateObjectSave> {
    public enum RotateMode {
        Simple,
        Random
    }
    public RotateMode rotateMode;
    private bool RandomRotateMode => rotateMode == RotateMode.Random;
    public bool importantToSave = false;
    public bool useLocalCoordinates = true;

    [Header("Starting rotation speeds")]
    public float rotationsPerSecondX;
    public float rotationsPerSecondY;
    public float rotationsPerSecondZ;

    [ShowIf(nameof(RandomRotateMode))]
    public Vector2 xRandomBetween;
    [ShowIf(nameof(RandomRotateMode))]
    public Vector2 yRandomBetween;
    [ShowIf(nameof(RandomRotateMode))]
    public Vector2 zRandomBetween;
    [ShowIf(nameof(RandomRotateMode))]
    public float randomLerpSpeed = 1f;

    // Update is called once per frame
    void Update() {
        if (RandomRotateMode) {
            float GetNextDelta() {
                float rand = 2 * Random.value - 1; // Random value between -1 and 1
                float delta = rand * Time.deltaTime * randomLerpSpeed / 10f;
                return delta;
            }

            rotationsPerSecondX += GetNextDelta();
            rotationsPerSecondX = Mathf.Clamp(rotationsPerSecondX, xRandomBetween.x, xRandomBetween.y);
            rotationsPerSecondY += GetNextDelta();
            rotationsPerSecondY = Mathf.Clamp(rotationsPerSecondY, yRandomBetween.x, yRandomBetween.y);
            rotationsPerSecondZ += GetNextDelta();
            rotationsPerSecondZ = Mathf.Clamp(rotationsPerSecondZ, zRandomBetween.x, zRandomBetween.y);
        }
        
        float rotX = Time.deltaTime * rotationsPerSecondX * 360;
        float rotY = Time.deltaTime * rotationsPerSecondY * 360;
        float rotZ = Time.deltaTime * rotationsPerSecondZ * 360;

        transform.Rotate(rotX, rotY, rotZ, useLocalCoordinates ? Space.Self : Space.World);
    }

#region Saving

    public override void LoadSave(RotateObjectSave save) {
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

        public RotateObjectSave(RotateObject rotate) : base(rotate) {
            this.rotation = rotate.useLocalCoordinates ? rotate.transform.localRotation : rotate.transform.rotation;
        }
    }
#endregion
}