using EpitaphUtils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;
using System;
using PowerTrailMechanics;
using Saving;
using SerializableClasses;

namespace LevelSpecific.WhiteRoom {
    public class WhiteRoomPuzzle2 : SaveableObject<WhiteRoomPuzzle2, WhiteRoomPuzzle2.WhiteRoomPuzzle2Save> {
        [Serializable]
        public class PowerReceptacle {
            public int powerGenerated;
            public CubeReceptacle cubeReceptacle;
            public PowerTrail powerTrail;
        }

        Sprite[] base9Symbols;
        public SpriteRenderer currentValueBackground;
        public SpriteRenderer currentValueDisplayHi;
        public SpriteRenderer currentValueDisplayLo;
        public SpriteRenderer currentValueNegativeSymbol;
        public PowerReceptacle[] powerReceptacles;

        const int target = 47;

        int actualValue = 0;
        float _displayedValue = 0f;
        float displayedValue {
            get {
                return _displayedValue;
            }
            set {
                value = Mathf.Clamp(value, -80f, 80f);
                _displayedValue = value;

                int smallerValue = Mathf.FloorToInt(Mathf.Abs(value));
                int largerValue = Mathf.CeilToInt(Mathf.Abs(value));
                float t = Mathf.Abs(value) - smallerValue;

                //int roundedValue = Mathf.RoundToInt(value);
                bool isNegative = value < 0;
                currentValueDisplayLo.sprite = base9Symbols[smallerValue];
                currentValueDisplayLoAlpha = 1f - t;
                currentValueDisplayHi.sprite = base9Symbols[largerValue];
                currentValueDisplayHiAlpha = t;

                currentValueNegativeSymbol.enabled = isNegative;
                if (isNegative) {
                    if (smallerValue == 0) {
                        currentValueNegativeSymbolAlpha = t;
                    }
                    else {
                        currentValueNegativeSymbolAlpha = 1f;

                    }
                }
                else {
                    currentValueNegativeSymbolAlpha = 0f;
				}
            }
        }
        float currentValueDisplayLoAlpha = 1f;
        float currentValueDisplayHiAlpha = 0f;
        float currentValueNegativeSymbolAlpha = 0f;

        public Material whiteToBlack, blackToWhite;
        public Color white, black;
        const string colorProp = "_Color";
        const string colorProp2 = "_Color2";

        // Hexes react to the player looking at them by looking back
        public enum HexState {
            ValueInRangeAndRotating,
            LookingAtPlayer,
            ValueOutOfRange,
            CorrectValue
        }

        HexState _hexState;
        public HexState hexState {
            get { return _hexState; }
            set {
                if (value == hexState) {
                    return;
                }

                _hexState = value;
                timeSinceHexStateChanged = 0f;
            }
        }
        float timeSinceHexStateChanged = 0f;
        public bool solved => hexState == HexState.CorrectValue;
        Transform playerCamera;
        public Transform outerHex, innerHex;
        public Gradient lowerBoundColors;
        public Gradient upperBoundColors;
        public ParticleSystem outerParticles, innerParticles;
        public ParticleSystem laserToReceiverStart, laserToReceiver;
        RotateObject outerHexRotate, innerHexRotate;
        float outerHexRotateObjectSpeed, innerHexRotateObjectSpeed;
        const float valueOutOfRangeRotationSpeedMultiplier = 8f;
        Vector3 desiredOuterHexRotation, desiredInnerHexRotation;
        const float trackingSlerpSpeed = 4f;
        const float isLookingAtThreshold = 0.825f;

        public PowerTrail powerTrailToPortal;
        public Lightpost laserReceiver;

        public MeshRenderer obeliskLight;
        Shader obeliskLightShader;
        Material obeliskLightMaterial;
        // Allocate once to save GC every frame
        const int GRADIENT_ARRAY_SIZE = 10;
        readonly float[] floatGradientBuffer = new float[GRADIENT_ARRAY_SIZE];
        readonly Color[] colorGradientBuffer = new Color[GRADIENT_ARRAY_SIZE];
        public Transform valueLine;
        readonly Vector3 valueLineTop = new Vector3(0, 46, 0);
        readonly Vector3 valueLIneBot = new Vector3(0, 21.74f, 0);
        readonly Vector3 valueLineScaleMin = new Vector3(0.82f, 1f, 0.82f);
        readonly Vector3 valueLineScaleMax = new Vector3(1.175f, 1f, 1.165f);

