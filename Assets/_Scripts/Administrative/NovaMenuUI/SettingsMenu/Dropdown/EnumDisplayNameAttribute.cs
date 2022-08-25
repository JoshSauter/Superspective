using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class EnumDisplayNameAttribute : Attribute {
    public string DisplayName;

    public EnumDisplayNameAttribute(string displayName) {
        this.DisplayName = displayName;
    }
}
