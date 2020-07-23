using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct SerializableNode {
	public Vector3 pos;
	public int indexOfParent;
	public int childCount;
	public int indexOfFirstChild;
}
public class Node {
	private const float distanceToSpawnNewNodeAt = 0.5f;
	public Node parent;
	public List<Node> children = new List<Node>();
	public Vector3 pos;

	public Node(Vector3 pos) {
		this.children = new List<Node>();
		this.pos = pos;
	}

	public Node AddNewChild() {
		Vector3 grandparentToParent = Vector3.forward;
		if (parent != null) {
			grandparentToParent = (pos - parent.pos).normalized;
		}
		if (NodeSystem.buildAsStaircase) {
			grandparentToParent = Vector3.zero;
		}
		Node newNode = new Node(pos + grandparentToParent * distanceToSpawnNewNodeAt);
		newNode.parent = this;
		this.children.Add(newNode);

		return newNode;
	}

	public bool isRootNode { get { return parent == null; } }
	public bool isLeafNode { get { return children.Count == 0; } }
}

[ExecuteInEditMode]
public class NodeSystem : MonoBehaviour, ISerializationCallbackReceiver {
	// Used for special logic around repositioning nodes/placing new nodes
	public static bool buildAsStaircase = true;
	public bool showNodes = true;
	public List<Node> allNodes;
	[HideInInspector]
	public Node rootNode;
	public int selectedNodeIndex = -1;
	public List<SerializableNode> serializedNodes;

	public void OnBeforeSerialize() {
		// Unity is about to read the serializedNodes field's contents.
		// The correct data must now be written into that field "just in time".
		if (serializedNodes == null) serializedNodes = new List<SerializableNode>();
		if (rootNode == null) rootNode = new Node(Vector3.forward);
		serializedNodes.Clear();
		AddNodeToSerializedNodesRecursively(rootNode, -1);
		// Now Unity is free to serialize this field, and we should get back the expected 
		// data when it is deserialized later.
	}

	void AddNodeToSerializedNodesRecursively(Node n, int parentId) {
		Vector3 pos = n.pos;
		int thisIndex = serializedNodes.Count;
		var serializedNode = new SerializableNode() {
			indexOfParent = parentId,
			pos = pos,
			childCount = n.children.Count,
			indexOfFirstChild = serializedNodes.Count + 1
		};
		serializedNodes.Add(serializedNode);
		if (serializedNode.pos == selectedNode?.pos) {
			selectedNodeIndex = thisIndex;
		}
		foreach (var child in n.children) {
			AddNodeToSerializedNodesRecursively(child, thisIndex);
		}
	}

	public void OnAfterDeserialize() {
		//Unity has just written new data into the serializedNodes field.
		//let's populate our actual runtime data with those new values.
		if (serializedNodes.Count > 0) {
			ReadNodeFromSerializedNodesRecursively(0, out rootNode);
		}
		else {
			rootNode = new Node(Vector3.forward);
		}
	}

	int ReadNodeFromSerializedNodesRecursively(int index, out Node node) {
		var serializedNode = serializedNodes[index];
		// Transfer the deserialized data into the internal Node class
		Node newNode = new Node(serializedNode.pos);

		// The tree needs to be read in depth-first, since that's how we wrote it out.
		for (int i = 0; i != serializedNode.childCount; i++) {
			Node childNode;
			index = ReadNodeFromSerializedNodesRecursively(++index, out childNode);
			childNode.parent = newNode;
			newNode.children.Add(childNode);
		}
		node = newNode;
		return index;
	}

	private Node _selectedNode;
	public Node selectedNode {
		get {
			if (_selectedNode == null && rootNode != null) {
				_selectedNode = rootNode;
			}
			return _selectedNode;
		}
		set {
			_selectedNode = value;
		}
	}

	[ShowNativeProperty]
	public int Count => allNodes.Count;

	private void OnEnable() {
		Initialize();
	}

	void Initialize() {
		if (allNodes == null) {
			//Debug.LogWarning("Recreating allNodes dict from keys and values lists");
			allNodes = new List<Node>();
			if (rootNode != null) {
				allNodes = GetAllNodes();
			}
		}
		if (rootNode == null) {
			// Spawn at not-the-origin so it can be selected with the handle
			rootNode = new Node(Vector3.forward);
			allNodes.Add(rootNode);
			selectedNode = rootNode;
		}
	}

	public Node AddNewChildToSelected() {
		Node newNode = null;
		if (selectedNode != null) {
			newNode = selectedNode.AddNewChild();
			allNodes.Add(newNode);

			selectedNode = newNode;
		}

		return newNode;
	}

	public void RemoveSelected() {
		if (selectedNode != null) {
			RemoveNodeRecursively(selectedNode);
		}
	}

	void RemoveNodeRecursively(Node curNode) {
		curNode.parent?.children.Remove(curNode);
		allNodes.Remove(curNode);
		foreach (var child in curNode.children) {
			RemoveNodeRecursively(child);
		}
	}

	public List<Node> GetAllNodes() {
		List<Node> nodes = new List<Node>();
		GetAllNodesRecursively(rootNode, ref nodes);
		return nodes;
	}

	void GetAllNodesRecursively(Node curNode, ref List<Node> nodesSoFar) {
		nodesSoFar.Add(curNode);
		foreach (Node child in curNode.children) {
			if (child != null) {
				GetAllNodesRecursively(child, ref nodesSoFar);
			}
		}
	}

	private static float gizmoSphereSize = 0.15f;
	private void OnDrawGizmos() {
		if (rootNode == null) return;

		DrawGizmosRecursively(rootNode);
	}

	Color unselectedColor = new Color(.15f, .85f, .25f);
	Color selectedColor = new Color(.95f, .95f, .15f);
	void DrawGizmosRecursively(Node curNode) {
		if (!showNodes) return;
		Gizmos.color = (curNode == selectedNode) ? selectedColor : unselectedColor;
		Gizmos.DrawSphere(transform.TransformPoint(curNode.pos), gizmoSphereSize);

		foreach (Node child in curNode.children) {
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
				ResetTransformRecursively(s, s.rootNode);
			}
		}
	}

	static void ResetTransformRecursively(NodeSystem nodeSystem, Node curNode) {
		curNode.pos = nodeSystem.transform.InverseTransformPoint(curNode.pos);

		foreach (Node child in curNode.children) {
			if (child != null) {
				ResetTransformRecursively(nodeSystem, child);
			}
		}
	}

	[MenuItem("Custom/Power Trails/Clear Nodes")]
	public static void ClearNodes() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem nodeSystem = selected.GetComponent<NodeSystem>();
			if (nodeSystem != null) {
				nodeSystem.allNodes = new List<Node>();
				nodeSystem.rootNode = null;
				nodeSystem.Initialize();
			}
		}
	}

	static bool temp = false;
	[MenuItem("Custom/Power Trails/Add Child _F2")]
	public static void AddChild() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem ns = selected.GetComponent<NodeSystem>();
			if (ns != null && ns.selectedNode != null) {
				Node newNode = ns.AddNewChildToSelected();
				// Make it easy to do staircases:
				if (buildAsStaircase) {
					newNode.pos += 0.5f * (temp ? Vector3.right : Vector3.up);
				}
				temp = !temp;
			}
		}
	}

	[MenuItem("Custom/Power Trails/Remove Child _F3")]
	public static void RemoveNode() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem ns = selected.GetComponent<NodeSystem>();
			if (ns != null && ns.selectedNode != null) {
				ns.RemoveSelected();
			}
		}
	}
#endif
}
