using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using UnityEngine.SocialPlatforms;
using PowerTrailMechanics;

namespace LevelSpecific.WhiteRoom {
    public class CentralHexes : MonoBehaviour {
        //////////////////////////
        // INSPECTOR REFERENCES //
        //////////////////////////
        public ParticleSystem outerParticles, innerParticles, laserToCubeCone, laserToCube;
        public CubeReceptacle startReceptacle, endReceptacle;
        public Transform outerHex, innerHex;
        public RotateObject outerHexRotate, innerHexRotate;
        public Color calmColor1, calmColor2;
        public Gradient enragedGradient1, enragedGradient2;
        public AnimationCurve enrageCameraShake;
        public PowerTrail powerTrailVerticalBlue, powerTrailVerticalGreen, powerTrailHorizontalBlue, powerTrailHorizontalGreen;
        public Renderer[] obeliskLights;
        public Color obeliskLightColorStart, obeliskLightColorEnd;
        [ColorUsage(true, true)]
        public Color obeliskLightEmissionColorStart, obeliskLightEmissionColorEnd;
        public Material whiteToBlack, blackToWhite;
        private Color white, black;
        public CubeReceptacle receptacleToRespawnIn;

        ///////////////////////////////
        // GENERIC STATE INFORMATION //
        ///////////////////////////////
        public enum State {
            Calm,
            Tracking,
            Enraged,
            Respawning
        }

        private State _state = State.Calm;
        public State state {
            get { return _state; }
            set { timeSinceLastStateChange = 0f; _state = value; }
        }
        public float timeSinceLastStateChange = 0f;

        //////////////////////
        // CALM INFORMATION //
        //////////////////////
        float colorResetLerpSpeed = 1f;

        //////////////////////////
        // TRACKING INFORMATION //
        //////////////////////////
        PickupObject cubeFollowing;
        Vector3 desiredOuterHexRotation = Vector3.zero;
        Vector3 desiredInnerHexRotation = Vector3.zero;
        float trackingSlerpSpeed = 20f;

        ////////////////////////
        // ENRAGE INFORMATION //
        ////////////////////////
        float enrageDuration = 6f;
        float enrageCameraShakeMultiplier = 2.5f;

        ////////////////////////////
        // RESPAWNING INFORMATION //
        ////////////////////////////
        float respawnDelay = 8f;
        float respawnTrackDuration = 2f;
        float respawnTrackSlerpSpeed = 4f;
        float respawnLaserTime = 1.5f;
        float cameraShakeDelay = 1f;
        float laserToRespawnDelay = 1.5f;
        float cameraShakeIntensityOnCubeRespawn = 2f;
        float respawnEndDelay = 0.75f;

        private const string emissionProp = "_EmissionColor";
        private const string colorProp = "_Color";
        private const string colorProp2 = "_Color2";

        void Start() {
            startReceptacle.OnCubeHoldStartSimple += StartCalm;
            startReceptacle.OnCubeReleaseEnd += StartTracking;

            endReceptacle.OnCubeHoldStartSimple += StartCalm;
            endReceptacle.OnCubeReleaseEnd += StartTracking;

            white = whiteToBlack.GetColor(colorProp);
            black = blackToWhite.GetColor(colorProp);

            StartCalm();
        }

        private void OnDisable() {
            ResetWhiteBlackFadeMaterials();
        }

        private void ResetWhiteBlackFadeMaterials() {
            whiteToBlack.SetColor(colorProp, white);
            whiteToBlack.SetColor(colorProp2, black);
            blackToWhite.SetColor(colorProp, black);
            blackToWhite.SetColor(colorProp2, white);
        }

        private void StartCalm() {
            state = State.Calm;
            CalmParticleSystem(outerParticles);
            CalmParticleSystem(innerParticles);

            laserToCubeCone.Stop();
            laserToCube.Stop();

            CameraShake.instance.CancelShake();

            outerHexRotate.enabled = true;
            innerHexRotate.enabled = true;
        }

