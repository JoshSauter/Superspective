using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace NovaMenuUI {
    /// <summary>
    /// Provides text input via keyboard to a <see cref="TextField"/>.
    /// </summary>
    public class TextFieldKeyboardInput : TextFieldInputProvider {
        public event Action OnSubmit = null;

        [SerializeField]
        [Tooltip("If true, will fire submit events whenever the return key is pressed.")]
        private bool fireSubmitEvents = false;

        private Coroutine inputRoutine = null;

        /// <summary>
        /// Starts the input loop when the text field gains focus.
        /// </summary>
        protected override void HandleFocused() {
            inputRoutine = StartCoroutine(InputLoop());
        }

        /// <summary>
        /// Stops the input loop when the text field loses focus.
        /// </summary>
        protected override void HandleFocusLost() {
            if (inputRoutine != null) {
                if (fireSubmitEvents) {
                    OnSubmit?.Invoke();
                }

                StopCoroutine(inputRoutine);
                inputRoutine = null;
            }
        }

        private IEnumerator InputLoop() {
            // Don't process the input on the frame that we gain focus
            yield return null;

            Event cachedEvent = new Event();

            // Loop indefinitely, the loop will stop when the coroutine is stopped.
            while (true) {
                while (Event.PopEvent(cachedEvent)) {
                    if (cachedEvent.type != EventType.KeyDown) {
                        continue;
                    }

                    if (!ProcessKeyPress(cachedEvent)) {
                        break;
                    }
                }

                // Wait for the next frame
                yield return null;
            }
        }

        /// <summary>
        /// Returns true if should continue
        /// </summary>
        private bool ProcessKeyPress(Event evt) {
            var currentEventModifiers = evt.modifiers;

            // Get the modifiers for the event
            bool ctrl = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX ? (currentEventModifiers & EventModifiers.Command) != 0 : (currentEventModifiers & EventModifiers.Control) != 0;
            bool shift = (currentEventModifiers & EventModifiers.Shift) != 0;
            bool alt = (currentEventModifiers & EventModifiers.Alt) != 0;
            bool ctrlOnly = ctrl && !alt && !shift;

            // Process the "special" keys
            switch (evt.keyCode) {
                case KeyCode.KeypadEnter:
                case KeyCode.Return: {
                    if (!fireSubmitEvents || shift) {
                        break;
                    }

                    OnSubmit?.Invoke();
                    return false;
                }
                case KeyCode.Backspace: {
                    inputField.DeleteLeft();
                    return true;
                }
                case KeyCode.Delete: {
                    inputField.DeleteRight();
                    return true;
                }
                case KeyCode.Home: {
                    inputField.MoveCursor(inputField.CursorPosition.MoveToStartOfLine(), shift);
                    return true;
                }
                case KeyCode.End: {
                    inputField.MoveCursor(inputField.CursorPosition.MoveToEndOfLine(), shift);
                    return true;
                }
                case KeyCode.LeftArrow: {
                    if (ctrl) {
                        inputField.MoveCursor(inputField.CursorPosition.MoveLeft().MoveToStartOfWord(), shift);
                    }
                    else {
                        inputField.MoveCursor(inputField.CursorPosition.MoveLeft(), shift);
                    }

                    return true;
                }
                case KeyCode.RightArrow: {
                    if (ctrl) {
                        inputField.MoveCursor(inputField.CursorPosition.MoveToEndOfWord().MoveRight(), shift);
                    }
                    else {
                        inputField.MoveCursor(inputField.CursorPosition.MoveRight(), shift);
                    }

                    return true;
                }
                case KeyCode.UpArrow: {
                    inputField.MoveCursor(inputField.CursorPosition.MoveUp(), shift);
                    return true;
                }
                case KeyCode.DownArrow: {
                    inputField.MoveCursor(inputField.CursorPosition.MoveDown(), shift);
                    return true;
                }
                case KeyCode.A: {
                    if (ctrlOnly) {
                        inputField.SetCursorPosition(inputField.CursorPosition.MoveToEnd(), inputField.CursorPosition.MoveToStart());
                        return true;
                    }

                    break;
                }
                case KeyCode.C: {
                    if (ctrlOnly) {
                        GUIUtility.systemCopyBuffer = inputField.GetSelectedString();
                        return true;
                    }

                    break;
                }
                case KeyCode.V: {
                    if (ctrlOnly) {
                        inputField.Insert(GUIUtility.systemCopyBuffer);
                        return true;
                    }

                    break;
                }
                case KeyCode.X: {
                    if (ctrlOnly) {
                        GUIUtility.systemCopyBuffer = inputField.GetSelectedString();
                        inputField.DeleteSelection();
                        return true;
                    }

                    break;
                }

                case KeyCode.Escape: {
                    inputField.ClearTextSelection();
                    return false;
                }
            }

            // If we got to here, it wasn't a special character, so try to insert
            // it into the input field
            char c = evt.character;

            if (c == '\r' || (int)c == 3) {
                // Convert carriage return and end-of-text characters to newline.
                c = '\n';
            }

            if (!IsValidChar(c)) {
                return true;
            }

            // Insert the text into the input field
            inputField.Insert(c);
            return true;
        }
    }
}
