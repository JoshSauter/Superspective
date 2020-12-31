using MagicTriggerMechanics;
using PowerTrailMechanics;
using Saving;
using SerializableClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiteRoomPuzzle1 : MonoBehaviour, SaveableObject {
    public PowerTrail powerTrail;
    public MagicTrigger fakePortalTrigger;

	// Fake portal movement
    public GameObject fakePortal;
	Vector3 fakePortalUnsolvedPos;
	Vector3 fakePortalSolvedPos;
	Vector3 fakePortalTargetPos;
	float fakePortalLerpSpeed;
	const float fakePortalLerpSpeedUp = 4;
	const float fakePortalLerpSpeedDown = 10;

	// Hole cover needs to disappear when unsolved and in another dimension
	public GameObject holeCover;
	public PillarDimensionObject dimension0;

	// Fake portal plane needs to temporarily disappear if the player walks backwards through it
	public GameObject fakePortalPlane;
	public GameObject restoreFakePortalPlaneTrigger;

	// We trade out the ToCathedral DimensionObject for ToCathedral PillarDimensionObject after player walks through fake portal
	public GameObject toCathedral;
	public GameObject toCathedralInPillarDimension0;

	public enum State {
        Unsolved,
        FakePortalPowered,
        WalkedThroughFakePortal
	}
    private State _state;
    public State state {
        get { return _state; }
        set {
            if (_state == value) {
                return;
			}

			switch (value) {
				case State.Unsolved:
					fakePortalTargetPos = fakePortalUnsolvedPos;
					fakePortalLerpSpeed = fakePortalLerpSpeedDown;
					break;
				case State.FakePortalPowered:
					fakePortalTargetPos = fakePortalSolvedPos;
					fakePortalLerpSpeed = fakePortalLerpSpeedUp;
					break;
				case State.WalkedThroughFakePortal:
					break;
				default:
					break;
			}

			_state = value;
		}
	}

	private void Awake() {
		fakePortalUnsolvedPos = fakePortal.transform.TransformPoint(Vector3.down * 10);
		fakePortalSolvedPos = fakePortal.transform.position;

		fakePortal.transform.position = fakePortalUnsolvedPos;
	}

	private void Start() {
		state = State.Unsolved;

		fakePortalTrigger.OnMagicTriggerStayOneTime += (ctx) => {
			if (state == State.FakePortalPowered) {
				state = State.WalkedThroughFakePortal;
			}
		};
		fakePortalTrigger.OnNegativeMagicTriggerStayOneTime += (ctx) => {
			if (state == State.WalkedThroughFakePortal) {
				state = State.FakePortalPowered;
			}
			// Player walks backwards through wrong side of fake portal, hide the illusion temporarily
			else if (state == State.FakePortalPowered) {
				fakePortalPlane.SetActive(false);
				restoreFakePortalPlaneTrigger.SetActive(true);
			}
		};
	}

	private void Update() {
		fakePortalTargetPos = powerTrail.state == PowerTrail.PowerTrailState.powered ? fakePortalSolvedPos : fakePortalUnsolvedPos;
		fakePortal.SetActive(!(state == State.Unsolved && Vector3.Distance(fakePortal.transform.position, fakePortalUnsolvedPos) < 0.1f));
		fakePortal.transform.position = Vector3.Lerp(fakePortal.transform.position, fakePortalTargetPos, Time.deltaTime * fakePortalLerpSpeed);

		holeCover.SetActive(state != State.WalkedThroughFakePortal && dimension0.visibilityState == VisibilityState.visible);

		toCathedral.SetActive(state != State.WalkedThroughFakePortal && dimension0.visibilityState == VisibilityState.visible);
		toCathedralInPillarDimension0.SetActive(state == State.WalkedThroughFakePortal);

		switch (state) {
			case State.Unsolved:
				if (powerTrail.state == PowerTrail.PowerTrailState.powered) {
					state = State.FakePortalPowered;
				}
				break;
			case State.FakePortalPowered:
				if (powerTrail.state != PowerTrail.PowerTrailState.powered) {
					state = State.Unsolved;
				}
				break;
			case State.WalkedThroughFakePortal:
				break;
			default:
				break;
		}
	}

	#region Saving
	public bool SkipSave { get; set; }

	public string ID => "WhiteRoomPuzzle1";

	[Serializable]
	class WhiteRoomPuzzle1Save {
		int state;
		SerializableVector3 fakePortalPos;
		bool fakePortalActive;
		bool fakePortalPlaneActive;
		bool restoreFakePortalPlaneTriggerActive;
		bool holeCoverActive;
		bool toCathedralActive;
		bool toCathedralInPillarDimension0Active;

		float fakePortalLerpSpeed;

		public WhiteRoomPuzzle1Save(WhiteRoomPuzzle1 script) {
			this.state = (int)script.state;
			this.fakePortalPos = script.fakePortal.transform.position;
			this.fakePortalActive = script.fakePortal.activeSelf;
			this.fakePortalPlaneActive = script.fakePortalPlane.activeSelf;
			this.restoreFakePortalPlaneTriggerActive = script.restoreFakePortalPlaneTrigger.activeSelf;
			this.holeCoverActive = script.holeCover.activeSelf;
			this.toCathedralActive = script.toCathedral.activeSelf;
			this.toCathedralInPillarDimension0Active = script.toCathedralInPillarDimension0.activeSelf;
			this.fakePortalLerpSpeed = script.fakePortalLerpSpeed;
		}

		public void LoadSave(WhiteRoomPuzzle1 script) {
			script.state = (State)this.state;
			script.fakePortal.transform.position = this.fakePortalPos;
			script.fakePortal.SetActive(this.fakePortalActive);
			script.fakePortalPlane.SetActive(this.fakePortalPlaneActive);
			script.restoreFakePortalPlaneTrigger.SetActive(this.restoreFakePortalPlaneTriggerActive);
			script.holeCover.SetActive(this.holeCoverActive);
			script.toCathedral.SetActive(this.toCathedralActive);
			script.toCathedralInPillarDimension0.SetActive(this.toCathedralInPillarDimension0Active);
			script.fakePortalLerpSpeed = this.fakePortalLerpSpeed;
		}
	}

	public object GetSaveObject() {
		return new WhiteRoomPuzzle1Save(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		WhiteRoomPuzzle1Save save = savedObject as WhiteRoomPuzzle1Save;

		save.LoadSave(this);
	}
	#endregion
}
