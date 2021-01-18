using System;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class ObjectScale : MonoBehaviour, SaveableObject {
    public float minSize = 1f;
    public float maxSize = 1f;
    public float period = 3f;
    UniqueId _id;

    Vector3 startScale;

    float timeElapsed;

    UniqueId id {
        get {
            if (_id == null) _id = GetComponent<UniqueId>();
            return _id;
        }
    }

    // Update is called once per frame
    void Update() {
        timeElapsed += Time.deltaTime;
        transform.localScale = startScale.normalized * startScale.magnitude * Mathf.Lerp(
            minSize,
            maxSize,
            Mathf.Cos(timeElapsed * 2 * Mathf.PI / period) * 0.5f + 0.5f
        );
    }

    void OnEnable() {
        startScale = transform.localScale;
    }

    void OnDisable() {
        transform.localScale = startScale;
        timeElapsed = 0;
    }

#region Saving
    public bool SkipSave { get; set; }

    // All components on PickupCubes share the same uniqueId so we need to qualify with component name
    public string ID => $"ObjectScale_{id.uniqueId}";

    [Serializable]
    class ObjectScaleSave {
        float maxSize;
        float minSize;
        float period;
        SerializableVector3 scale;

        SerializableVector3 startScale;

        float timeElapsed;

        public ObjectScaleSave(ObjectScale script) {
            minSize = script.minSize;
            maxSize = script.maxSize;
            period = script.period;
            startScale = script.startScale;
            timeElapsed = script.timeElapsed;
            scale = script.transform.localScale;
        }

        public void LoadSave(ObjectScale script) {
            script.minSize = minSize;
            script.maxSize = maxSize;
            script.period = period;
            script.startScale = startScale;
            script.timeElapsed = timeElapsed;
            script.transform.localScale = scale;
        }
    }

    public object GetSaveObject() {
        return new ObjectScaleSave(this);
    }

    public void LoadFromSavedObject(object savedObject) {
        ObjectScaleSave save = savedObject as ObjectScaleSave;

        save.LoadSave(this);
    }
#endregion
}