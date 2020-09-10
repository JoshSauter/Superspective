using Audio;
using PowerTrailMechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiteRoom3RoseBars : MonoBehaviour {
    public PowerTrail powerTrail;
    public Button powerButton;
    public GameObject invisibleWall;
    public GameObject[] bars;

    Vector3 targetBarPosition = new Vector3(0, 0, 11);

    public SoundEffect barsSfx;

    bool barsWereUpLastFrame = true;
    public bool barsAreUp = true;

    void Start() {
        powerButton.OnButtonPressBegin += (ctx) => powerTrail.powerIsOn = true;
        powerButton.OnButtonDepressFinish += (ctx) => powerTrail.powerIsOn = false;

        powerTrail.OnPowerFinish += () => barsAreUp = false;
        powerTrail.OnDepowerBegin += () => barsAreUp = true;
    }

	private void Update() {
        if (!barsAreUp && barsWereUpLastFrame) {
            barsSfx.Play();
		}

        invisibleWall.SetActive(barsAreUp);
        foreach (var bar in bars) {
            Vector3 barPos = bar.transform.localPosition;
            Vector3 targetPos = new Vector3(barPos.x, barPos.y, barsAreUp ? 0f : 11.1f);
            bar.transform.localPosition = Vector3.Lerp(barPos, targetPos, Time.deltaTime * 3f);
		}

        barsWereUpLastFrame = barsAreUp;
	}
}
