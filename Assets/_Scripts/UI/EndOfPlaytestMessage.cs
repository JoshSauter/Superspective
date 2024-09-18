using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(UniqueId))]
public class EndOfPlaytestMessage : Singleton<EndOfPlaytestMessage> {

	public Image background;
	public TMP_Text gameNameText;
	public TMP_Text[] thankYouText;
	public TMP_Text[] discordText;
	public Image discordLogo;
	public Image discordButtonImage;
	public UnityEngine.UI.Button discordLinkButton;

	public enum State {
        Off,
        BackgroundFadingIn,
        GameNameFadingIn,
        ThankYouTextFadingIn,
        DiscordLinkFadingIn,
        Finished
    }
    public StateMachine<State> state;
    
    // Animation settings
    private const float timeForBackgroundFadeIn = 3f;
    private const float timeForGameNameFadeIn = 2f;
    private const float timeForThankYouFadeIn = 2f;
    private const float timeForDiscordLinkFadeIn = 2f;

    protected void Start() {
	    state = this.StateMachine(State.Off);
	    
	    state.AddStateTransition(State.BackgroundFadingIn, State.GameNameFadingIn, timeForBackgroundFadeIn);
	    state.AddStateTransition(State.GameNameFadingIn, State.ThankYouTextFadingIn, timeForGameNameFadeIn);
	    state.AddStateTransition(State.ThankYouTextFadingIn, State.DiscordLinkFadingIn, timeForThankYouFadeIn);
	    state.AddStateTransition(State.DiscordLinkFadingIn, State.Finished, timeForDiscordLinkFadeIn);
        
	    state.AddTrigger(State.GameNameFadingIn, () => {
		    background.color = background.color.WithAlpha(1);
		    
		    // Disable player control
		    PlayerMovement.instance.thisRigidbody.isKinematic = true;
	    });
	    state.AddTrigger(State.Finished, () => {
		    Cursor.visible = true;
		    Cursor.lockState = CursorLockMode.Confined;
		    discordLinkButton.interactable = true;
	    });
    }

    void Update() {
	    float t = 0f;
	    switch (state.State) {
		    case State.Off:
			    break;
		    case State.BackgroundFadingIn:
			    t = state.Time / timeForBackgroundFadeIn;
			    background.enabled = true;
			    background.color = background.color.WithAlpha(Easing.EaseInOut(t));
			    break;
		    case State.GameNameFadingIn:
			    gameNameText.enabled = true;
			    t = state.Time / timeForGameNameFadeIn;
			    gameNameText.color = gameNameText.color.WithAlpha(Easing.EaseInOut(t));
			    break;
		    case State.ThankYouTextFadingIn:
			    t = state.Time / timeForThankYouFadeIn;
			    foreach (var text in thankYouText) {
				    text.enabled = true;
				    text.color = text.color.WithAlpha(Easing.EaseInOut(t));
			    }
			    break;
		    case State.DiscordLinkFadingIn:
			    t = state.Time / timeForDiscordLinkFadeIn;
			    foreach (var text in discordText) {
				    text.enabled = true;
				    text.color = text.color.WithAlpha(Easing.EaseInOut(t));
			    }
			    discordLogo.enabled = true;
			    discordLogo.color = discordLogo.color.WithAlpha(Easing.EaseInOut(t));
			    discordButtonImage.enabled = true;
			    discordButtonImage.color = discordButtonImage.color.WithAlpha(Easing.EaseInOut(t));
			    discordLinkButton.enabled = true;
			    break;
		    case State.Finished:
			    break;
		    default:
			    throw new ArgumentOutOfRangeException();
	    }
    }
}
