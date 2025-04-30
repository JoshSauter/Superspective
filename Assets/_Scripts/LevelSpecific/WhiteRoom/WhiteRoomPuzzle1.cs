using PowerTrailMechanics;
using Saving;
using SerializableClasses;
using System;
using System.Collections;
using DissolveObjects;
using PortalMechanics;
using SuperspectiveUtils;
using UnityEngine;

public class WhiteRoomPuzzle1 : SuperspectiveObject<WhiteRoomPuzzle1, WhiteRoomPuzzle1.WhiteRoomPuzzle1Save> {
    public PowerTrail powerTrail;
    public RevealerPortal revealerPortal;

	// Fake portal movement
    public GameObject fakePortal;
	Vector3 fakePortalUnsolvedPos;
	Vector3 fakePortalSolvedPos;
	Vector3 fakePortalTargetPos;
	float fakePortalLerpSpeed;
	const float FAKE_PORTAL_LERP_SPEED_UP = 4;
	const float FAKE_PORTAL_LERP_SPEED_DOWN = 10;

	public PillarDimensionObject dimension1;

	// Not sure if I like this idea yet, easy way to turn it off or on for playtesting
	public bool hideButtonPedestalAtFirst = true;
	public GameObject buttonPedestal;
	public DissolveObject dissolveBridge;
	private bool ShouldRevealButtonPedestal => dissolveBridge.stateMachine != DissolveObject.State.Dematerialized && dimension1.visibilityState == VisibilityState.Visible;
	
	public enum State : byte {
        Unsolved,
        FakePortalPowered,
        WalkedThroughFakePortal
	}

	State _state;

	public State state {
        get => _state;
        set {
            if (_state == value) {
                return;
			}
            
			switch (value) {
				case State.Unsolved:
					fakePortalTargetPos = fakePortalUnsolvedPos;
					fakePortalLerpSpeed = FAKE_PORTAL_LERP_SPEED_DOWN;
					break;
				case State.FakePortalPowered:
					fakePortalTargetPos = fakePortalSolvedPos;
					fakePortalLerpSpeed = FAKE_PORTAL_LERP_SPEED_UP;
					break;
				case State.WalkedThroughFakePortal:
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
		yield return new WaitWhile(() => !GameManager.instance.gameHasLoaded);
		yield return new WaitUntil(() => gameObject.IsInActiveScene());
		state = State.Unsolved;

		revealerPortal.revealState.OnStateChangeSimple += () => {
			if (revealerPortal.revealState == RevealerPortal.RevealState.Visible) {
				state = State.WalkedThroughFakePortal;
			}
			else if (state == State.WalkedThroughFakePortal) {
				state = State.FakePortalPowered;
			}
		};

		yield return null;
		
		if (hideButtonPedestalAtFirst) {
			SetButtonPedestalActive(false);
		}
	}

	private Renderer[] _buttonPedestalRenderers;
	private Renderer[] ButtonPedestalRenderers => _buttonPedestalRenderers ??= buttonPedestal.GetComponentsInChildrenRecursively<Renderer>();
	private Collider[] _buttonPedestalColliders;
	private Collider[] ButtonPedestalColliders => _buttonPedestalColliders ??= buttonPedestal.GetComponentsInChildrenRecursively<Collider>();
	private bool buttonPedestalEnabled = false;
	private void SetButtonPedestalActive(bool active) {
		foreach (Renderer buttonPedestalRenderer in ButtonPedestalRenderers) {
			buttonPedestalRenderer.enabled = active;
		}

		foreach (Collider buttonPedestalCollider in ButtonPedestalColliders) {
			buttonPedestalCollider.enabled = active;
		}

		buttonPedestalEnabled = active;
	}

    void Update() {
	    if (GameManager.instance.IsCurrentlyLoading) return;
	    
		fakePortalTargetPos = powerTrail.pwr.FullyPowered ? fakePortalSolvedPos : fakePortalUnsolvedPos;
		bool fakePortalActive = !(state == State.Unsolved && Vector3.Distance(fakePortal.transform.position, fakePortalUnsolvedPos) < 0.1f);
		if (fakePortal.activeSelf != fakePortalActive) {
			fakePortal.SetActive(fakePortalActive);
		}

		fakePortal.transform.position = Vector3.Lerp(fakePortal.transform.position, fakePortalTargetPos, Time.deltaTime * fakePortalLerpSpeed);
		if (hideButtonPedestalAtFirst) {
			if (!buttonPedestalEnabled && ShouldRevealButtonPedestal) {
				SetButtonPedestalActive(true);
			}
		}

		switch (state) {
			case State.Unsolved:
				if (powerTrail.pwr.FullyPowered || this.InstaSolvePuzzle()) {
					state = State.FakePortalPowered;
				}
				break;
			case State.FakePortalPowered:
				if (!powerTrail.pwr.FullyPowered) {
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

	public override void LoadSave(WhiteRoomPuzzle1Save save) {
		fakePortal.transform.position = save.fakePortalPos;
		fakePortal.SetActive(save.fakePortalActive);
	}

	public override string ID => "WhiteRoomPuzzle1";

	[Serializable]
	public class WhiteRoomPuzzle1Save : SaveObject<WhiteRoomPuzzle1> {
		public SerializableVector3 fakePortalPos;
		public bool fakePortalActive;

		public WhiteRoomPuzzle1Save(WhiteRoomPuzzle1 script) : base(script) {
			this.fakePortalPos = script.fakePortal.transform.position;
			this.fakePortalActive = script.fakePortal.activeSelf;
		}
	}
#endregion
}
