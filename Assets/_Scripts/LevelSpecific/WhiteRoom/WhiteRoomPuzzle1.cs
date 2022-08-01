using MagicTriggerMechanics;
using PowerTrailMechanics;
using Saving;
using SerializableClasses;
using System;
using System.Collections;
using SuperspectiveUtils;
using Library.Functional;
using UnityEngine;
using DimensionObjectReference = SerializableClasses.SerializableReference<DimensionObject, DimensionObject.DimensionObjectSave>;
using MaybeDimensionObject = Library.Functional.Either<DimensionObject, DimensionObject.DimensionObjectSave>;

public class WhiteRoomPuzzle1 : SaveableObject<WhiteRoomPuzzle1, WhiteRoomPuzzle1.WhiteRoomPuzzle1Save> {
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

	public PillarDimensionObject dimension0;

	// Fake portal plane needs to temporarily disappear if the player walks backwards through it
	public GameObject fakePortalPlane;
	public GameObject restoreFakePortalPlaneTrigger;

	// We trade out the ToCathedral DimensionObject for ToCathedral PillarDimensionObject after player walks through fake portal
	// These are SerializableReferences because they come from another scene (WhiteRoomBackRoom)
	public DimensionObjectReference archToNextRoomReference;

	MaybeDimensionObject archToNextRoom => gameObject.IsInActiveScene() ? archToNextRoomReference.Reference : null;

	public DimensionObjectReference holeCoverReference;
	MaybeDimensionObject holeCover => gameObject.IsInActiveScene() ? holeCoverReference.Reference : null;
	
	public enum State {
        Unsolved,
        FakePortalPowered,
        WalkedThroughFakePortal
	}

	State _state;

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
					archToNextRoom?.MatchAction(
						dimensionObject => dimensionObject.SwitchVisibilityState(VisibilityState.invisible, true),
						saveObject => saveObject.visibilityState = (int) VisibilityState.invisible
					);
					holeCover?.MatchAction(
						dimensionObject => dimensionObject.SwitchVisibilityState(VisibilityState.invisible, true),
						saveObject => saveObject.visibilityState = (int) VisibilityState.invisible
					);
					break;
				case State.FakePortalPowered:
					fakePortalTargetPos = fakePortalSolvedPos;
					fakePortalLerpSpeed = fakePortalLerpSpeedUp;
					archToNextRoom?.MatchAction(
						dimensionObject => dimensionObject.SwitchVisibilityState(VisibilityState.partiallyVisible, true),
						saveObject => saveObject.visibilityState = (int) VisibilityState.partiallyVisible
					);
					holeCover?.MatchAction(
						dimensionObject => dimensionObject.SwitchVisibilityState(VisibilityState.partiallyVisible, true),
						saveObject => saveObject.visibilityState = (int) VisibilityState.partiallyVisible
					);
					break;
				case State.WalkedThroughFakePortal:
					archToNextRoom?.MatchAction(
						dimensionObject => dimensionObject.SwitchVisibilityState(VisibilityState.visible, true),
						saveObject => saveObject.visibilityState = (int) VisibilityState.visible
					);
					holeCover?.MatchAction(
						dimensionObject => dimensionObject.SwitchVisibilityState(VisibilityState.visible, true),
						saveObject => saveObject.visibilityState = (int) VisibilityState.visible
					);
					break;
				default:
					break;
			}

			_state = value;
		}
	}

	protected override void Awake() {
		base.Awake();
		fakePortalUnsolvedPos = fakePortal.transform.TransformPoint(Vector3.down * 10);
		fakePortalSolvedPos = fakePortal.transform.position;

		fakePortal.transform.position = fakePortalUnsolvedPos;
	}

	protected override void Start() {
		base.Start();
		StartCoroutine(Initialize());
	}

	IEnumerator Initialize() {
		yield return new WaitUntil(() => gameObject.IsInActiveScene());
		state = State.Unsolved;

		fakePortalTrigger.OnMagicTriggerStayOneTime += () => {
			if (state == State.FakePortalPowered) {
				state = State.WalkedThroughFakePortal;
			}
		};
		fakePortalTrigger.OnNegativeMagicTriggerStayOneTime += () => {
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

    void Update() {
		fakePortalTargetPos = powerTrail.state == PowerTrailState.Powered ? fakePortalSolvedPos : fakePortalUnsolvedPos;
		bool fakePortalActive = !(state == State.Unsolved && Vector3.Distance(fakePortal.transform.position, fakePortalUnsolvedPos) < 0.1f);
		if (fakePortal.activeSelf != fakePortalActive) {
			fakePortal.SetActive(fakePortalActive);
		}

		fakePortal.transform.position = Vector3.Lerp(fakePortal.transform.position, fakePortalTargetPos, Time.deltaTime * fakePortalLerpSpeed);

		switch (state) {
			case State.Unsolved:
				if (powerTrail.state == PowerTrailState.Powered || this.InstaSolvePuzzle()) {
					state = State.FakePortalPowered;
				}
				break;
			case State.FakePortalPowered:
				if (powerTrail.state != PowerTrailState.Powered) {
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
	public override string ID => "WhiteRoomPuzzle1";

	[Serializable]
	public class WhiteRoomPuzzle1Save : SerializableSaveObject<WhiteRoomPuzzle1> {
		int state;
		SerializableVector3 fakePortalPos;
		bool fakePortalActive;
		bool fakePortalPlaneActive;
		bool restoreFakePortalPlaneTriggerActive;

		float fakePortalLerpSpeed;

		public WhiteRoomPuzzle1Save(WhiteRoomPuzzle1 script) : base(script) {
			this.state = (int)script.state;
			this.fakePortalPos = script.fakePortal.transform.position;
			this.fakePortalActive = script.fakePortal.activeSelf;
			this.fakePortalPlaneActive = script.fakePortalPlane.activeSelf;
			this.restoreFakePortalPlaneTriggerActive = script.restoreFakePortalPlaneTrigger.activeSelf;
			this.fakePortalLerpSpeed = script.fakePortalLerpSpeed;
		}

		public override void LoadSave(WhiteRoomPuzzle1 script) {
			script.state = (State)this.state;
			script.fakePortal.transform.position = this.fakePortalPos;
			script.fakePortal.SetActive(this.fakePortalActive);
			script.fakePortalPlane.SetActive(this.fakePortalPlaneActive);
			script.restoreFakePortalPlaneTrigger.SetActive(this.restoreFakePortalPlaneTriggerActive);
			script.fakePortalLerpSpeed = this.fakePortalLerpSpeed;
		}
	}
	#endregion
}
