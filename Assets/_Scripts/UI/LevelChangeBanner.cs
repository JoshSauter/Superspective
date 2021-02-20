using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using LevelManagement;

public class LevelChangeBanner : Singleton<LevelChangeBanner> {
    CanvasGroup bannerGroup;
    [Serializable]
    public struct Banner {
        public Levels level;
        public GameObject banner;
    }
    public Banner[] banners;
    public Dictionary<Levels, GameObject> levelToBanner = new Dictionary<Levels, GameObject>();
    public Levels lastBannerLoaded = Levels.ManagerScene;
    public Levels queuedBanner = Levels.ManagerScene;
    public bool isPlayingBanner;

    float fadeTime = 2.5f;
    float displayTime = 4f;

    void Awake() {
        bannerGroup = GetComponent<CanvasGroup>();

        foreach (var banner in banners) {
            levelToBanner[banner.level] = banner.banner; 
        }
    }

    void Update() {
        if (!isPlayingBanner && queuedBanner != lastBannerLoaded && queuedBanner != Levels.ManagerScene) {
            PlayBanner(queuedBanner);
            queuedBanner = Levels.ManagerScene;
        }
    }

    public void PlayBanner(Levels level) {
        // If we're not already playing a banner
        if (!isPlayingBanner) {
            // If we have a banner prepared for this level
            if (levelToBanner.ContainsKey(level)) {
                StartCoroutine(PlayBannerCoroutine(level));
            }
        }
        // If we're playing a banner and attempting to queue up a different banner
        else if (level != lastBannerLoaded && levelToBanner.ContainsKey(level)){
            queuedBanner = level;
        }
        // If we're playing a banner and attempt to queue up the same banner
        else {
            queuedBanner = Levels.ManagerScene;
        }
    }

    IEnumerator PlayBannerCoroutine(Levels level) {
        // managerScene acts as a flag value for "not set"
        if (lastBannerLoaded != Levels.ManagerScene) {
            levelToBanner[lastBannerLoaded].SetActive(false);
        }
        levelToBanner[level].SetActive(true);

        lastBannerLoaded = level;

        isPlayingBanner = true;
        float timeElapsed = 0f;

        while (timeElapsed < fadeTime) {
            timeElapsed += Time.deltaTime;
            // If another banner is queued up, speed up the animation
            if (queuedBanner != Levels.ManagerScene && queuedBanner != level) {
                timeElapsed += 2 * Time.deltaTime;
            }
            float t = timeElapsed / fadeTime;

            bannerGroup.alpha = t*t;

            yield return null;
        }

        timeElapsed = 0f;
        while (timeElapsed < displayTime) {
            timeElapsed += Time.deltaTime;
            // If another banner is queued up, speed up the animation
            if (queuedBanner != Levels.ManagerScene && queuedBanner != level) {
                timeElapsed += 2 * Time.deltaTime;
            }

            yield return null;
        }

        timeElapsed = 0f;
        while (timeElapsed < fadeTime) {
            timeElapsed += Time.deltaTime;
            // If another banner is queued up, speed up the animation
            if (queuedBanner != Levels.ManagerScene && queuedBanner != level) {
                timeElapsed += 2 * Time.deltaTime;
            }
            float t = timeElapsed / fadeTime;

            bannerGroup.alpha = 1-Mathf.Sqrt(t);

            yield return null;
        }

        isPlayingBanner = false;
    }
}
