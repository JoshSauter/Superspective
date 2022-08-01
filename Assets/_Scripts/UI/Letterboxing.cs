using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Saving;
using StateUtils;
using UnityEngine.UI;

[RequireComponent(typeof(UniqueId))]
public class Letterboxing : SingletonSaveableObject<Letterboxing, Letterboxing.LetterboxingSave> {
	public Image topLetterboxBar, bottomLetterboxBar;
	const float letterboxHeight = 0.15f; // Fraction of the screen that each bar takes up
	const float letterboxAppearTime = 2f;
	
    public enum State {
        Off,
        On
    }
    public StateMachine<State> state = new StateMachine<State>(State.Off);

    void Update() {
	    float t = state.timeSinceStateChanged / letterboxAppearTime;
	    bool letterBoxShouldBeVisible = state == State.On;
	    
	    if (state.timeSinceStateChanged < letterboxAppearTime) {
		    t = Easing.EaseInOut(t);
            
		    Color targetColor = Color.black;
		    targetColor.a = letterBoxShouldBeVisible ? 1 : 0;
		    Color startColor = targetColor;
		    startColor.a = 1 - startColor.a;

		    topLetterboxBar.color = Color.Lerp(startColor, targetColor, t);
		    bottomLetterboxBar.color = Color.Lerp(startColor, targetColor, t);

		    float botTarget = letterBoxShouldBeVisible ? Mathf.Lerp(0, letterboxHeight, t) : Mathf.Lerp(letterboxHeight, 0, t);
		    float topTarget = 1 - botTarget;

		    Vector2 topAnchorMin = topLetterboxBar.rectTransform.anchorMin;
		    topAnchorMin.y = topTarget;
		    topLetterboxBar.rectTransform.anchorMin = topAnchorMin;

		    Vector2 botAnchorMax = bottomLetterboxBar.rectTransform.anchorMax;
		    botAnchorMax.y = botTarget;
		    bottomLetterboxBar.rectTransform.anchorMax = botAnchorMax;
	    }
    }
    
#region Saving
		[Serializable]
		public class LetterboxingSave : SerializableSaveObject<Letterboxing> {
            private StateMachine<State>.StateMachineSave stateSave;
            
			public LetterboxingSave(Letterboxing script) : base(script) {
                this.stateSave = script.state.ToSave();
			}

			public override void LoadSave(Letterboxing script) {
                script.state.FromSave(this.stateSave);
			}
		}
#endregion
}