        protected override void Awake() {
            base.Awake();
            outerHexRotate = outerHex.GetComponent<RotateObject>();
            innerHexRotate = innerHex.GetComponent<RotateObject>();
            outerHexRotateObjectSpeed = outerHexRotate.rotationsPerSecondY;
            innerHexRotateObjectSpeed = innerHexRotate.rotationsPerSecondZ;

            obeliskLightShader = Shader.Find("Custom/GradientUnlitEmissive");
            obeliskLightMaterial = new Material(obeliskLightShader) {
                hideFlags = HideFlags.HideAndDontSave
            };
            obeliskLight.material = obeliskLightMaterial;
            
            white = Resources.Load<Material>("Materials/Unlit/Unlit").GetColor(colorProp);
            black = Resources.Load<Material>("Materials/Unlit/UnlitBlack").GetColor(colorProp);
        }

        protected override void Start() {
            base.Start();
            playerCamera = EpitaphScreen.instance.playerCamera.transform;

            base9Symbols = Resources.LoadAll<Sprite>("Images/Base9/").OrderBy(s => int.Parse(s.name)).ToArray();
        }

        protected override void Init() {
            foreach (var powerReceptacle in powerReceptacles) {
                powerReceptacle.powerTrail.OnPowerFinish += () => actualValue += powerReceptacle.powerGenerated;
                powerReceptacle.powerTrail.OnDepowerBegin += () => actualValue -= powerReceptacle.powerGenerated;
            }
        }

        void OnDisable() {
            ResetWhiteBlackFadeMaterials();

            if (obeliskLightMaterial != null) {
                DestroyImmediate(obeliskLightMaterial);
                obeliskLightMaterial = null;
            }
        }

        void ResetWhiteBlackFadeMaterials() {
            whiteToBlack.SetColor(colorProp, white);
            whiteToBlack.SetColor(colorProp2, black);
            blackToWhite.SetColor(colorProp, black);
            blackToWhite.SetColor(colorProp2, white);
        }

        void Update() {
            if (!hasInitialized) return;
            
            if (Input.GetKey("9")) {
                actualValue = Mathf.Clamp(actualValue + 1, -80, 80);
            }
            else if (Input.GetKey("0")) {
                actualValue = Mathf.Clamp(actualValue - 1, -80, 80);
            }

            foreach (var powerReceptacle in powerReceptacles) {
                powerReceptacle.powerTrail.powerIsOn = powerReceptacle.cubeReceptacle.isCubeInReceptacle;
			}

            displayedValue = Mathf.Lerp(displayedValue, actualValue, Time.deltaTime * 4f);

            float amountPlayerIsLookingAtHexes = Vector3.Dot(playerCamera.forward, (transform.position - playerCamera.position).normalized);
            bool valueInRange = (80 - Mathf.Abs(displayedValue) > 0);
            UpdateHexesState(amountPlayerIsLookingAtHexes, valueInRange);

            float t = Mathf.InverseLerp(-80f, 80f, displayedValue);
            UpdateObeliskLight(t);
            UpdateValueLine(t);

            UpdateParticleSystems();
        }

