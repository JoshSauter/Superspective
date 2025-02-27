using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
///  Literally just a way to put notes on an object in the inspector
/// </summary>
public class Notes : MonoBehaviour {
    [Title("Notes")] // Optional: Adds a title for better organization
    [MultiLineProperty(10)] // Sets the height of the text box to fit ~10 lines
    [HideLabel] // Removes the label to make it cleaner
    public string notes;
}
