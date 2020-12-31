using EpitaphUtils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WhiteRoomPuzzle2 : MonoBehaviour {
    const int target = 47;

    int actualValue = 0;
    float _displayedValue = 0f;
    float displayedValue {
        get {
            return _displayedValue;
		}
        set {
            value = Mathf.Clamp(value, -80f, 80f);
            _displayedValue = value;

            int roundedValue = Mathf.RoundToInt(value);
            bool isNegative = roundedValue < 0;
            roundedValue = Mathf.Abs(roundedValue);
            currentValueDisplay.text = Base9FontConversions.ValueToBase9Char[roundedValue].ToString();
            currentValueNegativeSymbol.enabled = isNegative;
		}
	}

    public TextMeshPro currentValueDisplay;
    public SpriteRenderer currentValueNegativeSymbol;

    void Start() {
    }

	private void Update() {
        if (Input.GetKeyDown("9")) {
            actualValue = Mathf.Clamp(actualValue + 15, -80, 80);
        }
        else if (Input.GetKeyDown("0")) {
            actualValue = Mathf.Clamp(actualValue - 15, -80, 80);
        }

        displayedValue = actualValue;//Mathf.Lerp(displayedValue, actualValue, Time.deltaTime * 4f);
	}
}
