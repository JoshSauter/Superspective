using System;
using System.Collections;
using System.Collections.Generic;
using Nova;
using StateUtils;
using UnityEngine;

public class SendingDialog : DialogWindow {
    public enum SendingState {
        Sending,
        SentSuccessfully,
        FailedToSend
    }

    public StateMachine<SendingState> sendingState;

    public bool isSending => sendingState == SendingState.Sending;
    private const string Sent = "Thank you for your feedback!";
    private const string Failed = "Failed to send. How ironic :(";
    private readonly string[] Sending = new string[4] { "Sending...", ".Sending.. ", "..Sending.", "...Sending" };
    public TextBlock sendingText;

    private const float animationTime = 0.375f;
    public const float closeDelay = 1.75f;

    void Awake() {
        sendingState = this.StateMachine(SendingState.Sending);
    }

    protected override void Start() {
        base.Start();
        
        dialogWindowState.AddTrigger(DialogWindowState.Closed, menuFadeAnimationTime, ResetText);
        sendingState.AddStateTransition(SendingState.SentSuccessfully, SendingState.Sending, menuFadeAnimationTime+closeDelay);
        sendingState.AddStateTransition(SendingState.FailedToSend, SendingState.Sending, menuFadeAnimationTime+closeDelay);
    }
    
    private void Update() {
        switch (dialogWindowState.State) {
            case DialogWindowState.Closed:
                break;
            case DialogWindowState.Open:
                switch (sendingState.State) {
                    case SendingState.Sending:
                        int index = ((int)(dialogWindowState.Time / animationTime)) % 4;
                        sendingText.Text = Sending[index];
                        break;
                    case SendingState.SentSuccessfully:
                        sendingText.Text = Sent;
                        break;
                    case SendingState.FailedToSend:
                        sendingText.Text = Failed;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void ResetText() {
        sendingText.Text = Sending[0];
        ConfirmButton.TextBlock.Get().Text = "Click me!";
    }

    public void HandleConfirmClicked() {
        ConfirmButton.TextBlock.Get().Text = "Thanks :)";
    }
}
