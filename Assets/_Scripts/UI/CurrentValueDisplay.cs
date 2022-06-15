using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Saving;
using SuperspectiveUtils;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class CurrentValueDisplay : SaveableObject<CurrentValueDisplay, CurrentValueDisplay.CurrentValueDisplaySave> {
    private const int MIN = -80;
    private const int MAX = 80;

    public int actualValue = 0;
    public float lerpSpeed = 4f;
    
    Sprite[] base9Symbols;
    public SpriteRenderer currentValueDisplayHi;
    public SpriteRenderer currentValueDisplayLo;
    public SpriteRenderer currentValueNegativeSymbol;
    
    public float currentValueDisplayLoAlpha = 1f;
    public float currentValueDisplayHiAlpha = 0f;
    public float currentValueNegativeSymbolAlpha = 0f;
    
    float _displayedValue = 0f;
    public float displayedValue {
        get => _displayedValue;
        set {
            value = Mathf.Clamp(value, MIN, MAX);
            _displayedValue = value;

            int smallerValue = Mathf.FloorToInt(Mathf.Abs(value));
            int largerValue = Mathf.CeilToInt(Mathf.Abs(value));
            float t = Mathf.Abs(value) - smallerValue;

            //int roundedValue = Mathf.RoundToInt(value);
            bool isNegative = value < 0;
            currentValueDisplayLo.sprite = base9Symbols[smallerValue];
            currentValueDisplayLoAlpha = Mathf.Sqrt(1 - t);
            currentValueDisplayHi.sprite = base9Symbols[largerValue];
            currentValueDisplayHiAlpha = Mathf.Sqrt(t);

            currentValueNegativeSymbol.enabled = isNegative;
            if (isNegative) {
                if (smallerValue == 0) {
                    currentValueNegativeSymbolAlpha = t;
                }
                else {
                    currentValueNegativeSymbolAlpha = 1f;

                }
            }
            else {
                currentValueNegativeSymbolAlpha = 0f;
            }

            Color curColor = currentValueDisplayHi.color;
            curColor.a = currentValueDisplayHiAlpha;
            currentValueDisplayHi.color = curColor;

            curColor = currentValueDisplayLo.color;
            curColor.a = currentValueDisplayLoAlpha;
            currentValueDisplayLo.color = curColor;

            curColor = currentValueNegativeSymbol.color;
            curColor.a = currentValueNegativeSymbolAlpha;
            currentValueNegativeSymbol.color = curColor;
        }
    }
    
    private Color _color;
    [ShowNativeProperty]
    public Color color {
        get => _color;
        set {
            currentValueDisplayHi.color = value.WithAlphaFrom(currentValueDisplayHi.color);
            currentValueDisplayLo.color = value.WithAlphaFrom(currentValueDisplayLo.color);
            currentValueNegativeSymbol.color = value.WithAlphaFrom(currentValueNegativeSymbol.color);
            _color = value;
        }
    }

    // Start is called before the first frame update
    protected override void Start() {
        base.Start();
        base9Symbols = Resources.LoadAll<Sprite>("Images/Base9/").OrderBy(s => int.Parse(s.name)).ToArray();
    }

    // Update is called once per frame
    void Update() {
        if (DebugInput.GetKey("9")) {
            actualValue = Mathf.Clamp(actualValue + 1, MIN, MAX);
        }
        else if (DebugInput.GetKey("0")) {
            actualValue = Mathf.Clamp(actualValue - 1, MIN, MAX);
        }
        
        displayedValue = Mathf.Lerp(displayedValue, actualValue, Time.deltaTime * lerpSpeed);
    }

    #region Saving
    
    [Serializable]
    public class CurrentValueDisplaySave : SerializableSaveObject<CurrentValueDisplay> {
        private int actualValue;
        private float displayedValue;
        private float currentValueDisplayLoAlpha;
        private float currentValueDisplayHiAlpha;
        private float currentValueNegativeSymbolAlpha;
        
        public CurrentValueDisplaySave(CurrentValueDisplay script) : base(script) {
            this.actualValue = script.actualValue;
            this.displayedValue = script.displayedValue;
            this.currentValueDisplayLoAlpha = script.currentValueDisplayLoAlpha;
            this.currentValueDisplayHiAlpha = script.currentValueDisplayHiAlpha;
            this.currentValueNegativeSymbolAlpha = script.currentValueNegativeSymbolAlpha;
        }
        public override void LoadSave(CurrentValueDisplay script) {
            script.actualValue = this.actualValue;
            script.displayedValue = this.displayedValue;
            script.currentValueDisplayLoAlpha = this.currentValueDisplayLoAlpha;
            script.currentValueDisplayHiAlpha = this.currentValueDisplayHiAlpha;
            script.currentValueNegativeSymbolAlpha = this.currentValueNegativeSymbolAlpha;
        }
    }

    #endregion
}
