using System;
using System.Collections;
using System.Collections.Generic;
using LevelManagement;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine.UI;

[RequireComponent(typeof(UniqueId))]
public class Letterboxing : SingletonSuperspectiveObject<Letterboxing, Letterboxing.LetterboxingSave> {
	public Image topLetterboxBar, bottomLetterboxBar;
	public const float LETTERBOX_HEIGHT = 0.15f; // Fraction of the screen that each bar takes up
	public const float LETTERBOX_APPEAR_TIME = 2f;
	public float LETTERBOX_DISAPPEAR_TIME = LevelChangeBanner.FADE_TIME;
	private const float MAX_ALPHA = 0.975f;

	public bool LetterboxingEnabled => Settings.Video.LetterboxingEnabled;
	private bool IsDisplaying => state == State.On;
	
    public enum State : byte {
        Off,
        On
    }
    public StateMachine<State> state;

    protected override void Awake() {
	    base.Awake();
	    
	    state = this.StateMachine(State.Off);
	    
	    state.AddStateTransition(State.On, State.Off, () => !LevelChangeBanner.instance.IsPlayingBanner && !LevelChangeBanner.instance.HasQueuedBanner);
    }

    public void TurnOnLetterboxing() {
	    state.Set(State.On);
    }
    
    public void TurnOffLetterboxing() {
	    if (!LevelChangeBanner.instance.HasQueuedBanner) {
		    state.Set(State.Off);
	    }
	}

    void Update() {
	    if (!LetterboxingEnabled) {
		    state.Set(State.Off);
	    }
	    
	    if (!IsDisplaying && state.Time >= LETTERBOX_DISAPPEAR_TIME) {
		    topLetterboxBar.enabled = bottomLetterboxBar.enabled = LetterboxingEnabled;
	    }
	    
	    float t = Easing.EaseInOut(state.Time / (IsDisplaying ? LETTERBOX_APPEAR_TIME : LETTERBOX_DISAPPEAR_TIME));

	    if (IsDisplaying) {
		    if (state.Time < LETTERBOX_APPEAR_TIME) {
			    Color targetColor = Color.black;
			    targetColor.a = MAX_ALPHA;
			    Color startColor = targetColor;
			    startColor.a = 0;

			    topLetterboxBar.color = Color.Lerp(startColor, targetColor, t);
			    bottomLetterboxBar.color = Color.Lerp(startColor, targetColor, t);

			    float botTarget = Mathf.Lerp(0, LETTERBOX_HEIGHT, t);
			    float topTarget = 1 - botTarget;

			    // Only move the letterbox as it arrives, not when it leaves
			    Vector2 topAnchorMin = topLetterboxBar.rectTransform.anchorMin;
			    topAnchorMin.y = topTarget;
			    topLetterboxBar.rectTransform.anchorMin = topAnchorMin;

			    Vector2 botAnchorMax = bottomLetterboxBar.rectTransform.anchorMax;
			    botAnchorMax.y = botTarget;
			    bottomLetterboxBar.rectTransform.anchorMax = botAnchorMax;
		    }
		    else {
			    topLetterboxBar.color = Color.black.WithAlpha(MAX_ALPHA);
			    bottomLetterboxBar.color = Color.black.WithAlpha(MAX_ALPHA);
		    }
	    }
	    else {
		    if (state.Time < LETTERBOX_DISAPPEAR_TIME) {
			    Color targetColor = Color.black;
			    targetColor.a = 0;
			    Color startColor = targetColor;
			    startColor.a = MAX_ALPHA;

			    topLetterboxBar.color = Color.Lerp(startColor, targetColor, t);
			    bottomLetterboxBar.color = Color.Lerp(startColor, targetColor, t);

			    float botTarget = Mathf.Lerp(LETTERBOX_HEIGHT, 0, t);
			    float topTarget = 1 - botTarget;
		    }
		    else {
			    Color targetColor = Color.black.WithAlpha(0);
			    topLetterboxBar.color = targetColor;
			    bottomLetterboxBar.color = targetColor;
		    }
	    }
    }
    
#region Saving

	public override void LoadSave(LetterboxingSave save) {
		state.LoadFromSave(save.stateSave);
	}

	[Serializable]
	public class LetterboxingSave : SaveObject<Letterboxing> {
        public StateMachineSave<State> stateSave;
        
		public LetterboxingSave(Letterboxing script) : base(script) {
            this.stateSave = script.state.ToSave();
		}
	}
#endregion
}
