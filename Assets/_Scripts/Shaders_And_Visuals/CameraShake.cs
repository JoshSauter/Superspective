using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Saving;
using StateUtils;

// TODO: Make saveable? Not that important tho
[RequireComponent(typeof(UniqueId))]
public class CameraShake : SingletonSuperspectiveObject<CameraShake, CameraShake.CameraShakeSave> {
    private const float FULL_SHAKE_DISTANCE = 10f; // Distance within which the player will feel the full intensity of the shake
    private const float NO_SHAKE_DISTANCE = 150f; // Distance beyond which the player will feel no shake
    private const float RETURN_TO_CENTER_LERP_SPEED = 2f; // Speed at which we lerp the camera back to its original position after shaking
    private const float INTENSITY_TO_OFFSET_MULTPLIER = 1f; // Arbitrary "unit" conversion from intensity to offset (I like specifying values like 2.5f instead of .05f for shake intensity)
    
    public Vector2 totalOffsetApplied = Vector2.zero;
    
    private float SettingsIntensityMultiplier => Settings.Gameplay.CameraShake / 100f;

    private Transform PlayerCam => Player.instance.PlayerCam.transform;

    public class CameraShakeEvent {
        public float time = 0;
        public Func<Vector3> locationProvider = () => Vector3.zero;
        public float intensity = 1;
        public float duration = 1;
        public float spatial = 1; // 0 is 2D, 1 is 3D, between is partial 3D
        public AnimationCurve intensityCurve = AnimationCurve.Linear(0, 1, 1, 0);
    }
    private readonly HashSet<CameraShakeEvent> cameraShakeEvents = new HashSet<CameraShakeEvent>();

    /// <summary>
    /// Triggers a camera shake with the vibration originating from the value of locationProvider
    /// </summary>
    /// <param name="locationProvider">Evaluates each frame to determine the current location of the vibration</param>
    /// <param name="intensity">Intensity when the player is at the location of the vibration</param>
    /// <param name="duration">How long the vibration should last for</param>
    public CameraShakeEvent Shake(Func<Vector3> locationProvider, float intensity, float duration) {
        CameraShakeEvent shakeEvent = new CameraShakeEvent {
            time = 0,
            locationProvider = locationProvider,
            intensity = intensity,
            duration = duration,
            spatial = 1,
        };
        return AddShake(shakeEvent);
    }
    
    /// <summary>
    /// Triggers a camera shake with the vibration originating from the given location
    /// </summary>
    /// <param name="location">Location of the vibration</param>
    /// <param name="intensity">Intensity when the player is at the location of the vibration</param>
    /// <param name="duration">How long the vibration should last for</param>
    public CameraShakeEvent Shake(Vector3 location, float intensity, float duration) {
        CameraShakeEvent shakeEvent = new CameraShakeEvent {
            time = 0,
            locationProvider = () => location,
            intensity = intensity,
            duration = duration,
            spatial = 1
        };
        return AddShake(shakeEvent);
    }

    public CameraShakeEvent Shake(CameraShakeEvent shakeConfig) {
        return AddShake(shakeConfig);
    }

    /// <summary>
    /// Triggers a 2D camera shake that is the same no matter where the player is
    /// </summary>
    /// <param name="intensity">Intensity of the camera shake</param>
    /// <param name="duration">How long the camera shake should last for</param>
    public CameraShakeEvent Shake(float intensity, float duration) {
        CameraShakeEvent shakeEvent = new CameraShakeEvent() {
            time = 0,
            locationProvider = () => Vector3.zero,
            intensity = intensity,
            duration = duration,
            spatial = 0
        };
        return AddShake(shakeEvent);
    }

    private CameraShakeEvent AddShake(CameraShakeEvent shakeEvent) {
        cameraShakeEvents.Add(shakeEvent);
        return shakeEvent;
    }

    public void CancelShake(CameraShakeEvent shakeEvent) {
        cameraShakeEvents.Remove(shakeEvent);
    }

    void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;
        
        if (DebugInput.GetKey(KeyCode.LeftShift) && DebugInput.GetKeyDown(KeyCode.C)) {
            Shake(4, 2);
        }

