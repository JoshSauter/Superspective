using System.Collections;
using System.Runtime.Serialization;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OldPowerTrail {
	[ExecuteInEditMode]
	[RequireComponent(typeof(NodeSystem))]
	public class OldPowerTrail : MonoBehaviour {
		public bool DEBUG = false;
		public DebugLogger debug;
		public GameObject powerTrailMaskPrefab;

		#region events
		public delegate void PowerTrailAction();
		public event PowerTrailAction OnPowerBegin;
		public event PowerTrailAction OnPowerFinish;
		public event PowerTrailAction OnDepowerBegin;
		public event PowerTrailAction OnDepowerFinish;
		#endregion

		public enum PowerTrailState {
			depowered,
			partiallyPowered,
			powered
		}
		public NodeSystem powerNodes;
		public float speed = 1f;
		float powerTrailSize = 0.15f;

		[System.Serializable]
		public class NodeTrailInfo {
			public Node parent;
			public Node child;
			public GameObject maskGO;
			public float startDistance;
			public float endDistance;
		}
		public List<NodeTrailInfo> trailInfo = new List<NodeTrailInfo>();

		///////////
		// State //
		///////////
		public float distance = 0f;
		public float maxDistance = 0f;
		public bool powerIsOn = false;
		[SerializeField]
		private PowerTrailState _state = PowerTrailState.depowered;
		public PowerTrailState state {
			get { return _state; }
			set {
				if (_state == PowerTrailState.depowered && value == PowerTrailState.partiallyPowered) {
					if (OnPowerBegin != null) {
						OnPowerBegin();
					}
				}
				else if (_state == PowerTrailState.partiallyPowered && value == PowerTrailState.powered) {
					if (OnPowerFinish != null) {
						OnPowerFinish();
					}
				}
				else if (_state == PowerTrailState.powered && value == PowerTrailState.partiallyPowered) {
					if (OnDepowerBegin != null) {
						OnDepowerBegin();
					}
				}
				else if (_state == PowerTrailState.partiallyPowered && value == PowerTrailState.depowered) {
					if (OnDepowerFinish != null) {
						OnDepowerFinish();
					}
				}
				_state = value;
			}
		}

		private void OnEnable() {
			if (powerNodes == null) {
				powerNodes = GetComponent<NodeSystem>();
			}

		}

		private void Start() {
			PopulateTrailInfo();
			debug = new DebugLogger(this, () => DEBUG);
		}

		void PopulateTrailInfo() {
			trailInfo.Clear();
			foreach (int childId in powerNodes.parentNode.childrenIds) {
				Node child = powerNodes.GetNode(childId);
				if (child != null) {
					PopulateTrailInfoRecursively(powerNodes.parentNode, child, 0);
				}
			}
		}

		void PopulateTrailInfoRecursively(Node parent, Node child, float distance) {
			NodeTrailInfo info = new NodeTrailInfo {
				parent = parent,
				child = child,
				startDistance = distance,
				endDistance = distance + (child.pos - parent.pos).magnitude
			};
			trailInfo.Add(info);

			if (info.endDistance > maxDistance) {
				maxDistance = info.endDistance;
			}

			foreach (int grandChildId in child.childrenIds) {
				Node grandchild = powerNodes.GetNode(grandChildId);
				if (grandchild != null) {
					PopulateTrailInfoRecursively(child, grandchild, info.endDistance);
				}
			}
		}

		void Update() {
			if (Input.GetKeyDown("v")) {
				powerIsOn = !powerIsOn;
			}

			float prevDistance = distance;
			float nextDistance = NextDistance();
			if (nextDistance == prevDistance) return;

			foreach (var info in trailInfo) {
				UpdatePowerTrail(info, prevDistance, nextDistance);
			}

			UpdateState(prevDistance, nextDistance);
			distance = nextDistance;
		}

		void UpdatePowerTrail(NodeTrailInfo info, float prevDistance, float nextDistance) {
			// Mask GameObject creation
			if (prevDistance <= info.startDistance && nextDistance > info.startDistance) {
				debug.Log("Powering node: " + info.parent.id + " <-> " + info.child.id);
				info.maskGO = Instantiate(powerTrailMaskPrefab);
				Transform power = info.maskGO.transform;
				power.SetParent(transform);
				power.position = transform.TransformPoint(info.parent.pos);
				power.localScale = new Vector3(powerTrailSize, powerTrailSize, 0);
				power.LookAt(transform.TransformPoint(info.child.pos));
			}
			// Mask GameObject destruction
			else if (prevDistance > info.startDistance && nextDistance <= info.startDistance) {
				Destroy(info.maskGO);
				info.maskGO = null;
			}

			if (info.maskGO != null) {
				Transform power = info.maskGO.transform;
				float t = Mathf.InverseLerp(info.startDistance, info.endDistance, nextDistance);

				Vector3 childPos = transform.TransformPoint(info.child.pos);
				Vector3 parentPos = transform.TransformPoint(info.parent.pos);

				Vector3 direction = (childPos - parentPos).normalized;
				Vector3 start = parentPos - direction * powerTrailSize / 2f;
				Vector3 end = childPos + direction * powerTrailSize / 2f;
				Vector3 diff = end - start;
				float totalDistance = diff.magnitude;

				// Mask GameObject hitting full size
				if (prevDistance < info.endDistance && nextDistance >= info.endDistance) {
					power.position = end - (diff / 2f);
					power.localScale = new Vector3(powerTrailSize, powerTrailSize, totalDistance);
				}
				// Anywhere in-between
				else {
					power.position = Vector3.Lerp(start, end - (diff / 2f), t);
					power.localScale = Vector3.Lerp(new Vector3(powerTrailSize, powerTrailSize, 0), new Vector3(powerTrailSize, powerTrailSize, totalDistance), t);
				}
			}
		}

		float NextDistance() {
			if (powerIsOn && distance < maxDistance) {
				return Mathf.Min(maxDistance, distance + Time.deltaTime * speed);
			}
			else if (!powerIsOn && distance > 0) {
				return Mathf.Max(0, distance - Time.deltaTime * speed);
			}
			else return distance;
		}

		void UpdateState(float prevDistance, float nextDistance) {
			if (powerIsOn) {
				if (prevDistance == 0 && nextDistance > 0) {
					state = PowerTrailState.partiallyPowered;
				}
				else if (prevDistance < maxDistance && nextDistance == maxDistance) {
					state = PowerTrailState.powered;
				}
			}
			else if (!powerIsOn) {
				if (prevDistance == maxDistance && nextDistance < maxDistance) {
					state = PowerTrailState.partiallyPowered;
				}
				else if (prevDistance > 0 && nextDistance == 0) {
					state = PowerTrailState.depowered;
				}
			}
		}


		public static float gizmoSphereSize = 0.05f;
		private void OnDrawGizmos() {
			if (powerNodes.parentNode == null) return;

			DrawGizmosRecursively(powerNodes.parentNode);
		}

		Color unselectedColor = new Color(.15f, .85f, .25f);
		Color selectedColor = new Color(.95f, .95f, .15f);
		void DrawGizmosRecursively(Node curNode) {
			Gizmos.color = (curNode == powerNodes.selectedNode) ? selectedColor : unselectedColor;

			foreach (int childId in curNode.childrenIds) {
				Node child = powerNodes.GetNode(childId);
				if (child != null) {
					DrawWireBox(curNode.pos, child.pos);
				}
			}
			foreach (int childId in curNode.childrenIds) {
				Node child = powerNodes.GetNode(childId);
				if (child != null) {
					DrawGizmosRecursively(child);
				}
			}
		}

		void DrawWireBox(Vector3 n1, Vector3 n2) {
			float halfBoxSize = powerTrailSize / 2f;
			Vector3 diff = n2 - n1;
			Vector3 absDiff = new Vector3(Mathf.Abs(diff.x), Mathf.Abs(diff.y), Mathf.Abs(diff.z));

			Vector3 bl, br, tl, tr;
			if (absDiff.x > absDiff.y && absDiff.x > absDiff.z) {
				bl = new Vector3(0, -1, -1);
				br = new Vector3(0, -1, 1);
				tl = new Vector3(0, 1, -1);
				tr = new Vector3(0, 1, 1);
			}
			else if (absDiff.y > absDiff.x && absDiff.y > absDiff.z) {
				bl = new Vector3(-1, 0, -1);
				br = new Vector3(-1, 0, 1);
				tl = new Vector3(1, 0, -1);
				tr = new Vector3(1, 0, 1);
			}
			else {
				bl = new Vector3(-1, -1, 0);
				br = new Vector3(-1, 1, 0);
				tl = new Vector3(1, -1, 0);
				tr = new Vector3(1, 1, 0);
			}

			Vector3[] from = new Vector3[4] {
			n1 - bl * halfBoxSize,
			n1 - br * halfBoxSize,
			n1 - tl * halfBoxSize,
			n1 - tr * halfBoxSize
		};
			Vector3[] to = new Vector3[4] {
			n2 - bl * halfBoxSize,
			n2 - br * halfBoxSize,
			n2 - tl * halfBoxSize,
			n2 - tr * halfBoxSize
		};

			Vector3 direction = diff.normalized;
			for (int i = 0; i < 4; i++) {
				Gizmos.DrawLine(from[i], to[i]);
			}
		}
	}
}