        private void StartTracking(CubeReceptacle receptacle, PickupObject cube) {
            cubeFollowing = cube;
            state = State.Tracking;

            outerHexRotate.enabled = false;
            innerHexRotate.enabled = false;

            CalmParticleSystem(laserToCubeCone);
            CalmParticleSystem(laserToCube);
        }

        private void StartEnrage() {
            state = State.Enraged;

            CameraShake.instance.Shake(enrageDuration, enrageCameraShakeMultiplier, enrageCameraShake);

            EnrageParticleSystem(laserToCubeCone);
            EnrageParticleSystem(laserToCube);
            EnrageParticleSystem(outerParticles);
            EnrageParticleSystem(innerParticles);
        }

        private void StartRespawning() {
            state = State.Respawning;

            // Respawn depends on no external systems so it's straightforward to use a coroutine here
            StartCoroutine(RespawnCoroutine());
        }

        IEnumerator RespawnCoroutine() {
            // Despawn cube
            MaterializeObject cubeMaterialize = cubeFollowing.GetComponent<MaterializeObject>();
            cubeMaterialize.destroyObjectOnDematerialize = false;
            cubeMaterialize.Dematerialize();

            yield return new WaitForSeconds(respawnDelay);

            // Move cube
            float timeElapsed = 0f;
            cubeFollowing.transform.position = receptacleToRespawnIn.transform.position + receptacleToRespawnIn.transform.up * 1f;
            cubeFollowing.transform.rotation = Quaternion.Euler(0, 0, 0);

            // Turn on outer/inner particles
            CalmParticleSystem(outerParticles);
            CalmParticleSystem(innerParticles);
            // Track to the cube's new position
            while (timeElapsed < respawnTrackDuration) {
                timeElapsed += Time.deltaTime;

                TrackCube(respawnTrackSlerpSpeed);

                yield return null;
            }

            // Turn on the laser particles
            CalmParticleSystem(laserToCubeCone);
            CalmParticleSystem(laserToCube);
            yield return new WaitForSeconds(cameraShakeDelay);
            CameraShake.instance.Shake(laserToRespawnDelay + respawnLaserTime - cameraShakeDelay + 0.15f, 0f, cameraShakeIntensityOnCubeRespawn);
            yield return new WaitForSeconds(respawnLaserTime - cameraShakeDelay);
            laserToCubeCone.Stop();
            laserToCube.Stop();

            yield return new WaitForSeconds(laserToRespawnDelay);

            // Respawn cube
            cubeMaterialize.Materialize();
            cubeMaterialize.GetComponent<Collider>().enabled = false;

            yield return new WaitForSeconds(respawnEndDelay);

            cubeMaterialize.GetComponent<Collider>().enabled = true;
            float speedBefore = powerTrailHorizontalBlue.speed;
            powerTrailHorizontalBlue.speed = 2 * speedBefore;
            powerTrailHorizontalGreen.speed = 2 * speedBefore;
            powerTrailVerticalBlue.speed = 2 * speedBefore;
            powerTrailVerticalGreen.speed = 2 * speedBefore;
            cubeFollowing.interactable = false;

            yield return new WaitForSeconds(powerTrailHorizontalBlue.duration);

            powerTrailHorizontalBlue.speed = speedBefore;
            powerTrailHorizontalGreen.speed = speedBefore;
            powerTrailVerticalBlue.speed = speedBefore;
            powerTrailVerticalGreen.speed = speedBefore;
            cubeFollowing.interactable = true;
            Debug.LogError($"After {cubeFollowing.interactable}");

            StartCalm();
        }

        private void CalmParticleSystem(ParticleSystem particles) {
            ParticleSystem.MainModule particlesMain = particles.main;
            particlesMain.startColor = new ParticleSystem.MinMaxGradient(calmColor1, calmColor2);
            particlesMain.prewarm = false;
            particlesMain.loop = true;

            particles.Play();
        }

