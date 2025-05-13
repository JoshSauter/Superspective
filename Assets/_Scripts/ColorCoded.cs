using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class ColorCoded : MonoBehaviour {
    public enum Colors {
        AnyColor,
        Red,
        Green,
        Blue,
        Purple,
        Orange
    }

    [GUIColor(nameof(ColorValue))]
    public Colors color = Colors.AnyColor;

    public Color ColorValue {
        get {
            switch (color) {
                case Colors.AnyColor:
                    return Color.white;
                case Colors.Red:
                    return new Color(1f, .35f, .35f);
                case Colors.Green:
                    return new Color(0.35f, 1f, .5f);
                case Colors.Blue:
                    return new Color(0.35f, .55f, 1f);
                case Colors.Purple:
                    return new Color(0.65f, .35f, 1f);
                case Colors.Orange:
                    return new Color(1f, .75f, .35f);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public bool AcceptedColor(ColorCoded otherColorCodedObj) {
        if (color == Colors.AnyColor || otherColorCodedObj == null || otherColorCodedObj.color == Colors.AnyColor)
            return true;
        return color == otherColorCodedObj.color;
    }
}
