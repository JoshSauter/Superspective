using UnityEditor;
using UnityEngine;

// This is a base class for editor settings that are stored in EditorPrefs.
// It will automatically load the value from EditorPrefs the first time the value is accessed,
// and save the value to EditorPrefs when the value is set.
public abstract class EditorSetting {
    public readonly string key;
    protected bool hasLoaded = false;
    
    protected EditorSetting(string key) {
        this.key = key;
    }
    
    public delegate void OnValueChanged();
    public OnValueChanged onValueChanged;
}

public class BoolEditorSetting : EditorSetting {
    private bool _value;
    public bool Value {
        get {
            if (!hasLoaded) {
                _value = EditorPrefs.GetBool(key, _value);
            }
            
            return _value;
        }
        set {
            if (_value == value) return;
            
            _value = value;
            EditorPrefs.SetBool(key, _value);
            onValueChanged?.Invoke();
        }
    }

    public BoolEditorSetting(string key, bool defaultValue) : base(key) {
        _value = defaultValue;
    }
    
    public static implicit operator bool(BoolEditorSetting setting) {
        return setting.Value;
    }
}

public class Vector3EditorSetting : EditorSetting {
    private string xKey => $"{key}.x";
    private string yKey => $"{key}.y";
    private string zKey => $"{key}.z";
    
    private Vector3 _value;

    public Vector3 Value {
        get {
            if (!hasLoaded) {
                float x = EditorPrefs.GetFloat(xKey, _value.x);
                float y = EditorPrefs.GetFloat(yKey, _value.y);
                float z = EditorPrefs.GetFloat(zKey, _value.z);
                _value = new Vector3(x, y, z);
            }
            
            return _value;
        }
        set {
            if (_value == value) return;
            
            _value = value;
            EditorPrefs.SetFloat(xKey, _value.x);
            EditorPrefs.SetFloat(yKey, _value.y);
            EditorPrefs.SetFloat(zKey, _value.z);
            onValueChanged?.Invoke();
        }
    }
    
    public Vector3EditorSetting(string key, Vector3 defaultValue) : base(key) {
        _value = defaultValue;
    }
    
    public static implicit operator Vector3(Vector3EditorSetting setting) {
        return setting.Value;
    }
}

public class StringEditorSetting : EditorSetting {
    private string _value;
    public string Value {
        get {
            if (!hasLoaded) {
                _value = EditorPrefs.GetString(key, _value);
            }
            
            return _value;
        }
        set {
            if (_value == value) return;
            
            _value = value;
            EditorPrefs.SetString(key, _value);
            onValueChanged?.Invoke();
        }
    }

    public StringEditorSetting(string key, string defaultValue) : base(key) {
        _value = defaultValue;
    }
    
    public static implicit operator string(StringEditorSetting setting) {
        return setting.Value;
    }
}