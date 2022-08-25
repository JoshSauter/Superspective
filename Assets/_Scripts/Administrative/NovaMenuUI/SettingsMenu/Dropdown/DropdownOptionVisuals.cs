using System.Collections;
using System.Collections.Generic;
using Nova;
using UnityEngine;

public class DropdownOptionVisuals : ItemVisuals {
    public TextBlock Name;
    public UIBlock Background;

    public void SetSelected(bool selected) {
        if (selected) {
            Background.Color = Color.black;
            Name.Color = Color.white;
        }
        else {
            Background.Color = Color.clear;
            Name.Color = Color.black;
        }
    }
}
