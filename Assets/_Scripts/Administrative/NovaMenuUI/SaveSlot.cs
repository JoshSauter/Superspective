using System;
using System.Collections.Generic;
using System.Linq;
using Nova;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

namespace NovaMenuUI {
    public class SaveSlot : MonoBehaviour {
        private static UIStylesheet Style => UIStyleManager.instance.CurrentStylesheet;
        
        public bool DEBUG = false;
        private DebugLogger _debug;

        private DebugLogger debug {
            get {
                if (_debug == null) {
                    _debug = new DebugLogger(gameObject, () => DEBUG);
                }

                return _debug;
            }
        }

        public Interactable SaveSlotInteractable;
        public Interactable DeleteSaveInteractable;
        public Interactable DeleteSaveConfirmInteractable;
        public Interactable DeleteSaveCancelInteractable;
        private List<Interactable> _allInteractables;

        private List<Interactable> allInteractables {
            get {
                if (_allInteractables == null || _allInteractables.Count == 0) {
                    _allInteractables = new List<Interactable>() { SaveSlotInteractable, DeleteSaveInteractable, DeleteSaveConfirmInteractable, DeleteSaveCancelInteractable };
                }

                return _allInteractables;
            }
        }

        private List<Interactable> _deleteSaveInteractables;

        private List<Interactable> deleteSaveInteractables {
            get {
                if (_deleteSaveInteractables == null || _deleteSaveInteractables.Count == 0) {
                    _deleteSaveInteractables = new List<Interactable>() { DeleteSaveInteractable, DeleteSaveConfirmInteractable, DeleteSaveCancelInteractable };
                }

                return _deleteSaveInteractables;
            }
        }

        public Option<SaveMetadataWithScreenshot> saveMetadata;
        public TextBlock SaveName;
        public UIBlock2D Screenshot;
        public TextBlock LevelName;
        public TextBlock DateSaved;
        public TextBlock TimeSaved;
        public UIBlock2D BackgroundBlock;

        public UIBlock2D DeleteSaveArea;
        public UIBlock2D DeleteSaveIcon;
        public TextBlock DeleteSaveLabel;
        public UIBlock2D DeleteSaveConfirm;
        public UIBlock2D DeleteSaveCancel;

        private DialogWindow dialogWindow; // May or may not exist on some parent object, used to determine when to ignore inputs
        bool shouldIgnoreInputBehindDialog => DialogWindow.anyDialogueIsOpen && DialogWindow.windowsOpen.Peek() != dialogWindow;

        private static Sprite[] _deleteIconSprites;
        private static readonly string deleteIconSpritesPath = "Images/UI/Icons/DeleteIcon/";

        public static Sprite[] deleteIconSprites {
            get {
                if (_deleteIconSprites == null || _deleteIconSprites.Length == 0) {
                    _deleteIconSprites = Resources.LoadAll<Sprite>(deleteIconSpritesPath);
                }

                return _deleteIconSprites;
            }
        }

        public enum ButtonState {
            Idle,
            Hovered,
            ClickHeld,
            Clicked
        }

        public StateMachine<ButtonState> saveSlotButtonState;

        public enum DeleteButtonState {
            Invisible,
            Idle,
            Hovered,
            ClickHeld,
            DeleteConfirmation
        }

        // This will be set to Invisible at Start, but needs to start not on Invisible so that OnStateChange triggers then
        public StateMachine<DeleteButtonState> deleteButtonState;
        public StateMachine<ButtonState> deleteConfirmButtonState;
        public StateMachine<ButtonState> deleteCancelButtonState;

        // UI Animation
        private AnimationHandle saveSlotColorAnimationHandle;
        private AnimationHandle deleteSaveAnimationHandle;
        private AnimationHandle deleteIconAnimationHandle;
        private AnimationHandle deleteConfirmAnimationHandle;
        private AnimationHandle deleteCancelAnimationHandle;

        private const float saveSlotAnimationTime = 0.075f;
        private const float saveSlotUnhoverAnimationTime = .5f;

        private const float saveSlotDeleteAnimationTime = .15f;
        private const float saveSlotDeleteIconLoopTime = 1f;
        private const float deleteConfirmCancelAnimationTime = 0.1f;