        void UpdateHexesState(float amountPlayerIsLookingAtHexes, bool valueInRange) {
            if (!valueInRange) {
                hexState = HexState.ValueOutOfRange;
            }
            else if (actualValue == target) {
                hexState = HexState.CorrectValue;
			}
            else if (amountPlayerIsLookingAtHexes > isLookingAtThreshold) {
                hexState = HexState.LookingAtPlayer;
            }
            else {
                hexState = HexState.ValueInRangeAndRotating;
            }

            float rotationLerpSpeed = 0.2f;
            if (hexState != HexState.ValueOutOfRange) {
                float t = timeSinceHexStateChanged;

                Color whiteToBlackColor = whiteToBlack.GetColor(colorProp);
                Color blackToWhiteColor = blackToWhite.GetColor(colorProp2);
                whiteToBlack.SetColor(colorProp, Color.Lerp(whiteToBlackColor, white, t));
                blackToWhite.SetColor(colorProp2, Color.Lerp(blackToWhiteColor, white, t));
            }

            // Make sure we aren't running laser-to-receiver particle systems if correct value hasn't been found
            if (hexState != HexState.CorrectValue) {
                if (laserToReceiverStart.isPlaying) laserToReceiverStart.Stop();
                if (laserToReceiver.isPlaying) laserToReceiver.Stop();
                powerTrailToPortal.powerIsOn = false;
            }

            switch (hexState) {
                case HexState.ValueInRangeAndRotating:
                    innerHexRotate.enabled = true;
                    outerHexRotate.enabled = true;
                    outerHexRotate.rotationsPerSecondY = Mathf.Lerp(outerHexRotate.rotationsPerSecondY, outerHexRotateObjectSpeed, rotationLerpSpeed * Time.deltaTime);
                    innerHexRotate.rotationsPerSecondZ = Mathf.Lerp(innerHexRotate.rotationsPerSecondZ, innerHexRotateObjectSpeed, rotationLerpSpeed * Time.deltaTime);

                    Color curColor = currentValueDisplayHi.color;
                    curColor.a = 0;
                    currentValueDisplayLo.color = curColor;
                    currentValueDisplayHi.color = curColor;
                    currentValueNegativeSymbol.color = curColor;
                    currentValueBackground.color = Color.clear;
                    break;
                case HexState.LookingAtPlayer:
                    innerHexRotate.enabled = false;
                    outerHexRotate.enabled = false;

                    TrackPosition(trackingSlerpSpeed, playerCamera.position);
                    float lookingAtLerpValue = Mathf.InverseLerp(isLookingAtThreshold, 0.975f, Mathf.Abs(amountPlayerIsLookingAtHexes));

                    // Update the high value's alpha
                    curColor = currentValueDisplayHi.color;
                    curColor.a = currentValueDisplayHiAlpha * lookingAtLerpValue;
                    currentValueDisplayHi.color = curColor;

                    // Update the low value's alpha
                    curColor = currentValueDisplayLo.color;
                    curColor.a = currentValueDisplayLoAlpha * lookingAtLerpValue;
                    currentValueDisplayLo.color = curColor;

                    // Update the negative symbol's alpha
                    currentValueNegativeSymbol.color = new Color(1, 1, 1, currentValueNegativeSymbolAlpha * lookingAtLerpValue);
                    currentValueBackground.color = new Color(0, 0, 0, lookingAtLerpValue);
                    break;
                case HexState.ValueOutOfRange:
                    // Re-enable and speed up rotations
                    innerHexRotate.enabled = true;
                    outerHexRotate.enabled = true;
                    outerHexRotate.rotationsPerSecondY = Mathf.Lerp(outerHexRotate.rotationsPerSecondY, outerHexRotateObjectSpeed * valueOutOfRangeRotationSpeedMultiplier, rotationLerpSpeed * Time.deltaTime);
                    innerHexRotate.rotationsPerSecondZ = Mathf.Lerp(innerHexRotate.rotationsPerSecondZ, innerHexRotateObjectSpeed * valueOutOfRangeRotationSpeedMultiplier, rotationLerpSpeed * Time.deltaTime);

                    // After some time, fade the room and then pop cubes out
                    if (timeSinceHexStateChanged > 3f) {
                        float t = Mathf.InverseLerp(3f, 6f, timeSinceHexStateChanged);
                        whiteToBlack.SetColor(colorProp, Color.Lerp(white, black, t));
                        blackToWhite.SetColor(colorProp2, Color.Lerp(white, black, t));
                    }
                    if (timeSinceHexStateChanged > 8f) {
                        PopCubesOut();
                    }
                    break;
                case HexState.CorrectValue:
                    innerHexRotate.enabled = false;
                    outerHexRotate.enabled = false;
                    TrackPosition(trackingSlerpSpeed, laserReceiver.transform.position - laserReceiver.transform.up * 0.5f);
                    if (timeSinceHexStateChanged > 2.5f) {
                        laserToReceiverStart.Play();
                        laserToReceiver.Play();
					}

                    if (timeSinceHexStateChanged > 4f) {
                        powerTrailToPortal.powerIsOn = true;
					}
                    break;
                default:
                    break;
            }

            timeSinceHexStateChanged += Time.deltaTime;
        }

        void PopCubesOut() {
            foreach (var powerReceptacle in powerReceptacles) {
                PickupObject cubeToEject = powerReceptacle.cubeReceptacle.cubeInReceptacle;
                if (cubeToEject == null) {
                    continue;
                }
                powerReceptacle.cubeReceptacle.ReleaseCubeFromReceptacleInstantly();
                Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;
                float forceMagnitude = UnityEngine.Random.Range(240f, 350f);
                Vector3 ejectionDirection = powerReceptacle.cubeReceptacle.transform.TransformDirection(new Vector3(-Mathf.Abs(randomDirection.x), 4, randomDirection.y));
                cubeToEject.thisRigidbody.AddForce(ejectionDirection * forceMagnitude, ForceMode.Impulse);
            }
        }

