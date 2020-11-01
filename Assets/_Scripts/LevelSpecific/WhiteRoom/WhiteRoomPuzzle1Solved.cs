using Saving;
using SerializableClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
	public class WhiteRoomPuzzle1Solved : MonoBehaviour, SaveableObject {
		public CubeReceptacle receptacle;
		public GameObject fakePortal;
		public GameObject fakePortalPillarLeft, fakePortalPillarRight;

		Vector3 startPos;
		Vector3 endPos;
		Vector3 targetPos;
		float moveSpeed;

		const float moveSpeedUp = 4;
		const float moveSpeedDown = 10;

		void Awake() {
			moveSpeed = moveSpeedUp;

			receptacle = GetComponent<CubeReceptacle>();

			startPos = fakePortal.transform.position;
			endPos = fakePortal.transform.TransformPoint(Vector3.up * 10);
			targetPos = startPos;
		}

		void Start() {
			receptacle.OnCubeHoldEndSimple += OnCubePlaced;
			receptacle.OnCubeReleaseStartSimple += OnCubeRemoved;
		}

		void Update() {
			if (fakePortal.activeSelf) {
				Vector3 oldFakePortalPos = fakePortal.transform.position;
				fakePortal.transform.position = Vector3.Lerp(fakePortal.transform.position, targetPos, Time.deltaTime * moveSpeed);
				fakePortalPillarLeft.transform.position = new Vector3(fakePortalPillarLeft.transform.position.x, fakePortal.transform.position.y, fakePortalPillarLeft.transform.position.z);
				fakePortalPillarRight.transform.position = new Vector3(fakePortalPillarRight.transform.position.x, fakePortal.transform.position.y, fakePortalPillarRight.transform.position.z);

				if (Vector3.Distance(fakePortal.transform.position, startPos) < 0.1f) {
					ResetFakePortal();
				}
			}

		}

		void OnCubePlaced() {
			fakePortal.SetActive(true);
			fakePortalPillarLeft.SetActive(true);
			fakePortalPillarRight.SetActive(true);
			targetPos = endPos;
			moveSpeed = moveSpeedUp;
		}

		void OnCubeRemoved() {
			targetPos = startPos;
			moveSpeed = moveSpeedDown;
		}

		void ResetFakePortal() {
			fakePortal.transform.position = startPos;
			fakePortal.SetActive(false);
			fakePortalPillarLeft.SetActive(false);
			fakePortalPillarRight.SetActive(false);
		}

		#region Saving
		public bool SkipSave { get; set; }

		public string ID => "WhiteRoomPuzzle1Solved";

		[Serializable]
		class WhiteRoomPuzzle1SolvedSave {
			SerializableVector3 fakePortalPos, fakePortalPillarLeftPos, fakePortalPillarRightPos;
			bool fakePortalActive;

			SerializableVector3 startPos, endPos, targetPos;
			float movespeed;

			public WhiteRoomPuzzle1SolvedSave(WhiteRoomPuzzle1Solved script) {
				this.fakePortalPos = script.fakePortal.transform.position;
				this.fakePortalPillarLeftPos = script.fakePortalPillarLeft.transform.position;
				this.fakePortalPillarRightPos = script.fakePortalPillarRight.transform.position;
				this.fakePortalActive = script.fakePortal.activeSelf;

				this.startPos = script.startPos;
				this.endPos = script.endPos;
				this.targetPos = script.targetPos;
				this.movespeed = script.moveSpeed;
			}

			public void LoadSave(WhiteRoomPuzzle1Solved script) {
				script.fakePortal.transform.position = this.fakePortalPos;
				script.fakePortalPillarLeft.transform.position = this.fakePortalPillarLeftPos;
				script.fakePortalPillarRight.transform.position = this.fakePortalPillarRightPos;

				script.fakePortal.SetActive(this.fakePortalActive);
				script.fakePortalPillarLeft.SetActive(this.fakePortalActive);
				script.fakePortalPillarRight.SetActive(this.fakePortalActive);

				script.startPos = this.startPos;
				script.endPos = this.endPos;
				script.targetPos = this.targetPos;
				script.moveSpeed = this.movespeed;
			}
		}

		public object GetSaveObject() {
			return new WhiteRoomPuzzle1SolvedSave(this);
		}

		public void LoadFromSavedObject(object savedObject) {
			WhiteRoomPuzzle1SolvedSave save = savedObject as WhiteRoomPuzzle1SolvedSave;

			save.LoadSave(this);
		}
		#endregion
	}
}