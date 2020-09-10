using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTrailMechanics;
using Audio;

public class WhiteRoom3ExitBars : MonoBehaviour {
    public PowerTrail[] powerTrails;
    public Transform[] bars;
    public GameObject invisibleWall;

    public SoundEffect barsOpenSfx;

    public int numSolved = 0;
    bool wasSolvedLastFrame = false;
    bool solved => numSolved == powerTrails.Length;

    void Start() {
        foreach (var powerTrail in powerTrails) {
            powerTrail.OnPowerFinish += () => numSolved++;
            powerTrail.OnDepowerBegin += () => numSolved--;
		}
	}

    void Update() {
        if (solved && !wasSolvedLastFrame) {
            barsOpenSfx.Play();
		}

        invisibleWall.SetActive(!solved);
        foreach (var bar in bars) {
            Vector3 barPos = bar.transform.localPosition;
            Vector3 targetPos = new Vector3(barPos.x, barPos.y, solved ? 7.1f : 0);
            bar.transform.localPosition = Vector3.Lerp(barPos, targetPos, Time.deltaTime * 3f);
        }

        wasSolvedLastFrame = solved;
	}
}