        void TrackPosition(float trackSpeed, Vector3 position) {
            Vector3 localPos = transform.InverseTransformDirection(position - outerHex.position);
            desiredOuterHexRotation = new Vector3(0, Mathf.Rad2Deg * Mathf.Atan2(-localPos.z, localPos.x), 0);
            outerHex.localRotation = Quaternion.Slerp(outerHex.localRotation, Quaternion.Euler(desiredOuterHexRotation), trackSpeed * Time.deltaTime);

            localPos = outerHex.InverseTransformDirection(position - innerHex.position);
            desiredInnerHexRotation = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(localPos.y, localPos.x));
            innerHex.localRotation = Quaternion.Slerp(innerHex.localRotation, Quaternion.Euler(desiredInnerHexRotation), trackSpeed * Time.deltaTime);
        }

        void UpdateParticleSystems() {
            if (solved) {
                ParticleSystem.MainModule outerParticlesMain = outerParticles.main;
                outerParticlesMain.startColor = new ParticleSystem.MinMaxGradient(Color.green * 0.6f, Color.green);

                ParticleSystem.MainModule innerParticlesMain = innerParticles.main;
                innerParticlesMain.startColor = new ParticleSystem.MinMaxGradient(Color.green * 0.6f, Color.green);
            }
            else {
                float t = Mathf.InverseLerp(-80f, 80f, displayedValue);
                ParticleSystem.MainModule outerParticlesMain = outerParticles.main;
                outerParticlesMain.startColor = new ParticleSystem.MinMaxGradient(lowerBoundColors.Evaluate(t), upperBoundColors.Evaluate(t));

                ParticleSystem.MainModule innerParticlesMain = innerParticles.main;
                innerParticlesMain.startColor = new ParticleSystem.MinMaxGradient(lowerBoundColors.Evaluate(t), upperBoundColors.Evaluate(t));
            }
        }

        #region ObeliskLight
        void UpdateObeliskLight(float t) {
            SetObeliskLightColorGradients();
            SetObeliskLightEmissionColorGradients();

            obeliskLightMaterial.SetFloat("_Power", t);
		}
        
        /// <summary>
        /// Sets the _ColorGradientKeyTimes and _ColorGradient float and Color arrays, respectively
        /// Populates _ColorGradientKeyTimes with the times of each colorKey in lowerBoundColors (as well as a 0 as the first key and a series of 1s to fill out the array at the end)
        /// Populates _ColorGradient with the colors of each colorKey in lowerBoundColors (as well as values for the times filled in as described above)
        /// </summary>
        void SetObeliskLightColorGradients() {
            Color startColor = lowerBoundColors.Evaluate(0);
            Color endColor = lowerBoundColors.Evaluate(1);
            float startAlpha = startColor.a;
            float endAlpha = endColor.a;

            obeliskLightMaterial.SetFloatArray("_ColorGradientKeyTimes", GetGradientFloatValues(0f, lowerBoundColors.colorKeys.Select(x => x.time), 1f));
            obeliskLightMaterial.SetColorArray("_ColorGradient", GetGradientColorValues(startColor, lowerBoundColors.colorKeys.Select(x => x.color), endColor));
            obeliskLightMaterial.SetFloatArray("_AlphaGradientKeyTimes", GetGradientFloatValues(0f, lowerBoundColors.alphaKeys.Select(x => x.time), 1f));
            obeliskLightMaterial.SetFloatArray("_AlphaGradient", GetGradientFloatValues(startAlpha, lowerBoundColors.alphaKeys.Select(x => x.alpha), endAlpha));

            obeliskLightMaterial.SetInt("_ColorGradientMode", lowerBoundColors.mode == GradientMode.Blend ? 0 : 1);
        }

