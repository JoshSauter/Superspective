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
        public string overrideKey; // If this is set, this banner will be used instead of the level name
        public Image banner;
    }
    public Banner[] banners;
    public Dictionary<string, Image> levelToBanner = new Dictionary<string, Image>();
    public string lastBannerLoaded = "";
    public string queuedBanner = "";
    public bool HasQueuedBanner => !string.IsNullOrEmpty(queuedBanner);
    public bool isPlayingBanner;

    public const float FADE_TIME = 2.5f;
    public const float DISPLAY_TIME = 4f;

    void Awake() {
        bannerGroup = GetComponent<CanvasGroup>();

        foreach (var banner in banners) {
            string key = banner.overrideKey != "" ? banner.overrideKey : banner.level.ToString();
            levelToBanner[key] = banner.banner;
        }
    }

    void Update() {
        if (!isPlayingBanner && HasQueuedBanner && queuedBanner != lastBannerLoaded) {
            PlayBanner(queuedBanner);
            queuedBanner = "";
        }
    }

    public void PlayBanner(string key) {
        // If we're not already playing a banner
        if (!isPlayingBanner) {
            // If we have a banner prepared for this level
            if (levelToBanner.ContainsKey(key)) {
                StartCoroutine(PlayBannerCoroutine(key));
            }
        }
        // If we're playing a banner and attempting to queue up a different banner
        else if (key != lastBannerLoaded && levelToBanner.ContainsKey(key)){
            queuedBanner = key;
        }
        // If we're playing a banner and attempt to queue up the same banner
        else {
            queuedBanner = "";
        }
    }

    public void PlayBanner(Levels level) => PlayBanner(level.ToString());

    // TODO: Make separate coroutine to track banner position and color state each frame. EDIT: Why though?
    IEnumerator PlayBannerCoroutine(string key) {
        if (lastBannerLoaded != "") {
            levelToBanner[lastBannerLoaded].gameObject.SetActive(false);
        }
        RectTransform banner = levelToBanner[key].rectTransform;
        Image bannerImage = levelToBanner[key];
        bannerGroup.alpha = 0;
        banner.gameObject.SetActive(true);

        lastBannerLoaded = key;

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
        while (Letterboxing.instance.LetterboxingEnabled && Letterboxing.instance.state.Time < Letterboxing.LETTERBOX_APPEAR_TIME) {
            yield return null;
        }

        while (timeElapsed < FADE_TIME) {
            if (!CameraFlythrough.instance.isPlayingFlythrough) {
                float timeElapsedThisFrame = Time.deltaTime;
                // If another banner is queued up, speed up the animation
                if (HasQueuedBanner && queuedBanner != key) {
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
            if (HasQueuedBanner && queuedBanner != key) {
                timeElapsed += 2 * Time.deltaTime;
            }

            yield return null;
        }

        Letterboxing.instance.TurnOffLetterboxing();
        timeElapsed = 0f;
        while (timeElapsed < FADE_TIME) {
            timeElapsed += Time.deltaTime;
            // If another banner is queued up, speed up the animation
            if (HasQueuedBanner && queuedBanner != key) {
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
