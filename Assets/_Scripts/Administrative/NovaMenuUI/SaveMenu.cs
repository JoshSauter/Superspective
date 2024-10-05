using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nova;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

public class SaveMenu : NovaSubMenu<SaveMenu> {
    public List<Option<SaveMetadataWithScreenshot>> playerSaves = new List<Option<SaveMetadataWithScreenshot>>();
    public List<Option<SaveMetadataWithScreenshot>> allSaves = new List<Option<SaveMetadataWithScreenshot>>();
    private bool includeAutosaves => includeAutosavesToggle.value;

    public ToggleSetting includeAutosavesToggle = new ToggleSetting() {
        key = "ShowAutosaves",
        name = "Show Autosaves",
        isEnabled = true,
        value = false,
        defaultValue = false
    };

    public ListView includeAutosavesSettingsListView;
    public bool cachedIncludedAutosavesToggleValue = false;
    public CreateNewSaveDialog NewSaveDialog;
    
    public ListView SavesListView;
    public TextBlock SaveMenuLabel;
    public TextBlock NoSavesLabel;
    public ClipMask SaveMenuClipMask;
    public ClipMask SaveListClipMask;

    private Texture2D newSaveScreenshot;

    private AnimationHandle saveMenuAnimationHandle;
    private AnimationHandle saveListAnimationHandle;
    private const float fadeAnimationDuration = .5f;
    public enum SaveMenuState {
        Off,
        LoadSave,
        WriteSave
    }
    public StateMachine<SaveMenuState> saveMenuState;

    // Start is called before the first frame update
    void Start() {
        saveMenuState = this.StateMachine(SaveMenuState.Off, false, true);

        includeAutosavesSettingsListView.AddDataBinder<ToggleSetting, ToggleVisuals>(SettingsList.BindToggle);
        includeAutosavesSettingsListView.SetDataSource(new List<Setting>() { includeAutosavesToggle } );

        SavesListView.AddDataBinder<Option<SaveMetadataWithScreenshot>, SaveSlotVisuals>(BindSaveSlot);

        PopulateSaveSlots();
        
        SavesListView.SetDataSource(playerSaves);

        SaveFileUtils.OnSavesChanged += () => {
            PopulateSaveSlots(true);
        };

        // When the player enters the save menu, take a screenshot of the player's view to use as the new save screenshot
        saveMenuState.AddTrigger(SaveMenuState.WriteSave, () => newSaveScreenshot = SaveFileUtils.ScreenshotOfPlayerCameraView());

        InitSaveMenuStateMachine();
        saveMenuState.Set(SaveMenuState.Off, true);
    }

    private void Update() {
        if (cachedIncludedAutosavesToggleValue != includeAutosaves) {
            cachedIncludedAutosavesToggleValue = includeAutosaves;
            
            PopulateSaveSlots(true);
        }
    }

