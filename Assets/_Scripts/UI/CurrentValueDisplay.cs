using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Saving;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(UniqueId))]
public class CurrentValueDisplay : ValueDisplay {
    public SpriteRenderer currentValueDisplayLo;
    
    private float currentValueDisplayLoAlpha = 1f;
    private float currentValueDisplayHiAlpha = 1f;
    private float currentValueNegativeSymbolAlpha = 0f;
    
    public new float spriteAlpha {
        get => _spriteAlpha;
        set => _spriteAlpha = value;
    }
    
    public new float displayedValue {
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
            float finalLoAlpha = currentValueDisplayLoAlpha * spriteAlpha * Mathf.Sqrt(1 - t);
            currentValueDisplay.sprite = base9Symbols[largerValue];
            float finalHiAlpha = currentValueDisplayHiAlpha * spriteAlpha * Mathf.Sqrt(t);

            currentValueNegativeSymbol.enabled = isNegative;
            if (isNegative) {
                if (smallerValue == 0) {
                    currentValueNegativeSymbolAlpha = t;
                }
                else {
                    currentValueNegativeSymbolAlpha = spriteAlpha;

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
            throw new NotImplementedException("CurrentValueDisplaySave");
        }
        public override void LoadSave(CurrentValueDisplay script) {
            throw new NotImplementedException("CurrentValueDisplaySave");
        }
    }

    #endregion
}
