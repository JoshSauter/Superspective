using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  Literally just a way to put notes on an object in the inspector
/// </summary>
public class Notes : MonoBehaviour {
    [TextArea]
    public string notes;
}
