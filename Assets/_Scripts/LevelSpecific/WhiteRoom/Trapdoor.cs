﻿using System;
using System.Linq;
using SuperspectiveUtils;
using PowerTrailMechanics;
using Saving;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
    [RequireComponent(typeof(UniqueId))]
    public class Trapdoor : SuperspectiveObject<Trapdoor, Trapdoor.TrapdoorSave> {
        public PowerTrail powerTrailToReactTo;
        
        public enum State {
            Closed,
            Opening,
            Open,
            Closing
        }

        State _state;

        State state {
            get => _state;
            set {
                if (_state == value) {
                    return;
                }

                _state = value;
                timeSinceStateChanged = 0f;
            }
        }
        float timeSinceStateChanged = 0f;
        const float openCloseTime = 1.25f;

        readonly Vector3 closedScale = Vector3.one;
        readonly Vector3 openScale = new Vector3(0.15f, 0.15f, 1f);

        Transform[] trapdoorPieces;

        protected override void Awake() {
            base.Awake();
            trapdoorPieces = transform.GetChildren().Take(2).ToArray();
        }

        void Update() {
            if (GameManager.instance.IsCurrentlyLoading) return;
            
            UpdateState();
            
            timeSinceStateChanged += Time.deltaTime;
            float t = timeSinceStateChanged / openCloseTime;
            
            switch (state) {
                case State.Closed:
                    foreach (var piece in trapdoorPieces) {
                        piece.localScale = closedScale;
                    }
                    break;
                case State.Opening:
                    foreach (var piece in trapdoorPieces) {
                        piece.localScale = Vector3.Lerp(closedScale, openScale, Easing.EaseInOut(t));
                    }
                    break;
                case State.Open:
                    foreach (var piece in trapdoorPieces) {
                        piece.localScale = openScale;
                    }
                    break;
                case State.Closing:
                    foreach (var piece in trapdoorPieces) {
                        piece.localScale = Vector3.Lerp(openScale, closedScale, Easing.EaseInOut(t));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void UpdateState() {
            switch (state) {
                case State.Closed:
                    if (powerTrailToReactTo.IsFullyPowered || this.InstaSolvePuzzle()) {
                        state = State.Opening;
                    }
                    break;
                case State.Opening:
                    if (!powerTrailToReactTo.IsFullyPowered) {
                        float cachedTimeSinceStateChange = timeSinceStateChanged;
                        state = State.Closing;
                        // Don't start from 0 if opening or closing was interrupted
                        timeSinceStateChanged = 1 - cachedTimeSinceStateChange;
                    }
                    else if (timeSinceStateChanged > openCloseTime) {
                        state = State.Open;
                    }
                    break;
                case State.Open:
                    if (!powerTrailToReactTo.IsFullyPowered) {
                        state = State.Closing;
                    }
                    break;
                case State.Closing:
                    if (powerTrailToReactTo.IsFullyPowered) {
                        float cachedTimeSinceStateChange = timeSinceStateChanged;
                        state = State.Opening;
                        // Don't start from 0 if opening or closing was interrupted
                        timeSinceStateChanged = 1 - cachedTimeSinceStateChange;
                    }
                    else if (timeSinceStateChanged > openCloseTime) {
                        state = State.Closed;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public override void LoadSave(TrapdoorSave save) { }
        
        [Serializable]
        public class TrapdoorSave : SaveObject<Trapdoor> {
            public TrapdoorSave(Trapdoor saveableObject) : base(saveableObject) { }
        }
    }
}