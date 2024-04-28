using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using LevelManagement;
using SuperspectiveUtils;
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
    public bool HasQueuedBanner => queuedBanner != Levels.ManagerScene;
    public bool isPlayingBanner;

    public const float FADE_TIME = 2.5f;
    public const float DISPLAY_TIME = 4f;

    void Awake() {
        bannerGroup = GetComponent<CanvasGroup>();

        foreach (var banner in banners) {
            levelToBanner[banner.level] = banner.banner;
        }
    }

    void Update() {
        if (!isPlayingBanner && HasQueuedBanner && queuedBanner != lastBannerLoaded) {
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
        // managerScene acts as a flag value for "not set"
        if (lastBannerLoaded != Levels.ManagerScene) {
            levelToBanner[lastBannerLoaded].gameObject.SetActive(false);
        }
        RectTransform banner = levelToBanner[level].rectTransform;
        Image bannerImage = levelToBanner[level];
        banner.gameObject.SetActive(true);

        lastBannerLoaded = level;

        Letterboxing.instance.TurnOnLetterboxing();
        isPlayingBanner = true;
        float timeElapsed = 0f;
        float lerpToMatchFlythroughCamSpeed = 2f;
        Color originalColor = bannerImage.color;
        float defaultBannerYMidpoint = .8f; // Height of non-raised, non-letterboxed level change banner
        float bannerYMidpoint = defaultBannerYMidpoint;
        Vector2 halfBannerSize = (banner.anchorMax - banner.anchorMin) / 2f;

        bool placeBannerAtTopOfScreen = Letterboxing.instance.LetterboxingEnabled || CameraFlythrough.instance.isPlayingFlythrough;
        bannerYMidpoint = placeBannerAtTopOfScreen ? 1 - halfBannerSize.y : defaultBannerYMidpoint;
        banner.anchorMin = new Vector2(0.5f - halfBannerSize.x, bannerYMidpoint - halfBannerSize.y);
        banner.anchorMax = new Vector2(0.5f + halfBannerSize.x, bannerYMidpoint + halfBannerSize.y);

        if (placeBannerAtTopOfScreen && originalColor.WithAlpha(1).Distance(Color.black) < .1f) {
            // Black-on-black text is unreadable, so we'll make it white
            bannerImage.color = Color.white.WithAlphaFrom(bannerImage.color);
        }

        // Wait for letterbox to appear before showing the banner
        while (Letterboxing.instance.LetterboxingEnabled && Letterboxing.instance.state.timeSinceStateChanged < Letterboxing.LETTERBOX_APPEAR_TIME) {
            yield return null;
        }

        while (timeElapsed < FADE_TIME) {
            if (!CameraFlythrough.instance.isPlayingFlythrough) {
                float timeElapsedThisFrame = Time.deltaTime;
                // If another banner is queued up, speed up the animation
                if (HasQueuedBanner && queuedBanner != level) {
                    timeElapsedThisFrame += 2 * Time.deltaTime;
                }

                timeElapsed += timeElapsedThisFrame;
                float t = timeElapsed / FADE_TIME;

                bannerGroup.alpha = t*t;
            }

            yield return null;
        }

        timeElapsed = 0f;
        while (timeElapsed < DISPLAY_TIME) {
            timeElapsed += Time.deltaTime;
            // If another banner is queued up, speed up the animation
            if (HasQueuedBanner && queuedBanner != level) {
                timeElapsed += 2 * Time.deltaTime;
            }

            yield return null;
        }

        Letterboxing.instance.TurnOffLetterboxing();
        timeElapsed = 0f;
        while (timeElapsed < FADE_TIME) {
            timeElapsed += Time.deltaTime;
            // If another banner is queued up, speed up the animation
            if (queuedBanner != Levels.ManagerScene && queuedBanner != level) {
                timeElapsed += 2 * Time.deltaTime;
            }
            float t = timeElapsed / FADE_TIME;

            bannerGroup.alpha = 1-Mathf.Sqrt(t);

            yield return null;
        }
        
        bannerImage.color = originalColor;
        
        isPlayingBanner = false;
    }
}
