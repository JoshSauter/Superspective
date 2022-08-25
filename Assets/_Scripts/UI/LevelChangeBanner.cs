using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using LevelManagement;
using UnityEngine.UI;

public class LevelChangeBanner : Singleton<LevelChangeBanner> {
    CanvasGroup bannerGroup;
    [Serializable]
    public struct Banner {
        public Levels level;
        public Image banner;
    }
    public Banner[] banners;
    public Dictionary<Levels, Image> levelToBanner = new Dictionary<Levels, Image>();
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

    // TODO: Make separate coroutine to track banner position and color state each frame. EDIT: Why though?
    IEnumerator PlayBannerCoroutine(Levels level) {
        // TODO: Temporarily disabled while I test Nova UI stuff
        yield break;
        
        // managerScene acts as a flag value for "not set"
        if (lastBannerLoaded != Levels.ManagerScene) {
            levelToBanner[lastBannerLoaded].gameObject.SetActive(false);
        }
        RectTransform banner = levelToBanner[level].rectTransform;
        Image bannerImage = levelToBanner[level];
        banner.gameObject.SetActive(true);

        lastBannerLoaded = level;

        isPlayingBanner = true;
        float timeElapsed = 0f;
        float lerpToMatchFlythroughCamSpeed = 2f;
        Color originalColor = bannerImage.color;
        float defaultBannerYMidpoint = (banner.anchorMax.y + banner.anchorMin.y) / 2f;
        float bannerYMidpoint = defaultBannerYMidpoint;
        Vector2 halfBannerSize = (banner.anchorMax - banner.anchorMin) / 2f;

        while (timeElapsed < fadeTime) {
            // Hold the LevelChangeBanner up if we're doing a CameraFlythrough
            if (!CameraFlythrough.instance.isPlayingFlythrough) {
                float timeElapsedThisFrame = Time.deltaTime;
                // If another banner is queued up, speed up the animation
                if (queuedBanner != Levels.ManagerScene && queuedBanner != level) {
                    timeElapsedThisFrame += 2 * Time.deltaTime;
                }

                timeElapsed += timeElapsedThisFrame;
                float t = timeElapsed / fadeTime;
                
                bannerImage.color = Color.Lerp(bannerImage.color, originalColor, lerpToMatchFlythroughCamSpeed * Time.deltaTime);
                bannerYMidpoint = Mathf.Lerp(bannerYMidpoint, defaultBannerYMidpoint, lerpToMatchFlythroughCamSpeed * Time.deltaTime);

                bannerGroup.alpha = t*t;
            }
            else {
                bannerImage.color = Color.Lerp(bannerImage.color, Color.clear, lerpToMatchFlythroughCamSpeed * Time.deltaTime);
                bannerYMidpoint = Mathf.Lerp(bannerYMidpoint, 1 - halfBannerSize.y, lerpToMatchFlythroughCamSpeed * Time.deltaTime);
                // bannerYMidpoint = 1 - halfBannerSize.y;
            }

            banner.anchorMin = new Vector2(0.5f - halfBannerSize.x, bannerYMidpoint - halfBannerSize.y);
            banner.anchorMax = new Vector2(0.5f + halfBannerSize.x, bannerYMidpoint + halfBannerSize.y);
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

    // IEnumerator BannerColorAndPositionTracker(Levels level) {
    //     
    // }
}
