using System.Collections;
using System.Collections.Generic;
using Audio;
using UnityEngine;

// For when you just want to play a sound through a UnityEvent or something
public class PlayGlobalSound : MonoBehaviour {
    public AudioName audio;
    public string ID;

    public void Play() {
        AudioManager.instance.Play(audio, ID, true);
    }
}