        /// <summary>
        /// Sets the _ColorGradientKeyTimes and _ColorGradient float and Color arrays, respectively
        /// Populates _ColorGradientKeyTimes with the times of each colorKey in lowerBoundColors (as well as a 0 as the first key and a series of 1s to fill out the array at the end)
        /// Populates _ColorGradient with the colors of each colorKey in lowerBoundColors (as well as values for the times filled in as described above)
        /// </summary>
        void SetObeliskLightEmissionColorGradients() {
            Color startColor = upperBoundColors.Evaluate(0);
            Color endColor = upperBoundColors.Evaluate(1);
            float startAlpha = startColor.a;
            float endAlpha = endColor.a;

            obeliskLightMaterial.SetFloatArray("_EmissionColorGradientKeyTimes", GetGradientFloatValues(0f, upperBoundColors.colorKeys.Select(x => x.time), 1f));
            obeliskLightMaterial.SetColorArray("_EmissionColorGradient", GetGradientColorValues(startColor, upperBoundColors.colorKeys.Select(x => x.color), endColor));
            obeliskLightMaterial.SetFloatArray("_EmissionAlphaGradientKeyTimes", GetGradientFloatValues(0f, upperBoundColors.alphaKeys.Select(x => x.time), 1f));
            obeliskLightMaterial.SetFloatArray("_EmissionAlphaGradient", GetGradientFloatValues(startAlpha, upperBoundColors.alphaKeys.Select(x => x.alpha), endAlpha));

            obeliskLightMaterial.SetInt("_EmissionColorGradientMode", upperBoundColors.mode == GradientMode.Blend ? 0 : 1);
        }

        // Actually just populates the float buffer with the values provided, then returns a reference to the float buffer
        float[] GetGradientFloatValues(float startValue, IEnumerable<float> middleValues, float endValue) {
            float[] middleValuesArray = middleValues.ToArray();
            floatGradientBuffer[0] = startValue;
            for (int i = 1; i < middleValuesArray.Length + 1; i++) {
                floatGradientBuffer[i] = middleValuesArray[i - 1];
            }
            for (int j = middleValuesArray.Length + 1; j < GRADIENT_ARRAY_SIZE; j++) {
                floatGradientBuffer[j] = endValue;
            }
            return floatGradientBuffer;
        }

        // Actually just populates the color buffer with the values provided, then returns a reference to the color buffer
        Color[] GetGradientColorValues(Color startValue, IEnumerable<Color> middleValues, Color endValue) {
            Color[] middleValuesArray = middleValues.ToArray();
            colorGradientBuffer[0] = startValue;
            for (int i = 1; i < middleValuesArray.Length + 1; i++) {
                colorGradientBuffer[i] = middleValuesArray[i - 1];
            }
            for (int j = middleValuesArray.Length + 1; j < GRADIENT_ARRAY_SIZE; j++) {
                colorGradientBuffer[j] = endValue;
            }
            return colorGradientBuffer;
        }
		#endregion

		#region ValueLine
        void UpdateValueLine(float t) {
            valueLine.localPosition = Vector3.Lerp(valueLIneBot, valueLineTop, t);
            valueLine.localScale = Vector3.Lerp(valueLineScaleMax, valueLineScaleMin, t);
		}
		#endregion
        
#region Saving
        public override string ID => "WhiteRoomPuzzle2";

        [Serializable]
        public class WhiteRoomPuzzle2Save : SerializableSaveObject<WhiteRoomPuzzle2> {
            int actualValue;
            float displayedValue;
            HexState hexState;
            float timeSinceHexStateChanged;
            float[] floatGradientBuffer;
            SerializableColor[] colorGradientBuffer;
            
            public WhiteRoomPuzzle2Save(WhiteRoomPuzzle2 script) {
                this.actualValue = script.actualValue;
                this.displayedValue = script.displayedValue;
                this.hexState = script.hexState;
                this.timeSinceHexStateChanged = script.timeSinceHexStateChanged;
                this.floatGradientBuffer = new float[GRADIENT_ARRAY_SIZE];
                this.colorGradientBuffer = new SerializableColor[GRADIENT_ARRAY_SIZE];
                for (int i = 0; i < GRADIENT_ARRAY_SIZE; i++) {
                    this.floatGradientBuffer[i] = script.floatGradientBuffer[i];
                    this.colorGradientBuffer[i] = script.colorGradientBuffer[i];
                }
            }

            public override void LoadSave(WhiteRoomPuzzle2 script) {
                script.actualValue = this.actualValue;
                script.displayedValue = this.displayedValue;
                script._hexState = this.hexState;
                script.timeSinceHexStateChanged = this.timeSinceHexStateChanged;
                for (int i = 0; i < GRADIENT_ARRAY_SIZE; i++) {
                    script.floatGradientBuffer[i] = this.floatGradientBuffer[i];
                    script.colorGradientBuffer[i] = this.colorGradientBuffer[i];
                }
            }
        }
#endregion
	}
}
