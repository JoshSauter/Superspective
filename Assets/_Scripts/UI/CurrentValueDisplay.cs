using System;
using Saving;
using SuperspectiveUtils;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class CurrentValueDisplay : ValueDisplay {
    public SpriteRenderer currentValueDisplayLo;
    
    private const float CURRENT_VALUE_DISPLAY_LO_ALPHA = 1f;
    private const float CURRENT_VALUE_DISPLAY_HI_ALPHA = 1f;
    private float currentValueNegativeSymbolAlpha = 0f;
    
    public new float SpriteAlpha {
        get => _spriteAlpha;
        set => _spriteAlpha = value;
    }
    
    public new float DisplayedValue {
        get => _displayedValue;
        private set {
            value = Mathf.Clamp(value, MIN, MAX);
            _displayedValue = value;

            int smallerValue = Mathf.FloorToInt(Mathf.Abs(value));
            int largerValue = Mathf.CeilToInt(Mathf.Abs(value));
            float t = Mathf.Abs(value) - smallerValue;

            //int roundedValue = Mathf.RoundToInt(value);
            bool isNegative = value < 0;
            currentValueDisplayLo.sprite = base9Symbols[smallerValue];
            float finalLoAlpha = CURRENT_VALUE_DISPLAY_LO_ALPHA * SpriteAlpha * Mathf.Sqrt(1 - t);
            currentValueDisplay.sprite = base9Symbols[largerValue];
            float finalHiAlpha = CURRENT_VALUE_DISPLAY_HI_ALPHA * SpriteAlpha * Mathf.Sqrt(t);

            currentValueNegativeSymbol.enabled = isNegative;
            if (isNegative) {
                if (smallerValue == 0) {
                    currentValueNegativeSymbolAlpha = t;
                }
                else {
                    currentValueNegativeSymbolAlpha = SpriteAlpha;

                }
            }
            else {
                currentValueNegativeSymbolAlpha = 0f;
            }

            currentValueDisplay.color = currentValueDisplay.color.WithAlpha(finalHiAlpha);
            currentValueDisplayLo.color = currentValueDisplayLo.color.WithAlpha(finalLoAlpha);
            currentValueNegativeSymbol.color = currentValueNegativeSymbol.color.WithAlpha(currentValueNegativeSymbolAlpha);
        }
    }

    protected override void ApplyColors(Color to) {
        currentValueDisplay.color = to.WithAlphaFrom(currentValueDisplay.color);
        currentValueDisplayLo.color = to.WithAlphaFrom(currentValueDisplayLo.color);
        currentValueNegativeSymbol.color = to.WithAlphaFrom(currentValueNegativeSymbol.color);
    }

    protected override void Update() {
        base.Update();

        DisplayedValue = Mathf.Lerp(DisplayedValue, actualValue, Time.deltaTime * lerpSpeed);
    }

    #region Saving

    public override SaveObject CreateSave() {
        return new CurrentValueDisplaySave(this);
    }

    public override void LoadSave(ValueDisplaySave save) {
        base.LoadSave(save);

        if (save is not CurrentValueDisplaySave currentValueDisplaySave) {
            Debug.LogError("Expected CurrentValueDisplaySave but got " + save.GetType(), this);
            return;
        }
        
        currentValueDisplayLo.color = currentValueDisplayLo.color.WithAlpha(currentValueDisplaySave.currentValueDisplayLoAlpha);
        currentValueDisplay.color = currentValueDisplay.color.WithAlpha(currentValueDisplaySave.currentValueDisplayHiAlpha);
        currentValueNegativeSymbol.color = currentValueNegativeSymbol.color.WithAlpha(currentValueDisplaySave.currentValueNegativeSymbolAlpha);
    }

    [Serializable]
    public class CurrentValueDisplaySave : ValueDisplaySave {
        public float currentValueDisplayLoAlpha;
        public float currentValueDisplayHiAlpha;
        public float currentValueNegativeSymbolAlpha;
        
        public CurrentValueDisplaySave(CurrentValueDisplay script) : base(script) {
            this.currentValueDisplayLoAlpha = script.currentValueDisplayLo.color.a;
            this.currentValueDisplayHiAlpha = script.currentValueDisplay.color.a;
            this.currentValueNegativeSymbolAlpha = script.currentValueNegativeSymbol.color.a;
        }
    }

    #endregion
}