        ShakeCamera(GetCurrentShakeIntensity());
    }

    /// <summary>
    /// Finds the maximum intensity of all of the camera shake events and returns it.
    /// Also takes care of updating the camera shake events and removing those that have expired.
    /// </summary>
    /// <returns>Maximum camera shake intensity among all of the events</returns>
    private float GetCurrentShakeIntensity() {
        float maxIntensity = 0f;
        
        // Evaluate all of the camera shake events, removing those that have expired
        List<CameraShakeEvent> eventsToRemove = new List<CameraShakeEvent>();
        foreach (CameraShakeEvent cameraShakeEvent in cameraShakeEvents) {
            float intensity = UpdateCameraShakeEvent(cameraShakeEvent);

            if (intensity < 0) {
                eventsToRemove.Add(cameraShakeEvent);
                continue;
            }
            
            maxIntensity = Mathf.Max(maxIntensity, intensity);
        }
        
        foreach (CameraShakeEvent cameraShakeEvent in eventsToRemove) {
            cameraShakeEvents.Remove(cameraShakeEvent);
        }
        
        return maxIntensity;
    }

    private void ShakeCamera(float curIntensity) {
        // Apply the settings intensity multiplier
        curIntensity *= SettingsIntensityMultiplier;

        // Generate a random offset based on the adjusted intensity
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle.normalized * (curIntensity * INTENSITY_TO_OFFSET_MULTPLIER);
    
        // Apply the random offset to the total offset applied
        Vector2 newTotalOffset = totalOffsetApplied + randomOffset;
    
        // Gradually return the camera to its original position
        newTotalOffset = Vector2.Lerp(newTotalOffset, Vector2.zero, RETURN_TO_CENTER_LERP_SPEED * Time.deltaTime);

    
        // Apply the final offset to the camera's position
        Vector3 finalOffset = new Vector3(newTotalOffset.x, newTotalOffset.y, 0f);
        PlayerCam.localPosition += finalOffset * Time.deltaTime;
        
        if (curIntensity > 0) {
            debug.Log($"Shake: {curIntensity}\nOffset before: {totalOffsetApplied}\nOffset after: {newTotalOffset}");
        }
    
        // Update the totalOffsetApplied to the final offset value
        totalOffsetApplied = newTotalOffset;
    }

    /// <summary>
    /// Helper function to update the CameraShakeEvents' timeShaking value and
    /// return the current intensity of the shake given the player's distance from the event's location and the event's intensity and duration
    /// </summary>
    /// <returns>The amount of shake that this event contributes to the overall camera shake, or -1 if this shake has concluded</returns>
    float UpdateCameraShakeEvent(CameraShakeEvent shakeEvent) {
        shakeEvent.time += Time.deltaTime;
        
        if (shakeEvent.time >= shakeEvent.duration || shakeEvent.locationProvider == null) {
            return -1;
        }

        try {
            Vector3 location = shakeEvent.locationProvider.Invoke();

            // Don't bother computing distance if the spatial is set to 2D
            if (shakeEvent.spatial == 0) {
                return shakeEvent.intensity * (1 - Mathf.InverseLerp(0, shakeEvent.duration, shakeEvent.time));
            }
        
            float distance = SuperspectivePhysics.ShortestDistance(Player.instance.transform.position, location);
            float rawDistanceMultiplier = Easing.EaseInOut(1 - Mathf.InverseLerp(FULL_SHAKE_DISTANCE, NO_SHAKE_DISTANCE, distance));
            float distanceMultiplier = Mathf.Lerp(1, rawDistanceMultiplier, shakeEvent.spatial);
            float intensity = shakeEvent.intensity * distanceMultiplier * shakeEvent.intensityCurve.Evaluate(shakeEvent.time / shakeEvent.duration);
        
            return intensity;
        }
        catch (Exception e) {
            debug.LogError($"Error in camera shake event: {e.Message}\n{e.StackTrace}");
            return -1;
        }
    }
    
#region Saving

    public override void LoadSave(CameraShakeSave save) {
        // TODO: Implement?
    }

    [Serializable]
	public class CameraShakeSave : SaveObject<CameraShake> {
        // TODO: Add save data here
        
		public CameraShakeSave(CameraShake script) : base(script) {}
	}
#endregion
}
