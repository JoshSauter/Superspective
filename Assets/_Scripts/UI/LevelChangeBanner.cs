using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Audio;
using LevelManagement;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LevelChangeBanner : SingletonSuperspectiveObject<LevelChangeBanner, LevelChangeBanner.LevelChangeBannerSave> {
    CanvasGroup bannerGroup;
    [Serializable]
    public struct Banner {
        public Image banner;
        public string overrideKey; // If this is set, this banner will be used instead of the level name
        public Levels level;
        public bool skipSfx;
    }
    public Banner[] banners;
    public Dictionary<string, Banner> levelToBanner = new Dictionary<string, Banner>();
    private HashSet<string> bannersPlayed = new HashSet<string>();
    
    public bool HasQueuedBanner => !string.IsNullOrEmpty(queuedBanner);
    public bool IsPlayingBanner => state == State.Playing || state == State.WaitingForLetterbox;
    
    public const float FADE_TIME = 2.5f;
    public const float DISPLAY_TIME = 4f;
    const float DEFAULT_BANNER_Y_MIDPOINT = .8f; // Height of non-raised, non-letterboxed level change banner

    public enum State : byte {
        NotPlaying,
        WaitingForLetterbox,
        Playing
    }
    public StateMachine<State> state;
    public string lastBannerLoaded = "";
    public string queuedBanner = "";
    public string currentlyPlayingBanner = "";

    protected override void Awake() {
        base.Awake();
        bannerGroup = GetComponent<CanvasGroup>();

        foreach (var banner in banners) {
            string key = banner.overrideKey != "" ? banner.overrideKey : banner.level.ToString();
            levelToBanner[key] = banner;
        }

        InitializeStateMachine();
    }

    private void InitializeStateMachine() {
        state = this.StateMachine(State.NotPlaying);
        
        Color originalColor = Color.clear;
        
        // Wait for letterboxing to appear, if necessary
        state.AddStateTransition(State.WaitingForLetterbox, State.Playing, () => !Letterboxing.instance.LetterboxingEnabled || Letterboxing.instance.state.Time >= Letterboxing.LETTERBOX_APPEAR_TIME);
        
        // When we start playing a banner, hide the last banner that was shown and show the new one
        void EnterPlayingState() {
            if (lastBannerLoaded != "" && lastBannerLoaded != currentlyPlayingBanner) {
                levelToBanner[lastBannerLoaded].banner.gameObject.SetActive(false);
            }
            lastBannerLoaded = currentlyPlayingBanner;
            
            // Trigger letterboxing, if needed
            Letterboxing.instance.TurnOnLetterboxing();
            if (Letterboxing.instance.LetterboxingEnabled && Letterboxing.instance.state.Time < Letterboxing.LETTERBOX_APPEAR_TIME) {
                // If we need to wait for the letterbox to appear, temporarily enter the waiting state
                // When the letterbox appears, we'll transition to back to the playing state but this condition will fail
                // thus allowing the actual playing to begin
                state.Set(State.WaitingForLetterbox);
                return;
            }

            bannerGroup.alpha = 0;
            Image bannerImage = levelToBanner[currentlyPlayingBanner].banner;
            bannerImage.gameObject.SetActive(true);
            RectTransform bannerRect = bannerImage.rectTransform;
            
            originalColor = bannerImage.color;

            // Position the banner in the appropriate position
            float bannerYMidpoint = DEFAULT_BANNER_Y_MIDPOINT;
            Vector2 halfBannerSize = (bannerRect.anchorMax - bannerRect.anchorMin) / 2f;
            bool placeBannerAtTopOfScreen = Letterboxing.instance.LetterboxingEnabled || CameraFlythrough.instance.isPlayingFlythrough;
            if (placeBannerAtTopOfScreen) {
                bannerYMidpoint = 1 - halfBannerSize.y;
            }
            bannerRect.anchorMin = new Vector2(0.5f - halfBannerSize.x, bannerYMidpoint - halfBannerSize.y);
            bannerRect.anchorMax = new Vector2(0.5f + halfBannerSize.x, bannerYMidpoint + halfBannerSize.y);
            
            if (placeBannerAtTopOfScreen && originalColor.WithAlpha(1).Distance(Color.black) < .1f) {
                // Black text on black letterboxing is unreadable, so we'll make it white
                bannerImage.color = Color.white.WithAlphaFrom(bannerImage.color);
            }
        }
        state.AddTrigger(State.Playing, EnterPlayingState);
        state.AddTrigger(State.Playing, 1.5f, () => {
            if (Time.time > 10f && !bannersPlayed.Contains(currentlyPlayingBanner)) {
                bannersPlayed.Add(currentlyPlayingBanner);
                if (!levelToBanner[currentlyPlayingBanner].skipSfx) {
                    AudioManager.instance.Play(AudioName.LevelChangeSting);
                }
            }
        });
        state.AddTrigger(State.Playing, FADE_TIME + DISPLAY_TIME, () => Letterboxing.instance.TurnOffLetterboxing());
        
        state.AddTrigger(State.NotPlaying, () => {
            currentlyPlayingBanner = "";
        });
        
        state.WithUpdate(State.Playing, time => {
            float timeElapsed = time;

            // Fade the banner in
            if (timeElapsed < FADE_TIME) {
                if (!CameraFlythrough.instance.isPlayingFlythrough) {
                    // If another banner is queued up, speed up the fade-in animation
                    if (HasQueuedBanner && queuedBanner != currentlyPlayingBanner) {
                        state.Time += 2 * Time.deltaTime;
                        timeElapsed = state.Time;
                    }

                    float t = timeElapsed / FADE_TIME;

                    bannerGroup.alpha = t*t;
                }

                return;
            }
            
            // Display the banner
            timeElapsed -= FADE_TIME;
            if (timeElapsed < DISPLAY_TIME) {
                // If another banner is queued up, speed up the animation
                if (HasQueuedBanner && queuedBanner != currentlyPlayingBanner) {
                    state.Time += 2 * Time.deltaTime;
                }

                return;
            }
            
            // Fade the banner out
            timeElapsed -= DISPLAY_TIME;
            if (timeElapsed < FADE_TIME) {
                // If another banner is queued up, speed up the animation
                if (HasQueuedBanner && queuedBanner != currentlyPlayingBanner) {
                    state.Time += 2 * Time.deltaTime;
                    timeElapsed = state.Time - FADE_TIME - DISPLAY_TIME;
                }

                float t = timeElapsed / FADE_TIME;

                bannerGroup.alpha = 1 - Mathf.Sqrt(t);
                return;
            }
            
            // Transition to next state
            bannerGroup.alpha = 0;
            levelToBanner[currentlyPlayingBanner].banner.color = originalColor;
            if (HasQueuedBanner && queuedBanner != currentlyPlayingBanner) {
                currentlyPlayingBanner = queuedBanner;
                queuedBanner = "";
                state.Set(State.Playing, true);
                EnterPlayingState();
            }
            else {
                state.Set(State.NotPlaying);
            }
        });
        
        state.WithUpdate(State.NotPlaying, _ => {
            bannerGroup.alpha = 0;
        });
    }

    void Update() {
        if (!IsPlayingBanner && HasQueuedBanner && queuedBanner != lastBannerLoaded) {
            PlayBanner(queuedBanner);
            queuedBanner = "";
        }
    }

    public void PlayBanner(string key) {
        debug.Log("Playing banner for " + key);
        // If we're not already playing a banner
        if (!IsPlayingBanner) {
            // If we have a banner prepared for this level
            if (levelToBanner.ContainsKey(key)) {
                currentlyPlayingBanner = key;
                state.Set(State.Playing);
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
    
    public void CancelAllBanners() {
        queuedBanner = "";
        currentlyPlayingBanner = "";
        state.Set(State.NotPlaying);
        foreach (Banner banner in banners) {
            banner.banner.gameObject.SetActive(false);
        }
    }
    
#region Saving

    public override void LoadSave(LevelChangeBannerSave save) {
        CancelAllBanners();
        
        lastBannerLoaded = save.lastBannerLoaded;
        queuedBanner = save.queuedBanner;
        currentlyPlayingBanner = save.currentlyPlayingBanner;
        state.LoadFromSave(save.state);
    }

    [Serializable]
    public class LevelChangeBannerSave : SaveObject<LevelChangeBanner> {
        public StateMachine<State>.StateMachineSave state;
        public string lastBannerLoaded;
        public string queuedBanner;
        public string currentlyPlayingBanner;
        
        public LevelChangeBannerSave(LevelChangeBanner script) : base(script) {
            this.lastBannerLoaded = script.lastBannerLoaded;
            this.queuedBanner = script.queuedBanner;
            this.currentlyPlayingBanner = script.currentlyPlayingBanner;
            this.state = script.state.ToSave();
        }
    }
#endregion
}