        public struct SaveSlotAnimation : IAnimation {
            public UIBlock background;
            public List<TextBlock> textBlocks;
            public Color startBgColor;
            public Color endBgColor;
            public Color startTextColor;
            public Color endTextColor;

            public void Update(float percentDone) {
                float t = Easing.EaseInOut(percentDone);
                background.Color = Color.Lerp(startBgColor, endBgColor, t);

                Color textColor = Color.Lerp(startTextColor, endTextColor, t);
                foreach (var textBlock in textBlocks) {
                    textBlock.Color = textColor.WithAlphaFrom(textBlock.Color);
                }
            }
        }

        public struct DeleteSaveAnimation : IAnimation {
            public UIBlock Background;
            public (Color, Color) bgColors; // (startColor, endColor)
            public UIBlock2D DeleteIcon;
            public (Color, Color) deleteIconColors;
            public TextBlock DeleteLabel;
            public (Color, Color) deleteLabelColors;
            public UIBlock DeleteConfirm;
            public (Color, Color) deleteConfirmColors;
            public UIBlock DeleteCancel;
            public (Color, Color) deleteCancelColors;

            public void Update(float percentDone) {
                float t = Easing.EaseInOut(percentDone);

                AnimateColors(DeleteIcon, deleteIconColors, t);
                AnimateColors(DeleteLabel, deleteLabelColors, t);
                AnimateColors(DeleteConfirm, deleteConfirmColors, t);
                AnimateColors(DeleteCancel, deleteCancelColors, t);
                AnimateColors(Background, bgColors, t);
            }

            void AnimateColors(UIBlock uiBlock, (Color, Color) startEndColors, float t) {
                Color startColor = startEndColors.Item1;
                Color endColor = startEndColors.Item2;

                uiBlock.Color = Color.Lerp(startColor, endColor, t);
            }
        }

        public struct DeleteIconSpriteAnimation : IAnimation {
            public UIBlock2D DeleteIcon;

            // Describes what t values we should switch the sprite of the DeleteIcon at multiples of (t*1, t*2, etc)
            public float spriteSwitchLerpTime;

            public void Update(float percentDone) {
                AnimateDeleteIconSpriteSwitching(percentDone, spriteSwitchLerpTime);
            }

            void AnimateDeleteIconSpriteSwitching(float percentDone, float spriteSwitchTime) {
                int unboundSpriteIndex = (int)(percentDone / spriteSwitchTime);
                int index = unboundSpriteIndex % deleteIconSprites.Length;

                DeleteIcon.SetImage(deleteIconSprites[index]);
            }
        }

        protected void Start() {
            saveSlotButtonState = this.StateMachine(ButtonState.Idle, false, true);
            deleteButtonState = this.StateMachine(DeleteButtonState.Invisible, false, true);
            deleteConfirmButtonState = this.StateMachine(ButtonState.Idle, false, true);
            deleteCancelButtonState = this.StateMachine(ButtonState.Idle, false, true);

            InitSaveSlotStateMachine();
            InitDeleteButtonStateMachine();
            InitDeleteConfirmStateMachine();
            InitDeleteCancelStateMachine();

            ResetSaveSlot();

            dialogWindow = gameObject.FindInParentsRecursively<DialogWindow>();
        }

        void InitDeleteButtonStateMachine() {
            deleteButtonState.OnStateChangeSimple += () => RunDeleteSaveAnimation(deleteButtonState.State, saveSlotDeleteAnimationTime);
        }

        void InitDeleteConfirmStateMachine() {
            deleteConfirmButtonState.OnStateChangeSimple += () => RunDeleteConfirmAnimation(deleteConfirmButtonState.State, deleteConfirmCancelAnimationTime);

            deleteConfirmButtonState.AddTrigger(ButtonState.Clicked, () => saveMetadata.ForEach(sm => SaveFileUtils.DeleteSave(sm.metadata.saveFilename)));
        }

        void InitDeleteCancelStateMachine() {
            deleteCancelButtonState.OnStateChangeSimple += () => RunDeleteCancelAnimation(deleteCancelButtonState.State, deleteConfirmCancelAnimationTime);

            deleteCancelButtonState.AddTrigger(ButtonState.Clicked, () => deleteButtonState.Set(DeleteButtonState.Hovered));
        }

