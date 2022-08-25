using System;
using System.Collections;
using System.Collections.Generic;
using Nova;
using UnityEngine;
using Saving;
using StateUtils;

public class CreateNewSaveDialog : DialogWindow {
    public TextBlock DialogLabel;
    public SaveSlot SaveSlotPreview;
    public TextField SaveNameTextField;

    private SaveMetadataWithScreenshot saveMetadata;

    public enum Mode {
        NewSave,
        OverwriteSave
    }
    
    private string displayName => string.IsNullOrWhiteSpace(SaveNameTextField.Text) ? SaveNameTextField.placeHolderText.Text : SaveNameTextField.Text;

    protected override void Start() {
        base.Start();

        dialogWindowState.OnStateChangeSimple += () => {
            if (dialogWindowState == DialogWindowState.Open) {
                SaveNameTextField.Text = "";
            }
        };
        
        ConfirmButton.OnClick += (_) => Submit();
        CancelButton.OnClick += (_) => Close();
        SaveNameTextField.OnTextChanged += () => SaveSlotPreview.SaveName.Text = displayName;
        SaveNameTextField.GetComponent<TextFieldKeyboardInput>().OnSubmit += Submit;
    }

    void Submit() {
        if (saveMetadata != null) {
            saveMetadata.metadata.displayName = displayName;
            SaveManager.Save(saveMetadata);
            Close();
        }
    }
    
    public void OpenWithMetadata(SaveMetadataWithScreenshot metadata, Mode mode) {
        PopulateWithMetadata(metadata, mode);
        Open();
    }

    public void OpenAndCreateNewMetadata() {
        string SanitizeString(string input) {
            return input
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(":", "_")
                .Replace(" ", "_");
        }
        
        // Fills in the lowest available # that's not already taken by a save
        string GetNewSaveFileDisplayName() {
            DateTime now = DateTime.Now;
            string date = now.ToShortDateString();
            string time = now.ToShortTimeString();
            string desiredSaveFileDisplayName = $"Save {date} {time}";
            string desiredSaveFileName = SanitizeString(desiredSaveFileDisplayName);

            int incrementor = 1;
            // If there's already a save file name at this date/time, add a number to the end to make it unique
            while (SaveFileUtils.saveMetadataCache.ContainsKey(desiredSaveFileName)) {
                desiredSaveFileDisplayName = $"{desiredSaveFileDisplayName}_{incrementor}";
                desiredSaveFileName = $"{desiredSaveFileName}_{incrementor}";
                incrementor++;
            }

            return desiredSaveFileDisplayName;
        }

        string saveFileDisplayName = GetNewSaveFileDisplayName();
        string saveFilename = SanitizeString(saveFileDisplayName);

        SaveMetadataWithScreenshot saveMetadataWithScreenshot = SaveFileUtils.CreateNewSaveMetadataFromCurrentState(saveFilename, saveFileDisplayName);
        
        OpenWithMetadata(saveMetadataWithScreenshot, Mode.NewSave);
    }

    private void PopulateWithMetadata(SaveMetadataWithScreenshot metadata, Mode mode) {
        metadata.screenshot.Apply(false);
        SaveSlotPreview.Screenshot.SetImage(metadata.screenshot);
        SaveSlotPreview.LevelName.Text = metadata.metadata.levelName;
        SaveSlotPreview.DateSaved.Text = metadata.metadata.saveDate;
        SaveSlotPreview.TimeSaved.Text = metadata.metadata.saveTime;
        SaveSlotPreview.SaveName.Text = metadata.metadata.displayName;
        
        SaveNameTextField.placeHolderText.Text = metadata.metadata.displayName;

        saveMetadata = metadata;

        DialogLabel.Text = mode == Mode.NewSave ? "Create New Save" : "Overwrite Save";
    }
}