    void RunAnimationAndUpdateState() {
        saveMenuAnimationHandle.Cancel();
        saveListAnimationHandle.Cancel();

        float endAlpha, startAlpha = SaveMenuClipMask.Tint.a;
        switch (saveMenuState.State) {
            case SaveMenuState.Off:
                endAlpha = 0f;
                break;
            case SaveMenuState.LoadSave:
                SaveMenuLabel.Text = "Load from...";
                endAlpha = 1f;
                break;
            case SaveMenuState.WriteSave:
                SaveMenuLabel.Text = "Save to...";
                endAlpha = 1f;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        MenuFadeAnimation menuFadeAnimation = new MenuFadeAnimation {
            menuToAnimate = SaveMenuClipMask,
            startAlpha = startAlpha,
            targetAlpha = endAlpha
        };
        
        MenuFadeAnimation saveListFadeAnimation = new MenuFadeAnimation {
            menuToAnimate = SaveListClipMask,
            startAlpha = startAlpha,
            targetAlpha = endAlpha
        };

        saveMenuAnimationHandle = menuFadeAnimation.Run(fadeAnimationDuration);
        saveListAnimationHandle = saveListFadeAnimation.Run(fadeAnimationDuration);
    }

    void InitSaveMenuStateMachine() {
        saveMenuState.OnStateChangeSimple += () => {
            PopulateSaveSlots(true);
        };
        
        saveMenuState.AddTrigger(enumValue => true, RunAnimationAndUpdateState);
        saveMenuState.AddTrigger(SaveMenuState.Off, fadeAnimationDuration, () => {
            transform.parent.localPosition = transform.parent.localPosition.WithZ(1);
        });
        saveMenuState.AddTrigger(enumValue => enumValue != SaveMenuState.Off, () => {
            transform.parent.localPosition = transform.parent.localPosition.WithZ(0);
        });
    }

    /// <summary>
    /// Binds a maybe populated SaveSlot with a SaveSlotVisuals
    /// </summary>
    private void BindSaveSlot(Data.OnBind<Option<SaveMetadataWithScreenshot>> evt, SaveSlotVisuals target, int index) {
        Option<SaveMetadataWithScreenshot> maybeSaveSlot = evt.UserData;

        if (maybeSaveSlot.IsDefined()) {
            BindSaveSlot(maybeSaveSlot.Get(), target);
        }
        else {
            BindEmptySaveSlot(target);
        }
    }

    /// <summary>
    /// Bind a populated SaveSlot to a SaveSlotVisuals
    /// </summary>
    /// <param name="saveSlot"></param>
    /// <param name="visuals"></param>
    private void BindSaveSlot(SaveMetadataWithScreenshot saveSlot, SaveSlotVisuals visuals) {
        visuals.Screenshot.SetImage(saveSlot.screenshot);
        visuals.Screenshot.Color = Color.white;
        visuals.SaveName.Text = saveSlot.metadata.displayName;
        visuals.DateSaved.Text = saveSlot.metadata.saveDate;
        visuals.TimeSaved.Text = saveSlot.metadata.saveTime;
        visuals.LevelName.Text = saveSlot.metadata.levelName;
        visuals.DeleteSaveArea.Tint = visuals.DeleteSaveArea.Tint.WithAlpha(1f);

        visuals.saveSlotScript.saveMetadata = Option<SaveMetadataWithScreenshot>.Of(saveSlot);
        visuals.saveSlotScript.ResetSaveSlot();
    }

    /// <summary>
    /// Create an empty visuals to represent the unused save slot
    /// </summary>
    private void BindEmptySaveSlot(SaveSlotVisuals visuals) {
        visuals.saveSlotScript.saveMetadata = new None<SaveMetadataWithScreenshot>();
        visuals.saveSlotScript.ResetSaveSlot();
        
        visuals.Screenshot.SetImage(newSaveScreenshot);
        visuals.Screenshot.Color = Color.white.WithAlphaFrom(visuals.Background.Color);
        visuals.SaveName.Text = $"Create new save...";
        visuals.DateSaved.Text = "";
        visuals.TimeSaved.Text = "";
        visuals.LevelName.Text = "";
        visuals.DeleteSaveArea.Tint = visuals.DeleteSaveArea.Tint.WithAlpha(0f);
    }

    private void PopulatePlayerSaves() {
        playerSaves.AddRange(SaveFileUtils.playerSaveScreenshotCache.Values
            .Where(m => SaveFileUtils.IsCompatibleWith(m.metadata.version, Application.version))
            .OrderByDescending(m => m.metadata.saveTimestamp)
            .Select(Option<SaveMetadataWithScreenshot>.Of));
    }
    
    private void PopulateAllSaves() {
        allSaves.AddRange(SaveFileUtils.allSavesScreenshotCache.Values
            .Where(m => SaveFileUtils.IsCompatibleWith(m.metadata.version, Application.version))
            .OrderByDescending(m => m.metadata.saveTimestamp)
            .Select(Option<SaveMetadataWithScreenshot>.Of));
    }

    private void PopulateSaveSlots(bool refresh = false) {
        // Just keep the list as is and return if we turned the menu off
        if (saveMenuState == SaveMenuState.Off) return;
        
        playerSaves.Clear();
        allSaves.Clear();

        SaveFileUtils.ReadAllSavedMetadataWithScreenshot((saveMetadata) => {
            // If we are writing saves, add a "New Save" box before the existing saves
            if (saveMenuState == SaveMenuState.WriteSave) {
                playerSaves.Add(new None<SaveMetadataWithScreenshot>());
            }
            
            PopulatePlayerSaves();
            PopulateAllSaves();

            NoSavesLabel.gameObject.SetActive(false);
        
            if (saveMenuState == SaveMenuState.LoadSave) {
                // Display a "No saves found" message if there are no saves
                bool noSaveLabelActive = (includeAutosaves && allSaves.Count == 0) || (!includeAutosaves && playerSaves.Count == 0);
                if (noSaveLabelActive) {
                    NoSavesLabel.gameObject.SetActive(true);
                }
            }

            if (refresh) {
                SavesListView.SetDataSource(includeAutosaves ? allSaves : playerSaves);
                SavesListView.Refresh();
            }
        });
    }
}