        void InitSaveSlotStateMachine() {
            saveSlotButtonState.AddTrigger(ButtonState.Idle, ResetSaveSlot);
            saveSlotButtonState.AddTrigger(ButtonState.Hovered, () => { RunSaveSlotAnimation(Style.SaveSlot.HoverBgColor, Style.SaveSlot.DefaultTextColor, saveSlotAnimationTime); });
            saveSlotButtonState.AddTrigger(ButtonState.ClickHeld, () => { RunSaveSlotAnimation(Style.SaveSlot.ClickHeldBgColor, Style.SaveSlot.ClickHeldTextColor, saveSlotAnimationTime); });
            saveSlotButtonState.AddTrigger(ButtonState.Clicked, () => { RunSaveSlotAnimation(Style.SaveSlot.ClickedBgColor, Style.SaveSlot.ClickedTextColor, saveSlotAnimationTime); });
        }

        void RunSaveSlotAnimation(Color targetBgColor, Color targetTextColor, float duration) {
            saveSlotColorAnimationHandle.Cancel();

            SaveSlotAnimation animation = new SaveSlotAnimation() {
                background = BackgroundBlock,
                textBlocks = new List<TextBlock>() { SaveName, LevelName, DateSaved, TimeSaved },
                startBgColor = BackgroundBlock.Color,
                endBgColor = targetBgColor,
                startTextColor = SaveName.Color,
                endTextColor = targetTextColor
            };

            saveSlotColorAnimationHandle = animation.Run(duration);
        }

