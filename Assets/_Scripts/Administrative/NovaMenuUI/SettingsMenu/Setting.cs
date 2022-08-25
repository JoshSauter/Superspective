using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Setting : SettingsItem {
    public string Key;

    public abstract bool IsEqual(Setting other);

    public abstract void CopySettingsFrom(Setting other);
}
