using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class NodeSystem : MonoBehaviour {
	private int _curId = 0;
	public List<int> keys = new List<int>();
	public List<Node> values = new List<Node>();

	public Dictionary<int, Node> allNodes;
	public Node parentNode;
	public Node selectedNode;

	private void OnEnable() {
		if (allNodes == null) {
			if (keys == null) {
				Debug.LogWarning("Recreating keys list");
				keys = new List<int>();
			}
			if (values == null) {
				Debug.LogWarning("Recreating values list");
				values = new List<Node>();
			}

			Debug.LogWarning("Recreating allNodes dict from keys and values lists");
			allNodes = new Dictionary<int, Node>();
			for (int i = 0; i < keys.Count; i++) {
				allNodes.Add(keys[i], values[i]);
			}
		}
		if (parentNode == null) {
			parentNode = new Node(_curId++, transform.localPosition);
			keys.Add(parentNode.id);
			values.Add(parentNode);
			allNodes.Add(parentNode.id, parentNode);
			selectedNode = parentNode;
		}
	}

	public Node GetNode(int id) {
		if (allNodes != null && allNodes.ContainsKey(id)) {
			return allNodes[id];
		}
		else return null;
	}

	public List<Node> GetAllNodes() {
		List<Node> nodes = new List<Node>();
		GetAllNodesRecursively(parentNode, ref nodes);
		return nodes;
	}

	void GetAllNodesRecursively(Node curNode, ref List<Node> nodesSoFar) {
		nodesSoFar.Add(curNode);
		foreach (int childId in curNode.childrenIds) {
			Node child = GetNode(childId);
			if (child != null) {
				GetAllNodesRecursively(GetNode(childId), ref nodesSoFar);
			}
		}
	}

	private static float gizmoSphereSize = 0.05f;
	private void OnDrawGizmos() {
		if (parentNode == null) return;

		DrawGizmosRecursively(parentNode);
	}

	Color unselectedColor = new Color(.15f, .85f, .25f);
	Color selectedColor = new Color(.95f, .95f, .15f);
	void DrawGizmosRecursively(Node curNode) {
		Gizmos.color = (curNode == selectedNode) ? selectedColor : unselectedColor;
		Gizmos.DrawSphere(transform.TransformPoint(curNode.pos), gizmoSphereSize);

		foreach (int childId in curNode.childrenIds) {
			Node child = GetNode(childId);
			if (child != null) {
				DrawGizmosRecursively(child);
			}
		}
	}

#if UNITY_EDITOR
	[MenuItem("Custom/Power Trails/Reset Transform")]
	public static void ResetTransform() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem s = selected.GetComponent<NodeSystem>();
			if (s != null) {
				ResetTransformRecursively(s, s.parentNode);
			}
		}
	}

	static void ResetTransformRecursively(NodeSystem s, Node curNode) {
		curNode.pos = s.transform.InverseTransformPoint(curNode.pos);

		foreach (int childId in curNode.childrenIds) {
			Node child = s.GetNode(childId);
			if (child != null) {
				ResetTransformRecursively(s, child);
			}
		}
	}

	[MenuItem("Custom/Power Trails/Clear Nodes")]
	public static void ClearNodes() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem s = selected.GetComponent<NodeSystem>();
			if (s != null) {
				s.UnregisterNodesRecursively(s.parentNode);
				Node newParentNode = new Node(s._curId++, s.transform.localPosition);
				s.keys.Add(newParentNode.id);
				s.values.Add(newParentNode);
				s.allNodes.Add(newParentNode.id, newParentNode);
				s.parentNode = newParentNode;
				s.selectedNode = newParentNode;
			}
		}
	}

	[MenuItem("Custom/Power Trails/Add Child _F2")]
	public static void AddChild() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem s = selected.GetComponent<NodeSystem>();
			if (s != null && s.selectedNode != null) {
				Node newNode = s.selectedNode.AddNewChild(s._curId++);
				s.keys.Add(newNode.id);
				s.values.Add(newNode);
				s.allNodes.Add(newNode.id, newNode);
				s.selectedNode = newNode;
			}
		}
	}

	void UnregisterNodesRecursively(Node curNode) {
		foreach (int childId in curNode.childrenIds) {
			Node child = GetNode(childId);
			if (child != null) {
				UnregisterNodesRecursively(child);
			}
		}

		allNodes.Remove(curNode.id);
		keys.Remove(curNode.id);
		values.RemoveAll(n => n.id == curNode.id);
	}
#endif
}

[System.Serializable]
public class Node {
	public int id;
	public int parentId = -1;
	public List<int> childrenIds = new List<int>();
	public Vector3 pos;

	public Node(int id, Vector3 pos) {
		this.childrenIds = new List<int>();
		this.pos = pos;
		this.id = id;
	}

	public Node AddNewChild(int id) {
		Node newNode = new Node(id, pos);
		newNode.parentId = this.id;
		this.childrenIds.Add(newNode.id);

		return newNode;
	}
}
