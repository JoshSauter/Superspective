using UnityEngine;

public class ColorCoded : MonoBehaviour {
    public enum Colors {
        AnyColor,
        Red,
        Green,
        Blue
    }

    public Colors color = Colors.AnyColor;

    public bool AcceptedColor(ColorCoded otherColorCodedObj) {
        if (color == Colors.AnyColor || otherColorCodedObj == null || otherColorCodedObj.color == Colors.AnyColor)
            return true;
        return color == otherColorCodedObj.color;
    }
}