        private void EnrageParticleSystem(ParticleSystem particles) {
            ParticleSystem.MainModule particlesMain = particles.main;
            particlesMain.startColor = new ParticleSystem.MinMaxGradient(enragedGradient1, enragedGradient2);
            particlesMain.prewarm = true;
            particlesMain.loop = false;

            particles.Stop();
            particles.Play();
        }

        void Update() {
            float t;
            switch (state) {
                case State.Calm:
                    foreach (var obeliskLight in obeliskLights) {
                        Color curColor = obeliskLight.material.GetColor(colorProp);
                        Color curEmissionColor = obeliskLight.material.GetColor(emissionProp);
                        obeliskLight.material.SetColor(colorProp, Color.Lerp(curColor, obeliskLightColorStart, Time.deltaTime * colorResetLerpSpeed));
                        obeliskLight.material.SetColor(emissionProp, Color.Lerp(curEmissionColor, obeliskLightEmissionColorStart, Time.deltaTime * colorResetLerpSpeed));
                    }

                    Color whiteToBlackColor = whiteToBlack.GetColor(colorProp);
                    Color blackToWhiteColor = blackToWhite.GetColor(colorProp2);
                    whiteToBlack.SetColor(colorProp, Color.Lerp(whiteToBlackColor, white, Time.deltaTime * colorResetLerpSpeed));
                    blackToWhite.SetColor(colorProp2, Color.Lerp(blackToWhiteColor, white, Time.deltaTime * colorResetLerpSpeed));
                    break;
                case State.Tracking:
                    TrackCube(trackingSlerpSpeed);

                    t = 1 - powerTrailVerticalBlue.distance / powerTrailVerticalBlue.maxDistance;
                    foreach (var obeliskLight in obeliskLights) {
                        obeliskLight.material.SetColor(emissionProp, Color.Lerp(obeliskLightEmissionColorStart, obeliskLightEmissionColorEnd, t));
                    }

                    if (powerTrailVerticalBlue.distance == 0 && powerTrailVerticalGreen.distance == 0 &&
                        powerTrailHorizontalBlue.distance == 0 && powerTrailHorizontalGreen.distance == 0) {
                        StartEnrage();
                    }
                    break;
                case State.Enraged:
                    t = timeSinceLastStateChange / enrageDuration;
                    foreach (var obeliskLight in obeliskLights) {
                        obeliskLight.material.SetColor(colorProp, Color.Lerp(obeliskLightColorStart, obeliskLightColorEnd, t));
                    }
                    whiteToBlack.SetColor(colorProp, Color.Lerp(white, black, t));
                    blackToWhite.SetColor(colorProp2, Color.Lerp(white, black, t));
                    TrackCube(trackingSlerpSpeed);

                    if (t >= 1) {
                        StartRespawning();
                    }
                    break;
                case State.Respawning:
                    break;
            }

            timeSinceLastStateChange += Time.deltaTime;
        }

        void TrackCube(float trackSpeed) {
            Vector3 localPlayerPos = transform.InverseTransformDirection(cubeFollowing.transform.position - outerHex.position);
            desiredOuterHexRotation = new Vector3(Mathf.Rad2Deg * Mathf.Atan2(localPlayerPos.z, localPlayerPos.y), 0, 0);
            outerHex.localRotation = Quaternion.Slerp(outerHex.localRotation, Quaternion.Euler(desiredOuterHexRotation), trackSpeed * Time.deltaTime);

            localPlayerPos = outerHex.InverseTransformDirection(cubeFollowing.transform.position - innerHex.position);
            desiredInnerHexRotation = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(localPlayerPos.x, -localPlayerPos.y));
            innerHex.localRotation = Quaternion.Slerp(innerHex.localRotation, Quaternion.Euler(desiredInnerHexRotation), trackSpeed * Time.deltaTime);
        }
    }
}