        void RunDeleteSaveAnimation(DeleteButtonState state, float duration) {
            deleteSaveAnimationHandle.Cancel();

            Color bgEndColor, bgStartColor = DeleteSaveArea.Color;
            Color deleteIconEndColor, deleteIconStartColor = DeleteSaveIcon.Color;
            Color deleteLabelEndColor, deleteLabelStartColor = DeleteSaveLabel.Color;
            Color deleteConfirmEndColor, deleteConfirmStartColor = DeleteSaveConfirm.Color;
            Color deleteCancelEndColor, deleteCancelStartColor = DeleteSaveCancel.Color;
            bool shouldAnimateDeleteIcon = false;
            switch (state) {
                case DeleteButtonState.Invisible:
                    bgEndColor = Style.SaveSlot.Delete.DeleteDefaultBgColor.WithAlpha(0);
                    deleteIconEndColor = Style.SaveSlot.Delete.DefaultTextAndIconColor.WithAlpha(0);
                    deleteLabelEndColor = Style.SaveSlot.Delete.DefaultTextAndIconColor.WithAlpha(0);
                    deleteConfirmEndColor = Style.SaveSlot.Delete.DefaultConfirmColor.WithAlpha(0);
                    deleteCancelEndColor = Style.SaveSlot.Delete.DefaultCancelColor.WithAlpha(0);
                    break;
                case DeleteButtonState.Idle:
                    bgEndColor = Style.SaveSlot.Delete.DeleteDefaultBgColor;
                    deleteIconEndColor = Style.SaveSlot.Delete.DefaultTextAndIconColor;
                    deleteLabelEndColor = Style.SaveSlot.Delete.DefaultTextAndIconColor;
                    deleteConfirmEndColor = Style.SaveSlot.Delete.DefaultConfirmColor.WithAlpha(0);
                    deleteCancelEndColor = Style.SaveSlot.Delete.DefaultCancelColor.WithAlpha(0);
                    break;
                case DeleteButtonState.Hovered:
                    bgEndColor = Style.SaveSlot.Delete.DeleteHoverBgColor;
                    deleteIconEndColor = Style.SaveSlot.Delete.DefaultTextAndIconColor;
                    deleteLabelEndColor = Style.SaveSlot.Delete.DefaultTextAndIconColor;
                    deleteConfirmEndColor = Style.SaveSlot.Delete.DefaultConfirmColor.WithAlpha(0);
                    deleteCancelEndColor = Style.SaveSlot.Delete.DefaultCancelColor.WithAlpha(0);
                    break;
                case DeleteButtonState.ClickHeld:
                    bgEndColor = Style.SaveSlot.Delete.DeleteClickHeldBgColor;
                    deleteIconEndColor = Style.SaveSlot.Delete.ClickHeldTextAndIconColor;
                    deleteLabelEndColor = Style.SaveSlot.Delete.ClickHeldTextAndIconColor;
                    deleteConfirmEndColor = Style.SaveSlot.Delete.DefaultConfirmColor.WithAlpha(0);
                    deleteCancelEndColor = Style.SaveSlot.Delete.DefaultCancelColor.WithAlpha(0);
                    break;
                case DeleteButtonState.DeleteConfirmation:
                    shouldAnimateDeleteIcon = true;

                    bgEndColor = Style.SaveSlot.Delete.DeleteClickedBgColor;
                    deleteIconEndColor = Style.SaveSlot.Delete.ClickedTextAndIconColor;
                    deleteLabelEndColor = Style.SaveSlot.Delete.ClickedTextAndIconColor.WithAlpha(0);
                    deleteConfirmEndColor = Style.SaveSlot.Delete.DefaultConfirmColor;
                    deleteCancelEndColor = Style.SaveSlot.Delete.DefaultCancelColor;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }

            DeleteSaveAnimation animation = new DeleteSaveAnimation {
                Background = DeleteSaveArea,
                bgColors = (bgStartColor, bgEndColor),
                DeleteIcon = DeleteSaveIcon,
                deleteIconColors = (deleteIconStartColor, deleteIconEndColor),
                DeleteLabel = DeleteSaveLabel,
                deleteLabelColors = (deleteLabelStartColor, deleteLabelEndColor),
                DeleteConfirm = DeleteSaveConfirm,
                deleteConfirmColors = (deleteConfirmStartColor, deleteConfirmEndColor),
                DeleteCancel = DeleteSaveCancel,
                deleteCancelColors = (deleteCancelStartColor, deleteCancelEndColor)
            };

            debug.LogWarning($"Running animation for Delete Area: {state}");
            deleteSaveAnimationHandle = animation.Run(duration);
            if (shouldAnimateDeleteIcon) {
                DeleteIconSpriteAnimation spriteAnimation = new DeleteIconSpriteAnimation {
                    DeleteIcon = DeleteSaveIcon,
                    spriteSwitchLerpTime = 1f / deleteIconSprites.Length
                };

                deleteIconAnimationHandle = spriteAnimation.Loop(saveSlotDeleteIconLoopTime);
            }
            else {
                deleteIconAnimationHandle.Cancel();
            }
        }

        void RunDeleteConfirmAnimation(ButtonState confirmButtonState, float duration) {
            deleteConfirmAnimationHandle.Cancel();

            Color deleteConfirmEndColor, deleteConfirmStartColor = DeleteSaveConfirm.Color;
            switch (confirmButtonState) {
                case ButtonState.Idle:
                case ButtonState.Hovered:
                    deleteConfirmEndColor = Style.SaveSlot.Delete.DefaultConfirmColor;
                    break;
                case ButtonState.ClickHeld:
                case ButtonState.Clicked:
                    deleteConfirmEndColor = Style.SaveSlot.Delete.ClickHeldConfirmColor;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(confirmButtonState), confirmButtonState, null);
            }

            ColorFadeAnimation animation = new ColorFadeAnimation {
                UIBlock = DeleteSaveConfirm,
                startColor = deleteConfirmStartColor,
                endColor = deleteConfirmEndColor
            };

            deleteConfirmAnimationHandle = animation.Run(duration);
        }

