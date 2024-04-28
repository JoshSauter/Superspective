using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Saving;
using SerializableClasses;
using StateUtils;
using SuperspectiveUtils;

/// This script is used to flash a set of renderers with a specific color and emission color for a set number of times.
/// Assumes that the renderers use _Color and _EmissionColor as their color and emission color properties.
/// Also assumes that the renderers aren't changing colors while this script is flashing them.
[RequireComponent(typeof(UniqueId))]
public class FlashColors : SaveableObject<FlashColors, FlashColors.FlashColorsSave> {
    public Color flashColor = new Color(.8f, .2f, .3f);
    [ColorUsage(false, true)]
    public Color flashEmission = new Color(1.4f, .05f, .055f);
    
    public int flashTimes = 3; // -1 means infinite
    public float interval = 0.8f; // Time between each flash peak

    public List<Renderer> renderers = new List<Renderer>();
    private Dictionary<Renderer, Color> startColors;
    private Dictionary<Renderer, Color> startEmissions;
    
    public enum State {
        Idle,
        Flashing
    }
    public StateMachine<State> state;
    private static readonly string emissionColorProperty = "_EmissionColor";

    protected override void Awake() {
        base.Awake();

        state = this.StateMachine(State.Idle);
        
        state.AddTrigger(State.Idle, () => {
            foreach (Renderer r in renderers) {
                r.SetColorForRenderer(startColors[r]);
                if (startEmissions.ContainsKey(r)) {
                    r.SetHDRColorForRenderer(startEmissions[r], emissionColorProperty);
                }
            }
        });
    }
    
    public void Flash(int numberOfTimes, float interval) {
        this.flashTimes = numberOfTimes;
        this.interval = interval;
        
        this.startColors = renderers.ToDictionary(r => r, r => r.material.color);
        this.startEmissions = renderers
            .Where(r => r.material.HasProperty(emissionColorProperty))
            .ToDictionary(r => r, r => r.material.GetColor(emissionColorProperty));
        
        state.Set(State.Flashing);
    }

    public void CancelFlash() {
        state.Set(State.Idle);
    }

    void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;

        switch (state.state) {
            case State.Idle:
                break;
            case State.Flashing:
                if (flashTimes > 0 && state.timeSinceStateChanged > flashTimes * interval) {
                    state.Set(State.Idle);
                    break;
                }
                
                float t = 0.5f + 0.5f * Mathf.Cos(state.timeSinceStateChanged * 2 * Mathf.PI / interval + Mathf.PI);
                foreach (Renderer r in renderers) {
                    r.SetColorForRenderer(Color.Lerp(startColors[r], flashColor, t));
                    if (startEmissions.ContainsKey(r)) {
                        r.SetHDRColorForRenderer(Color.Lerp(startEmissions[r], flashEmission, t), emissionColorProperty);
                    }
                }
                break;
        }
    }
    
#region Saving
		[Serializable]
		public class FlashColorsSave : SerializableSaveObject<FlashColors> {
            private StateMachine<State>.StateMachineSave stateSave;
            
            private List<SerializableColor> startColors;
            private List<SerializableColor> startEmissions;

			public FlashColorsSave(FlashColors script) : base(script) {
                this.stateSave = script.state.ToSave();

                this.startColors = new List<SerializableColor>();
                this.startEmissions = new List<SerializableColor>();
                // Only save the start colors if they have been initialized
                if (script.startColors != null && script.startColors.Count > 0) {
                    for (int i = 0; i < script.renderers.Count; i++) {
                        this.startColors.Add(script.startColors[script.renderers[i]]);
                        if (script.startEmissions.ContainsKey(script.renderers[i])) {
                            this.startEmissions.Add(script.startEmissions[script.renderers[i]]);
                        } else {
                            this.startEmissions.Add(Color.clear);
                        }
                    }
                }
            }

			public override void LoadSave(FlashColors script) {
                script.state.LoadFromSave(this.stateSave);
                
                script.startColors = new Dictionary<Renderer, Color>();
                script.startEmissions = new Dictionary<Renderer, Color>();
                for (int i = 0; i < script.renderers.Count; i++) {
                    script.startColors[script.renderers[i]] = this.startColors[i];
                    if (this.startEmissions[i] != Color.clear) {
                        script.startEmissions[script.renderers[i]] = this.startEmissions[i];
                    }
                }
			}
		}
#endregion
}
