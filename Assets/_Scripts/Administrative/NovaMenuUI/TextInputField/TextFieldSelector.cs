using System;
using System.Collections;
using System.Collections.Generic;
using Nova;
using UnityEngine;

namespace NovaMenuUI {
    /// <summary>
    /// Handles focus/unfocus logic for a <see cref="SuperspectiveTextField"/> using Nova's
    /// <see cref="Gesture"/> system. Also handles drag-to-select within the <see cref="SuperspectiveTextField"/>.
    /// </summary>
    /// <remarks>
    /// This is meant to serve as an example or a stub which can be extended depending
    /// on how your app/game handles focus state.
    /// </remarks>
    [RequireComponent(typeof(Interactable))]
    public class TextFieldSelector : MonoBehaviour {
        /// <summary>
        /// Fires when the associated <see cref="SuperspectiveTextField"/> gains focus.
        /// </summary>
        public event Action OnFocused = null;

        /// <summary>
        /// Fires when the associated <see cref="SuperspectiveTextField"/> loses focus.
        /// </summary>
        public event Action OnFocusLost = null;

        [SerializeField]
        [Tooltip("The TextField for which to provide focus/unfocus logic.")]
        private SuperspectiveTextField inputField = null;

        [SerializeField]
        [Tooltip("If true, whenever the TextField gains focus, all of the text will be highlighted.")]
        public bool SelectAllOnFocus = true;

        [SerializeField]
        [Tooltip("If two clicks occur on the TextField within this duration, the word at the clicked location will be highlighted")]
        private float wordSelectDoubleClickDelay = .25f;

        /// <summary>
        /// The <see cref="UIBlock"/> on this game object,
        /// which is how we'll subscribe to gesture events.
        /// </summary>
        private UIBlock uiBlock = null;

        /// <summary>
        /// Is this the actively focused <see cref="TextFieldSelector"/>?
        /// </summary>
        private bool isFocused = false;

        /// <summary>
        /// The last time this <see cref="TextFieldSelector"/> was clicked.
        /// </summary>
        private float lastClickTime = 0f;

        private void OnEnable() {
            if (uiBlock == null) {
                // Interactable is guaranteed to be there
                // because it's a required component
                uiBlock = GetComponent<Interactable>().UIBlock;
            }

            // Subscribe to events to handle different selection/focus changes
            uiBlock.AddGestureHandler<Gesture.OnClick>(HandleClick);
            uiBlock.AddGestureHandler<Gesture.OnPress>(HandlePress);
            uiBlock.AddGestureHandler<Gesture.OnDrag>(HandleDrag);
            NovaInputManager.OnPostClick += HandlePostClick;
        }

        private void OnDisable() {
            uiBlock.RemoveGestureHandler<Gesture.OnClick>(HandleClick);
            uiBlock.RemoveGestureHandler<Gesture.OnPress>(HandlePress);
            uiBlock.RemoveGestureHandler<Gesture.OnDrag>(HandleDrag);
            NovaInputManager.OnPostClick -= HandlePostClick;
        }

        /// <summary>
        /// Handles the drag event, moving the cursor to the new position.
        /// </summary>
        private void HandleDrag(Gesture.OnDrag evt) {
            if (!isFocused) {
                return;
            }

            if (TextPosition.TryCreate(inputField.TextBlock, evt.Interaction.Ray, out TextPosition pos)) {
                // Move the cursor with highlight
                inputField.MoveCursor(pos, true);
            }
        }

        /// <summary>
        /// We handle press events (as well as clicks) because we want the cursor to move
        /// when the mouse is pressed down, not on release.
        /// </summary>
        private void HandlePress(Gesture.OnPress evt) {
            if (!isFocused) {
                return;
            }

            if (TextPosition.TryCreate(inputField.TextBlock, evt.Interaction.Ray, out TextPosition pos)) {
                // Move the cursor to the position, highlighting along the way if shift is held down
                bool shiftHeldDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                inputField.MoveCursor(pos, shiftHeldDown);
            }
        }

        private void HandleClick(Gesture.OnClick evt) {
            if (!isFocused) {
                // Not focused yet, so focus the input field
                isFocused = true;

                if (SelectAllOnFocus) {
                    // Highlight all text
                    inputField.SetCursorPosition(inputField.CursorPosition.MoveToEnd(), inputField.CursorPosition.MoveToStart());
                }
                else if (TextPosition.TryCreate(inputField.TextBlock, evt.Interaction.Ray, out TextPosition pos)) {
                    // Move the cursor to the clicked location
                    inputField.MoveCursor(pos, false);
                }
                else {
                    // Move the cursor to the end of the text
                    inputField.MoveCursor(inputField.CursorPosition.MoveToEnd(), false);
                }

                OnFocused?.Invoke();
            }
            else {
                // Already focused, check if the user double clicked a word and highlight it if so.
                if ((Time.unscaledTime - lastClickTime) < wordSelectDoubleClickDelay) {
                    inputField.SetCursorPosition(inputField.CursorPosition.MoveToEndOfWord(), inputField.CursorPosition.MoveToStartOfWord());
                }
            }

            lastClickTime = Time.unscaledTime;
        }

        /// <summary>
        /// Unfocus the <see cref="SuperspectiveTextField"/>.
        /// </summary>
        public void RemoveFocus() {
            if (!isFocused) {
                return;
            }

            isFocused = false;
            inputField.HideAllVisuals();
            OnFocusLost?.Invoke();
        }

        /// <summary>
        /// Handles unfocusing the <see cref="SuperspectiveTextField"/> if the user clicks somewhere else.
        /// </summary>
        private void HandlePostClick(UIBlock clickedBlock) {
            if (!isFocused) {
                return;
            }

            if (clickedBlock != uiBlock) {
                // Clicked somewhere else, so remove focus.
                RemoveFocus();
            }
        }
    }
}
