using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LevelManagement;
using Nova;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine.Networking;

namespace NovaMenuUI {
// Big ups to Jeremy Bond and his XNBugReporter tool that I used as a starting point for this reporter:
// https://cdn.discordapp.com/attachments/1004592997859598336/1012506289844269098/XnBugReporter_by_Jeremy_Bond.unitypackage
    public class BugReportDialog : DialogWindow {
        public enum ReportType {
            Bug,
            Feedback,
            OtherComment
        }

        public enum BugReportType {
            Gameplay,
            UI_UX,
            Saving_Loading,
            Other
        }

        private static readonly DropdownSetting ReportTypeSetting = DropdownSetting.Of(
            key: "ReportType",
            name: "What would you like to send?",
            allDropdownItems: new List<DropdownOption>() {
                DropdownOption.Of("Bug Report", ReportType.Bug),
                DropdownOption.Of("Feedback", ReportType.Feedback),
                //DropdownOption.Of("Other Comment", ReportType.OtherComment)
            },
            defaultIndex: 0,
            selectedIndex: 0
        );

        private static readonly DropdownSetting TypeOfBugSetting = DropdownSetting.Of(
            key: "TypeOfBug",
            name: "Type of Bug",
            allDropdownItems: new List<DropdownOption>() {
                DropdownOption.Of("Gameplay", BugReportType.Gameplay),
                DropdownOption.Of("UI/UX", BugReportType.UI_UX),
                DropdownOption.Of("Saving/Loading", BugReportType.Saving_Loading),
                DropdownOption.Of("Other", BugReportType.Other)
            },
            defaultIndex: 0,
            selectedIndex: 0
        );

        private static readonly DropdownSetting SeverityOfBugSetting = DropdownSetting.Of(
            type: SelectionType.ZeroOrOne,
            key: "Severity",
            name: "Severity",
            allDropdownItems: new List<DropdownOption>() {
                DropdownOption.Of("Didn't bother me", 0),
                DropdownOption.Of("Kind of annoying", 1),
                DropdownOption.Of("Pretty bad", 2),
                DropdownOption.Of("Soft locked", 3),
                DropdownOption.Of("Hard crash", 4)
            },
            defaultIndexOpt: new None<int>(),
            selectedIndexOpt: new None<int>()
        );

        private static readonly TextAreaSetting DescriptionSetting = new TextAreaSetting() {
            key = "Description",
            Name = "Description",
            PlaceholderText = "What happened?"
        };

        private static readonly TextAreaSetting ReproductionStepsSetting = new TextAreaSetting() {
            key = "ReproSteps",
            Name = "Reproduction Steps",
            PlaceholderText = "How can we reproduce the issue?"
        };

        private static readonly TextAreaSetting CommentSetting = new TextAreaSetting() {
            key = "Comment",
            Name = "Comment",
            PlaceholderText = "Type something here..."
        };

        public List<SettingsItem> bugReportItems = new List<SettingsItem>() {
            ReportTypeSetting,
            new SeparatorSettingsItem(),
            TypeOfBugSetting,
            SeverityOfBugSetting,
            DescriptionSetting,
            ReproductionStepsSetting
        };

        public List<SettingsItem> commentReportItems = new List<SettingsItem>() {
            ReportTypeSetting,
            new SpacerSettingItem(),
            CommentSetting
        };

        public ListView bugReportListView;
        public SendingDialog sendingDialog;

        private ReportType currentReportType;

        private readonly string REPORT_SHEETS_URL = "https://script.google.com/macros/s/AKfycbxXeKkjCUzoY1ATyCimGRAfaf1_GbSKsZBSFYXZpSHm17guOvtb_fHF38d39uPtHnvxsQ/exec";

        protected override void Start() {
            base.Start();

            SettingsList.AddAllDataBinders(bugReportListView);

            currentReportType = (ReportType)ReportTypeSetting.dropdownSelection.selection.Datum;

            SetDatasource();

            bugReportListView.UIBlock.AutoLayout.Offset = 0;
        }

        private void Update() {
            ReportType nextReportType = (ReportType)ReportTypeSetting.dropdownSelection.selection.Datum;
            if (currentReportType != nextReportType) {
                currentReportType = nextReportType;
                SetDatasource();
            }
        }

        void SetDatasource() {
            switch (currentReportType) {
                case ReportType.Bug:
                    bugReportListView.SetDataSource(bugReportItems);
                    break;
                case ReportType.Feedback:
                case ReportType.OtherComment:
                    bugReportListView.SetDataSource(commentReportItems);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnConfirm() {
            sendingDialog.sendingState.Set(SendingDialog.SendingState.Sending);
            POST(REPORT_SHEETS_URL, CreateWWWFormFromCurrentState(), (result, response) => {
                if (result) {
                    sendingDialog.sendingState.Set(SendingDialog.SendingState.SentSuccessfully);
                    Debug.Log($"Sent feedback, received response: {response}");
                }
                else {
                    sendingDialog.sendingState.Set(SendingDialog.SendingState.FailedToSend);
                    Debug.LogWarning($"Failed to send feedback, received response: {response}");
                }

                sendingDialog.CloseDelayed(SendingDialog.closeDelay);
                CloseDelayed(SendingDialog.closeDelay);
            });
        }

        WWWForm CreateWWWFormFromCurrentState() {
            WWWForm wForm = new WWWForm();
            Dictionary<string, string> keyValues = bugReportListView.GetDataSource<SettingsItem>()
                .OfType<Setting>()
                .ToDictionary(s => s.key, s => s.PrintValue());
            foreach (var kv in keyValues) {
                wForm.AddField(kv.Key, kv.Value ?? "");
            }

            wForm.AddField("Version", Application.version);
            wForm.AddField("Scene reported from", LevelManager.instance.activeSceneName);

            return wForm;
        }

        public void POST(string sheetsUrl, WWWForm wForm, Action<bool, string> callback = null) {
            StartCoroutine(POSTCoroutine(sheetsUrl, wForm, callback));
        }

        IEnumerator POSTCoroutine(string uri, WWWForm wForm, Action<bool, string> callback = null) {
            UnityWebRequest req = UnityWebRequest.Post(uri, wForm);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success) {
                Debug.Log("Error downloading: " + req.error);
                callback?.Invoke(false, req.error);
            }
            else {
                // show the info back from Google Sheets
                Debug.Log(req.downloadHandler.text);
                callback?.Invoke(true, req.downloadHandler.text);
            }
        }
    }
}
