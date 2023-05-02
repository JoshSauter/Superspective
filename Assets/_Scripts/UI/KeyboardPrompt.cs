using System;
using System.Collections;
using System.Collections.Generic;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardPrompt : MonoBehaviour {
    public Image promptImage;
    public KeyCode key;
    
    // Start is called before the first frame update
    void Start() {
        if (promptImage == null) {
            promptImage = gameObject.GetOrAddComponent<Image>();
        }
    }

    private bool Dark => Input.GetKey(key);
    private string DarkOrLight => Dark ? "Dark" : "Light";

    private const float DarkColorMultiplier = 0.75f;
    private const float DarkScaleMultiplier = 1.1f;
    private const float LerpMultiplier = 18f;
    private const float PromptImageAlpha = 0.975f;

    // Update is called once per frame
    void Update() {
        ImageName().ForEach(imageName => {
            float lerpValue = LerpMultiplier * Time.deltaTime;
            Color targetColor = (Color.white * (Dark ? DarkColorMultiplier : 1f)).WithAlpha(PromptImageAlpha);
            float targetScale = (Dark ? DarkScaleMultiplier : 1f);
            promptImage.transform.localScale = Vector3.Lerp(promptImage.transform.localScale, Vector3.one * targetScale, lerpValue);
            promptImage.color = Color.Lerp(promptImage.color, targetColor, lerpValue);
            promptImage.sprite = Resources.Load<Sprite>($"Images/UI/ControlPrompts/Keyboard/{DarkOrLight}/{imageName}");
        });
    }

    public Option<string> ImageName() {
        string suffix = DarkOrLight;
        switch (key) {
            case KeyCode.None:
                break;
            case KeyCode.Backspace:
                break;
            case KeyCode.Delete:
                break;
            case KeyCode.Tab:
                break;
            case KeyCode.Clear:
                break;
            case KeyCode.Return:
                break;
            case KeyCode.Pause:
                break;
            case KeyCode.Escape:
                break;
            case KeyCode.Space:
                break;
            case KeyCode.Keypad0:
            case KeyCode.Keypad1:
            case KeyCode.Keypad2:
            case KeyCode.Keypad3:
            case KeyCode.Keypad4:
            case KeyCode.Keypad5:
            case KeyCode.Keypad6:
            case KeyCode.Keypad7:
            case KeyCode.Keypad8:
            case KeyCode.Keypad9:
                return Option<string>.Of($"{key.ToString()[6..]}_Key_{suffix}");
            case KeyCode.KeypadPeriod:
                break;
            case KeyCode.KeypadDivide:
                break;
            case KeyCode.KeypadMultiply:
                break;
            case KeyCode.KeypadMinus:
                break;
            case KeyCode.KeypadPlus:
                break;
            case KeyCode.KeypadEnter:
                break;
            case KeyCode.KeypadEquals:
                break;
            case KeyCode.UpArrow:
                break;
            case KeyCode.DownArrow:
                break;
            case KeyCode.RightArrow:
                break;
            case KeyCode.LeftArrow:
                break;
            case KeyCode.Insert:
                break;
            case KeyCode.Home:
                break;
            case KeyCode.End:
                break;
            case KeyCode.PageUp:
                break;
            case KeyCode.PageDown:
                break;
            case KeyCode.F1:
                break;
            case KeyCode.F2:
                break;
            case KeyCode.F3:
                break;
            case KeyCode.F4:
                break;
            case KeyCode.F5:
                break;
            case KeyCode.F6:
                break;
            case KeyCode.F7:
                break;
            case KeyCode.F8:
                break;
            case KeyCode.F9:
                break;
            case KeyCode.F10:
                break;
            case KeyCode.F11:
                break;
            case KeyCode.F12:
                break;
            case KeyCode.F13:
                break;
            case KeyCode.F14:
                break;
            case KeyCode.F15:
                break;
            case KeyCode.Alpha0:
            case KeyCode.Alpha1:
            case KeyCode.Alpha2:
            case KeyCode.Alpha3:
            case KeyCode.Alpha4:
            case KeyCode.Alpha5:
            case KeyCode.Alpha6:
            case KeyCode.Alpha7:
            case KeyCode.Alpha8:
            case KeyCode.Alpha9:
                return Option<string>.Of($"{key.ToString()[5..]}_Key_{suffix}");
            case KeyCode.Exclaim:
                break;
            case KeyCode.DoubleQuote:
                break;
            case KeyCode.Hash:
                break;
            case KeyCode.Dollar:
                break;
            case KeyCode.Percent:
                break;
            case KeyCode.Ampersand:
                break;
            case KeyCode.Quote:
                break;
            case KeyCode.LeftParen:
                break;
            case KeyCode.RightParen:
                break;
            case KeyCode.Asterisk:
                break;
            case KeyCode.Plus:
                break;
            case KeyCode.Comma:
                break;
            case KeyCode.Minus:
                break;
            case KeyCode.Period:
                break;
            case KeyCode.Slash:
                break;
            case KeyCode.Colon:
                break;
            case KeyCode.Semicolon:
                break;
            case KeyCode.Less:
                break;
            case KeyCode.Equals:
                break;
            case KeyCode.Greater:
                break;
            case KeyCode.Question:
                break;
            case KeyCode.At:
                break;
            case KeyCode.LeftBracket:
                break;
            case KeyCode.Backslash:
                break;
            case KeyCode.RightBracket:
                break;
            case KeyCode.Caret:
                break;
            case KeyCode.Underscore:
                break;
            case KeyCode.BackQuote:
                break;
            case KeyCode.A:
            case KeyCode.B:
            case KeyCode.C:
            case KeyCode.D:
            case KeyCode.E:
            case KeyCode.F:
            case KeyCode.G:
            case KeyCode.H:
            case KeyCode.I:
            case KeyCode.J:
            case KeyCode.K:
            case KeyCode.L:
            case KeyCode.M:
            case KeyCode.N:
            case KeyCode.O:
            case KeyCode.P:
            case KeyCode.Q:
            case KeyCode.R:
            case KeyCode.S:
            case KeyCode.T:
            case KeyCode.U:
            case KeyCode.V:
            case KeyCode.W:
            case KeyCode.X:
            case KeyCode.Y:
            case KeyCode.Z:
                return Option<string>.Of($"{key.ToString()}_Key_{suffix}");
            case KeyCode.LeftCurlyBracket:
                break;
            case KeyCode.Pipe:
                break;
            case KeyCode.RightCurlyBracket:
                break;
            case KeyCode.Tilde:
                break;
            case KeyCode.Numlock:
                break;
            case KeyCode.CapsLock:
                break;
            case KeyCode.ScrollLock:
                break;
            case KeyCode.RightShift:
                break;
            case KeyCode.LeftShift:
                break;
            case KeyCode.RightControl:
                break;
            case KeyCode.LeftControl:
                break;
            case KeyCode.RightAlt:
                break;
            case KeyCode.LeftAlt:
                break;
            case KeyCode.LeftMeta:
                break;
            case KeyCode.LeftWindows:
                break;
            case KeyCode.RightMeta:
                break;
            case KeyCode.RightWindows:
                break;
            case KeyCode.AltGr:
                break;
            case KeyCode.Help:
                break;
            case KeyCode.Print:
                break;
            case KeyCode.SysReq:
                break;
            case KeyCode.Break:
                break;
            case KeyCode.Menu:
                break;
            case KeyCode.Mouse0:
                break;
            case KeyCode.Mouse1:
                break;
            case KeyCode.Mouse2:
                break;
            case KeyCode.Mouse3:
                break;
            case KeyCode.Mouse4:
                break;
            case KeyCode.Mouse5:
                break;
            case KeyCode.Mouse6:
                break;
            default:
                break;
        }

        return new None<string>();
    }
}
