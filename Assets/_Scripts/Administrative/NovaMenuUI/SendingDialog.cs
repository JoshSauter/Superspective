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

    public StateMachine<SendingState> sendingState = new StateMachine<SendingState>(SendingState.Sending);

    public bool isSending => sendingState == SendingState.Sending;
    private const string Sent = "Thank you for your feedback!";
    private const string Failed = "Failed to send. How ironic :(";
    private readonly string[] Sending = new string[4] { "Sending...", ".Sending.. ", "..Sending.", "...Sending" };
    public TextBlock sendingText;

    private const float animationTime = 0.375f;
    public const float closeDelay = 1.75f;

    protected override void Start() {
        base.Start();
        
        dialogWindowState.AddTrigger(DialogWindowState.Closed, menuFadeAnimationTime, ResetText);
        sendingState.AddStateTransition(SendingState.SentSuccessfully, SendingState.Sending, menuFadeAnimationTime+closeDelay);
        sendingState.AddStateTransition(SendingState.FailedToSend, SendingState.Sending, menuFadeAnimationTime+closeDelay);
    }
    
    private void Update() {
        switch (dialogWindowState.state) {
            case DialogWindowState.Closed:
                break;
            case DialogWindowState.Open:
                switch (sendingState.state) {
                    case SendingState.Sending:
                        int index = ((int)(dialogWindowState.timeSinceStateChanged / animationTime)) % 4;
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