        void RunDeleteCancelAnimation(ButtonState cancelButtonState, float duration) {
            deleteCancelAnimationHandle.Cancel();

            Color deleteCancelEndColor, deleteCancelStartColor = DeleteSaveCancel.Color;
            switch (cancelButtonState) {
                case ButtonState.Idle:
                case ButtonState.Hovered:
                    deleteCancelEndColor = Style.SaveSlot.Delete.DefaultCancelColor;
                    break;
                case ButtonState.ClickHeld:
                case ButtonState.Clicked:
                    deleteCancelEndColor = Style.SaveSlot.Delete.ClickHeldCancelColor;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cancelButtonState), cancelButtonState, null);
            }

            ColorFadeAnimation animation = new ColorFadeAnimation {
                UIBlock = DeleteSaveCancel,
                startColor = deleteCancelStartColor,
                endColor = deleteCancelEndColor
            };

            deleteCancelAnimationHandle = animation.Run(duration);
        }

        public void ResetSaveSlot() {
            RunSaveSlotAnimation(Style.SaveSlot.DefaultBgColor, Style.SaveSlot.DefaultTextColor, saveSlotUnhoverAnimationTime);
            RunDeleteSaveAnimation(DeleteButtonState.Invisible, saveSlotDeleteAnimationTime);

            saveSlotButtonState.Set(ButtonState.Idle);
            deleteButtonState.Set(DeleteButtonState.Invisible);
            deleteConfirmButtonState.Set(ButtonState.Idle);
            deleteCancelButtonState.Set(ButtonState.Idle);
        }

        private void OnEnable() {
            SubscribeToMouseEvents();
        }

        private void OnDisable() {
            UnsubscribeFromMouseEvents();
        }

        void SubscribeToMouseEvents() {
            NovaUIBackground.instance.BackgroundInteractable.UIBlock.AddGestureHandler<Gesture.OnRelease>(HandleBGReleaseEvent);

            SaveSlotInteractable.UIBlock.AddGestureHandler<Gesture.OnHover>(HandleSaveSlotHoverEvent);
            SaveSlotInteractable.UIBlock.AddGestureHandler<Gesture.OnUnhover>(HandleSaveSlotUnhoverEvent);
            SaveSlotInteractable.UIBlock.AddGestureHandler<Gesture.OnPress>(HandleSaveSlotClickDownEvent);
            SaveSlotInteractable.UIBlock.AddGestureHandler<Gesture.OnRelease>(HandleSaveSlotReleaseEvent);

            DeleteSaveInteractable.UIBlock.AddGestureHandler<Gesture.OnHover>(HandleDeleteButtonHoverEvent);
            DeleteSaveInteractable.UIBlock.AddGestureHandler<Gesture.OnUnhover>(HandleDeleteButtonUnhoverEvent);
            DeleteSaveInteractable.UIBlock.AddGestureHandler<Gesture.OnPress>(HandleDeleteButtonClickDownEvent);
            DeleteSaveInteractable.UIBlock.AddGestureHandler<Gesture.OnRelease>(HandleDeleteButtonReleaseEvent);

            DeleteSaveConfirmInteractable.UIBlock.AddGestureHandler<Gesture.OnHover>(HandleDeleteConfirmHoverEvent);
            DeleteSaveConfirmInteractable.UIBlock.AddGestureHandler<Gesture.OnUnhover>(HandleDeleteConfirmUnhoverEvent);
            DeleteSaveConfirmInteractable.UIBlock.AddGestureHandler<Gesture.OnPress>(HandleDeleteConfirmClickDownEvent);
            DeleteSaveConfirmInteractable.UIBlock.AddGestureHandler<Gesture.OnRelease>(HandleDeleteConfirmReleaseEvent);

            DeleteSaveCancelInteractable.UIBlock.AddGestureHandler<Gesture.OnHover>(HandleDeleteCancelHoverEvent);
            DeleteSaveCancelInteractable.UIBlock.AddGestureHandler<Gesture.OnUnhover>(HandleDeleteCancelUnhoverEvent);
            DeleteSaveCancelInteractable.UIBlock.AddGestureHandler<Gesture.OnPress>(HandleDeleteCancelClickDownEvent);
            DeleteSaveCancelInteractable.UIBlock.AddGestureHandler<Gesture.OnRelease>(HandleDeleteCancelReleaseEvent);
        }

        void UnsubscribeFromMouseEvents() {
            SaveSlotInteractable.UIBlock.RemoveGestureHandler<Gesture.OnHover>(HandleSaveSlotHoverEvent);
            SaveSlotInteractable.UIBlock.RemoveGestureHandler<Gesture.OnUnhover>(HandleSaveSlotUnhoverEvent);
            SaveSlotInteractable.UIBlock.RemoveGestureHandler<Gesture.OnPress>(HandleSaveSlotClickDownEvent);
            SaveSlotInteractable.UIBlock.RemoveGestureHandler<Gesture.OnRelease>(HandleSaveSlotReleaseEvent);

            DeleteSaveInteractable.UIBlock.RemoveGestureHandler<Gesture.OnHover>(HandleDeleteButtonHoverEvent);
            DeleteSaveInteractable.UIBlock.RemoveGestureHandler<Gesture.OnUnhover>(HandleDeleteButtonUnhoverEvent);
            DeleteSaveInteractable.UIBlock.RemoveGestureHandler<Gesture.OnPress>(HandleDeleteButtonClickDownEvent);
            DeleteSaveInteractable.UIBlock.RemoveGestureHandler<Gesture.OnRelease>(HandleDeleteButtonReleaseEvent);

            DeleteSaveConfirmInteractable.UIBlock.RemoveGestureHandler<Gesture.OnHover>(HandleDeleteConfirmHoverEvent);
            DeleteSaveConfirmInteractable.UIBlock.RemoveGestureHandler<Gesture.OnUnhover>(HandleDeleteConfirmUnhoverEvent);
            DeleteSaveConfirmInteractable.UIBlock.RemoveGestureHandler<Gesture.OnPress>(HandleDeleteConfirmClickDownEvent);
            DeleteSaveConfirmInteractable.UIBlock.RemoveGestureHandler<Gesture.OnRelease>(HandleDeleteConfirmReleaseEvent);

            DeleteSaveCancelInteractable.UIBlock.RemoveGestureHandler<Gesture.OnHover>(HandleDeleteCancelHoverEvent);
            DeleteSaveCancelInteractable.UIBlock.RemoveGestureHandler<Gesture.OnUnhover>(HandleDeleteCancelUnhoverEvent);
            DeleteSaveCancelInteractable.UIBlock.RemoveGestureHandler<Gesture.OnPress>(HandleDeleteCancelClickDownEvent);
            DeleteSaveCancelInteractable.UIBlock.RemoveGestureHandler<Gesture.OnRelease>(HandleDeleteCancelReleaseEvent);
        }

        private void HandleBGReleaseEvent(Gesture.OnRelease evt) {
            // If the background receives a mouse up event, release the SaveSlotButtonState back to Idle
            if (!allInteractables.Select(i => i.UIBlock).Contains(evt.Receiver)) {
                debug.Log($"Background received a release event so setting {LevelName.Text} to Idle");
                saveSlotButtonState.Set(ButtonState.Idle);
            }
        }

        private bool EventHappenedOnSaveSlot(IGestureEvent evt) {
            bool isHoveredOverSaveSlotDirectly = evt.Receiver == SaveSlotInteractable.UIBlock;
            bool hoveredOverInvisibleDeleteButton = deleteButtonState == DeleteButtonState.Invisible && deleteSaveInteractables.Select(i => i.UIBlock).Contains(evt.Receiver);

            return (isHoveredOverSaveSlotDirectly || hoveredOverInvisibleDeleteButton) && !shouldIgnoreInputBehindDialog;
        }

        private void HandleSaveSlotHoverEvent(Gesture.OnHover evt) {
            // Effectively prevents clicking through something else to click on this
            if (!EventHappenedOnSaveSlot(evt)) return;

            if (saveSlotButtonState != ButtonState.Clicked) {
                debug.Log($"Setting {LevelName.Text} to Hovered");
                saveSlotButtonState.Set(ButtonState.Hovered);
            }
            else {
                debug.Log($"Already Clicked so not setting {LevelName.Text} to Hovered");
            }

            evt.Consume();
        }

        private void HandleSaveSlotUnhoverEvent(Gesture.OnUnhover evt) {
            if (!EventHappenedOnSaveSlot(evt)) return;

            if (saveSlotButtonState != ButtonState.Clicked) {
                debug.Log($"Setting {LevelName.Text} to Idle");
                saveSlotButtonState.Set(ButtonState.Idle);
            }
            else {
                debug.Log($"Already Clicked so not setting {LevelName.Text} to Idle");
            }

            evt.Consume();
        }

        private void HandleSaveSlotClickDownEvent(Gesture.OnPress evt) {
            if (!EventHappenedOnSaveSlot(evt)) return;

            debug.Log($"Setting {LevelName.Text} to ClickHeld");
            saveSlotButtonState.Set(ButtonState.ClickHeld);
            evt.Consume();
        }

        private void HandleSaveSlotReleaseEvent(Gesture.OnRelease evt) {
            if (shouldIgnoreInputBehindDialog) return;

            if (evt.Hovering) {
                debug.Log($"Setting {LevelName.Text} to Clicked (because hovered)");
                // Double-click on existing save, or single click on new one
                if (saveSlotButtonState.PrevState == ButtonState.Clicked || saveMetadata.IsEmpty()) {
                    SaveSlotDoubleClicked();
                }

                saveSlotButtonState.Set(ButtonState.Clicked);

                bool deleteButtonIsInvisible = deleteButtonState == DeleteButtonState.Invisible;
                bool deleteButtonShouldBeVisibleNow = saveMetadata.IsDefined();
                if (deleteButtonIsInvisible && deleteButtonShouldBeVisibleNow) {
                    bool receiverIsDeleteButton = deleteSaveInteractables.Select(i => i.UIBlock).Contains(evt.Receiver);
                    if (receiverIsDeleteButton) {
                        deleteButtonState.Set(DeleteButtonState.Hovered);
                    }
                    else {
                        deleteButtonState.Set(DeleteButtonState.Idle);
                    }
                }

                evt.Consume();
            }
            else if (saveSlotButtonState.PrevState == ButtonState.Clicked) {
                debug.Log($"Setting {LevelName.Text} to Clicked (because prevState)");
                saveSlotButtonState.Set(ButtonState.Clicked);
                evt.Consume();
            }
            else {
                debug.Log($"Setting {LevelName.Text} to Idle");
                saveSlotButtonState.Set(ButtonState.Idle);
            }
        }

        private void SaveSlotDoubleClicked() {
            if (SaveMenu.instance.saveMenuState == SaveMenu.SaveMenuState.LoadSave) {
                LoadSave();
            }
            else if (SaveMenu.instance.saveMenuState == SaveMenu.SaveMenuState.WriteSave) {
                if (saveMetadata.IsDefined()) {
                    SaveMetadata curMetadata = saveMetadata.Get().metadata;
                    SaveMetadataWithScreenshot newMetadata = SaveFileUtils.CreateNewSaveMetadataFromCurrentState(curMetadata.saveFilename, curMetadata.displayName);
                    SaveMenu.instance.NewSaveDialog.OpenWithMetadata(newMetadata, CreateNewSaveDialog.Mode.OverwriteSave);
                }
                else {
                    SaveMenu.instance.NewSaveDialog.OpenAndCreateNewMetadata();
                }
            }
        }

        private void LoadSave() {
            saveMetadata.ForEach(sm => {
                SaveManager.Load(sm);
                NovaPauseMenu.instance.ClosePauseMenu();
            });
        }

        private bool DeleteButtonShouldAcceptEvent(IGestureEvent evt) {
            bool deleteButtonIsInvisible = deleteButtonState == DeleteButtonState.Invisible;
            bool deleteButtonIsOnConfirm = deleteButtonState == DeleteButtonState.DeleteConfirmation;
            bool receiverIsDeleteButton = deleteSaveInteractables.Select(i => i.UIBlock).Contains(evt.Receiver);

            return !(deleteButtonIsInvisible || deleteButtonIsOnConfirm) && receiverIsDeleteButton && !shouldIgnoreInputBehindDialog;
        }

        private void HandleDeleteButtonHoverEvent(Gesture.OnHover evt) {
            if (!DeleteButtonShouldAcceptEvent(evt)) return;

            if (deleteButtonState != DeleteButtonState.DeleteConfirmation) {
                // debug.Log($"Setting {gameObject} to Hovered");
                deleteButtonState.Set(DeleteButtonState.Hovered);
                evt.Consume();
            }
        }

        private void HandleDeleteButtonUnhoverEvent(Gesture.OnUnhover evt) {
            if (!DeleteButtonShouldAcceptEvent(evt)) return;

            if (deleteButtonState != DeleteButtonState.DeleteConfirmation) {
                deleteButtonState.Set(DeleteButtonState.Idle);
                evt.Consume();
            }
        }

        private void HandleDeleteButtonClickDownEvent(Gesture.OnPress evt) {
            if (!DeleteButtonShouldAcceptEvent(evt)) return;

            deleteButtonState.Set(DeleteButtonState.ClickHeld);
            evt.Consume();
        }

        private void HandleDeleteButtonReleaseEvent(Gesture.OnRelease evt) {
            if (!DeleteButtonShouldAcceptEvent(evt)) return;

            if (evt.Hovering) {
                deleteButtonState.Set(DeleteButtonState.DeleteConfirmation);
                evt.Consume();
            }
            else if (deleteButtonState.PrevState == DeleteButtonState.DeleteConfirmation) {
                deleteButtonState.Set(DeleteButtonState.DeleteConfirmation);
                evt.Consume();
            }
            else {
                deleteButtonState.Set(DeleteButtonState.Idle);
            }
        }

        private bool DeleteConfirmShouldAcceptEvent(IGestureEvent evt) {
            bool confirmCancelIsVisible = deleteButtonState == DeleteButtonState.DeleteConfirmation;
            bool hoveredOnConfirm = evt.Receiver == DeleteSaveConfirm;

            return confirmCancelIsVisible && hoveredOnConfirm && !shouldIgnoreInputBehindDialog;
        }

        private void HandleDeleteConfirmHoverEvent(Gesture.OnHover evt) {
            if (!DeleteConfirmShouldAcceptEvent(evt)) return;

            deleteConfirmButtonState.Set(ButtonState.Hovered);
            evt.Consume();
        }

        private void HandleDeleteConfirmUnhoverEvent(Gesture.OnUnhover evt) {
            if (!DeleteConfirmShouldAcceptEvent(evt)) return;

            deleteConfirmButtonState.Set(ButtonState.Idle);
            evt.Consume();
        }

        private void HandleDeleteConfirmClickDownEvent(Gesture.OnPress evt) {
            if (!DeleteConfirmShouldAcceptEvent(evt)) return;

            deleteConfirmButtonState.Set(ButtonState.ClickHeld);
            evt.Consume();
        }

        private void HandleDeleteConfirmReleaseEvent(Gesture.OnRelease evt) {
            if (!DeleteConfirmShouldAcceptEvent(evt)) return;

            if (evt.Hovering) {
                deleteConfirmButtonState.Set(ButtonState.Clicked);
                evt.Consume();
            }
            else {
                deleteConfirmButtonState.Set(ButtonState.Idle);
            }
        }

        private bool DeleteCancelShouldAcceptEvent(IGestureEvent evt) {
            bool confirmCancelIsVisible = deleteButtonState == DeleteButtonState.DeleteConfirmation;
            bool hoveredOnCancel = evt.Receiver == DeleteSaveCancel;

            return confirmCancelIsVisible && hoveredOnCancel && !shouldIgnoreInputBehindDialog;
        }

        private void HandleDeleteCancelHoverEvent(Gesture.OnHover evt) {
            if (!DeleteCancelShouldAcceptEvent(evt)) return;

            deleteCancelButtonState.Set(ButtonState.Hovered);
            evt.Consume();
        }

        private void HandleDeleteCancelUnhoverEvent(Gesture.OnUnhover evt) {
            if (!DeleteCancelShouldAcceptEvent(evt)) return;

            deleteCancelButtonState.Set(ButtonState.Idle);
            evt.Consume();
        }

        private void HandleDeleteCancelClickDownEvent(Gesture.OnPress evt) {
            if (!DeleteCancelShouldAcceptEvent(evt)) return;

            deleteCancelButtonState.Set(ButtonState.ClickHeld);
            evt.Consume();
        }

        private void HandleDeleteCancelReleaseEvent(Gesture.OnRelease evt) {
            if (!DeleteCancelShouldAcceptEvent(evt)) return;

            if (evt.Hovering) {
                deleteCancelButtonState.Set(ButtonState.Clicked);
                evt.Consume();
            }
            else {
                deleteCancelButtonState.Set(ButtonState.Idle);
            }
        }
    }
}
