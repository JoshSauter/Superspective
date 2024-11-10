using Nova;
using NovaMenuUI;
using UnityEngine;

public class CustomCursor : MonoBehaviour {
    private readonly Vector2 cursorHotspot = new Vector2(12, 8);
    public Texture2D cursor; // Used as default
    public Texture2D cursorFull; // Used when hovering over something interactable

    // Start is called before the first frame update
    void Start() {
        Cursor.SetCursor(cursor, cursorHotspot, CursorMode.Auto);
    }

    // Update is called once per frame
    void Update() {
        if (Interaction.TryGetActiveReceiver(1, out UIBlockHit hit) && hit.UIBlock.TryGetComponent(out Interactable it) && it != NovaUIBackground.instance.BackgroundInteractable) {
            Cursor.SetCursor(cursorFull, cursorHotspot, CursorMode.Auto);
        }
        else {
            Cursor.SetCursor(cursor, cursorHotspot, CursorMode.Auto);
        }
    }
}
