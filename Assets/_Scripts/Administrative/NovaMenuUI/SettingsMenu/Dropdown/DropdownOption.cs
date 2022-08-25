using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropdownOption {
    public object Datum;
    public string DisplayName;
    
    public static DropdownOption Of(string name, object datum) {
        return new DropdownOption() {
            Datum = datum,
            DisplayName = name
        };
    }
}
