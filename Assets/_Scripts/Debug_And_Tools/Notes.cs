using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Literally just a way to put notes on an object in the inspector.
/// Now dynamically determines the number of lines based on content.
/// </summary>
public class Notes : MonoBehaviour {
#if UNITY_EDITOR
    private const int MIN_LINE_COUNT = 2;
    private const int MAX_LINE_COUNT = 20;
    
    [Title("Notes")]
    [HideLabel]
    [SerializeField, TextArea(MIN_LINE_COUNT, MAX_LINE_COUNT)]
    public string notes = "";
#endif
}
