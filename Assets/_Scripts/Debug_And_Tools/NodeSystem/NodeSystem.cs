using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using NaughtyAttributes;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct SerializableNode {
	public Vector3 pos;
	public int indexOfParent;
	public int childCount;
	public int indexOfFirstChild;
	public bool zeroDistanceToChildren;
	public bool staircaseSegment;
}
public class Node {
	public Node parent;
	public List<Node> children = new List<Node>();
	public Vector3 pos;
	public bool zeroDistanceToChildren = false;
	public bool staircaseSegment = false;

	public Node(Vector3 pos, bool zeroDistanceToChildren, bool staircaseSegment) {
		this.children = new List<Node>();
		this.pos = pos;
		this.zeroDistanceToChildren = zeroDistanceToChildren;
		this.staircaseSegment = staircaseSegment;
	}

	public Node AddNewChild(Vector3 offset) {
		Node newNode = new Node(pos + offset, false, false);
		newNode.parent = this;
		this.children.Add(newNode);

		return newNode;
	}

	public bool IsRootNode => parent == null;
	public bool IsLeafNode => children.Count == 0;
}

[ExecuteInEditMode]
public class NodeSystem : MonoBehaviour, ISerializationCallbackReceiver {
	const float distanceToSpawnNewNodeAt = 0.25f;
	// Used for special logic around repositioning nodes/placing new nodes
	public bool buildAsStaircase = true;
	public Vector3 staircaseDirection1 = Vector3.right, staircaseDirection2 = Vector3.up;
	public bool showNodes = true;
	public List<Node> allNodes;
	[HideInInspector]
	public Node rootNode;
	public List<int> selectedNodesIndices = new List<int>();
	public int startOfStaircaseIndex = -1;
	public List<SerializableNode> serializedNodes;

	[ShowNativeProperty]
	public bool NodeSelected => selectedNodesIndices.Count > 0;
	
	public Vector3 WorldPos(Node n) {
		return transform.TransformPoint(n.pos);
	}

	public void OnBeforeSerialize() {
		// Unity is about to read the serializedNodes field's contents.
		// The correct data must now be written into that field "just in time".
		if (serializedNodes == null) serializedNodes = new List<SerializableNode>();
		if (rootNode == null) rootNode = new Node(Vector3.up * 0.0625f, false, false);
		serializedNodes.Clear();
		selectedNodesIndices.Clear();
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
			indexOfFirstChild = serializedNodes.Count + 1,
			zeroDistanceToChildren = n.zeroDistanceToChildren,
			staircaseSegment = n.staircaseSegment
		};
		serializedNodes.Add(serializedNode);
		if (selectedNodes.Select(n => n.pos).Any(p => p == serializedNode.pos)) {
			selectedNodesIndices.Add(thisIndex);
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
			rootNode = new Node(Vector3.down * 0.0625f, false, false);
		}
	}

	int ReadNodeFromSerializedNodesRecursively(int index, out Node node) {
		var serializedNode = serializedNodes[index];
		// Transfer the deserialized data into the internal Node class
		Node newNode = new Node(serializedNode.pos, serializedNode.zeroDistanceToChildren, serializedNode.staircaseSegment);

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

	HashSet<Node> _selectedNodes = new HashSet<Node>();
	public HashSet<Node> selectedNodes {
		get {
			if ((_selectedNodes == null || _selectedNodes.Count == 0) && rootNode != null) {
				_selectedNodes = new HashSet<Node> {rootNode};
			}
			return _selectedNodes;
		}
		set => _selectedNodes = value;
	}

	[ShowNativeProperty]
	public int Count => allNodes?.Count ?? 0;

	void OnEnable() {
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
			rootNode = new Node(Vector3.down * 0.0625f, false, false);
			allNodes.Add(rootNode);
			selectedNodes = new HashSet<Node> {rootNode};
		}
	}

	private Node AddNewChildToNode(Node parent, bool useOffset = true) {
		Vector3 grandparentToParent = Vector3.forward;

		if (buildAsStaircase) {
			grandparentToParent = Vector3.zero;
		}
		else if (parent.parent != null) {
			grandparentToParent = (parent.pos - parent.parent.pos).normalized;
		}

		Vector3 offset = Vector3.zero;
		if (useOffset) {
			offset = grandparentToParent * distanceToSpawnNewNodeAt;
		}

		Node newNode = parent.AddNewChild(offset);
		allNodes.Add(newNode);

		return newNode;
	}

	public HashSet<Node> AddNewChildToSelected(bool useOffset = true) {
		selectedNodes = selectedNodes.Select(selectedNode => AddNewChildToNode(selectedNode, useOffset)).ToHashSet();

		return selectedNodes;
	}

	// Similar to Add but creates a new Node which is a child of the selected Node and a parent of the selected Node's
	// children, then removes the selected Node's children
	public HashSet<Node> InsertNewChildAfterSelected() {
		selectedNodes = selectedNodes.Select(selectedNode => {
			// When the selected Node has no children, functions same as AddNewChildToSelected
			if (selectedNode.children == null || selectedNode.children.Count == 0) {
				return AddNewChildToNode(selectedNode);
			}
			else {
				Node parent = selectedNode;
				List<Node> children = parent.children;

				Vector3 avgChildPosition = children
					.Select(node => node.pos)
					.Aggregate(Vector3.zero, (acc, pos) => acc + pos) / children.Count;

				Vector3 newNodePos = Vector3.Lerp(parent.pos, avgChildPosition, 0.5f);

				parent.children = new List<Node>();
				Node newNode = parent.AddNewChild(newNodePos - parent.pos);
				newNode.children = children;
				foreach (var child in children) {
					child.parent = newNode;
				}

				allNodes.Add(newNode);

				return newNode;
			}
		}).ToHashSet();

		return selectedNodes;
	}

	public void RemoveSelected() {
		selectedNodes = selectedNodes.Select(selectedNode => {
			Node nextNodeToSelect = selectedNode.parent;
			RemoveNodeRecursively(selectedNode);
			return nextNodeToSelect;
		}).ToHashSet();
	}

	void RemoveNodeRecursively(Node curNode) {
		// When multi-selecting nodes, don't want to try to remove a node that was already removed as a result of removing its parent
		// if (!allNodes.Contains(curNode)) return;
		
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

	public static float gizmoSphereSize = 0.05f;

	void OnDrawGizmos() {
		if (rootNode == null) return;

		DrawGizmosRecursively(rootNode);
	}

	Color unselectedColor = new Color(.15f, .85f, .25f);
	Color selectedColor = new Color(.95f, .95f, .15f);
	Color unselectedZeroDistanceToChildrenColor = new Color(.15f, .25f, .85f);
	Color unselectedStaircaseSegmentColor = new Color(.65f, .05f, .75f);
	void DrawGizmosRecursively(Node curNode) {
		if (!showNodes) return;
		bool selected = selectedNodes.Contains(curNode);
		float selectedT = selected ? (0.5f + 0.5f * Mathf.Sin(8*Time.realtimeSinceStartup)) : 0f;
		if (curNode.zeroDistanceToChildren) {
			Gizmos.color = Color.Lerp(unselectedZeroDistanceToChildrenColor, selectedColor, selectedT);
		}
		else if (curNode.staircaseSegment) {
			Gizmos.color = Color.Lerp(unselectedStaircaseSegmentColor, selectedColor, selectedT);
		}
		else {
			if (selected) {
				Gizmos.color = selectedColor;
			}
			else {
				Gizmos.color = unselectedColor;
			}
		}
		Gizmos.DrawSphere(transform.TransformPoint(curNode.pos), gizmoSphereSize);

		foreach (Node child in curNode.children) {
			if (child != null) {
				if (!curNode.staircaseSegment && child.staircaseSegment) {
					DrawStaircaseSegment(child);
				}
				DrawGizmosRecursively(child);
			}
		}
	}

	private void DrawStaircaseSegment(Node start) {
		Vector3 startPos = transform.TransformPoint(start.pos);
		Node end = start;
		bool endsOnLeaf = false;
		while (end.staircaseSegment) {
			if (end.IsLeafNode) {
				endsOnLeaf = true;
				break;
			}
				
			// Axiom: Staircase segments only have one child
			end = end.children[0];
		}
		end = endsOnLeaf ? end : end.parent;

		Vector3 endPos = transform.TransformPoint(end.pos);

		Color prevColor = Gizmos.color;
		Gizmos.color = unselectedStaircaseSegmentColor;
		Gizmos.DrawLine(startPos, endPos);
		Gizmos.color = prevColor;
	}

#if UNITY_EDITOR
	/// <summary>
	/// Recursively searches up the tree to find the root node(s) of the selected nodes. If multiple selected nodes have
	/// the same root, that root is only included in the result once. A root in this context is the first node up the tree that is not in selectedNodes.
	/// The second item returned in the tuple is the relative leaf node(s) of each root node.
	/// </summary>
	/// <param name="selection">Nodes to search for the root(s) and leaf(s) of</param>
	/// <returns>A HashSet containing the de-duplicated (root, leaf(s)) nodes of the selection</returns>
	private Dictionary<Node, HashSet<Node>> FindRelativeRootsAndLeafsOfSelected(HashSet<Node> selection) {
		Dictionary<Node, HashSet<Node>> roots = new Dictionary<Node, HashSet<Node>>();

		bool IsSelected(Node node) => selection.Contains(node);

		bool AnyAncestorIsSelected(Node node) {
			if (node == null || node.IsRootNode) return false;
			if (IsSelected(node)) return true;

			return AnyAncestorIsSelected(node.parent);
		}
		
		bool AnyDescendantIsSelected(Node node) {
			bool AnyDescendantIsSelectedHelper(Node curNode) {
				if (IsSelected(curNode)) return true;
				if (curNode.IsLeafNode) return false;

				return curNode.children.Any(AnyDescendantIsSelectedHelper);
			}

			return node.children.Any(AnyDescendantIsSelectedHelper);
		}
		
		HashSet<Node> FindLeafsRecursively(Node curNode) {
			HashSet<Node> leafs = new HashSet<Node>();
			// If we are at the end of the tree, or there are no further selections further along this branch, we are a relative leaf
			if (IsSelected(curNode) && !AnyDescendantIsSelected(curNode)) {
				leafs.Add(curNode);
				return leafs;
			}

			foreach (Node child in curNode.children) {
				leafs.UnionWith(FindLeafsRecursively(child));
			}

			return leafs;
		}
		
		void FindRootsRecursively(Node curNode) {
			// Only consider nodes that are closest to the root of a given branch of a tree
			// If any ancestor (recursively) is selected, we'll use the ancestor to find the root instead
			if (AnyAncestorIsSelected(curNode.parent)) return;
			
			if (!IsSelected(curNode)) {
				// Current node is a root node, find the relative leaf node(s)
				HashSet<Node> relativeLeafs = FindLeafsRecursively(curNode);
				roots.Add(curNode, relativeLeafs);
				return;
			}

			if (curNode.parent != null) {
				FindRootsRecursively(curNode.parent);
			}
		}
		
		foreach (Node selectedNode in selection) {
			FindRootsRecursively(selectedNode);
		}

		// Certain situations may lead to nodes being created twice, for different branches. This massages the roots data to eliminate this issue
		void DeduplicateRoots() {
			bool HasAncestorThatIsAlsoARoot(Node node) {
				bool HasAncestorThatIsAlsoARootHelper(Node node) {
					if (node == null || node.IsRootNode) return false;
					return roots.ContainsKey(node) || AnyAncestorIsSelected(node.parent);
				}

				return HasAncestorThatIsAlsoARootHelper(node.parent);
			}

			Node HighestRootInSelection(Node lowerRoot) {
				Node highestRoot = lowerRoot;
				void HighestRoot(Node node) {
					if (node == null || node.IsRootNode) return;
					if (roots.ContainsKey(node)) {
						highestRoot = node;
					}

					HighestRoot(node.parent);
				}

				HighestRoot(lowerRoot.parent);
				return highestRoot;
			}
			
			// Dictionary<lower root node, root Node above it in the hierarchy>
			Dictionary<Node, Node> rootsToConsolidate = roots
				.Keys
				.Where(HasAncestorThatIsAlsoARoot)
				.Select(root => {
					Node highestAncestorInRoots = HighestRootInSelection(root);
					return (root, highestAncestorInRoots);
				}).ToDictionary(kv => kv.Item1, kv => kv.Item2);

			foreach ((Node lowerRoot, Node higherRoot) in rootsToConsolidate) {
				if (lowerRoot == higherRoot) continue;
				
				roots[higherRoot].ExceptWith(roots[lowerRoot]);
			}
		}

		DeduplicateRoots();
		return roots;
	}

	private static HashSet<Node> DuplicateBranches(Dictionary<Node, HashSet<Node>> rootsAndLeafs) {
		HashSet<Node> newNodes = new HashSet<Node>();

		Node DuplicateBranch(Node root, Node curNode) {
			Node newNode = new Node(curNode.pos, curNode.zeroDistanceToChildren, curNode.staircaseSegment);

			// We've made our way back to the root, add the new node to the root's children
			if (curNode.parent == root) {
				newNode.parent = root;
				
				root.children.Add(newNode);
				newNodes.Add(newNode);
				return newNode;
			}
			
			newNode.parent = DuplicateBranch(root, curNode.parent);
			
			newNode.parent.children.Add(newNode);
			newNodes.Add(newNode);
			return newNode;
		}
		
		foreach ((Node root, HashSet<Node> leafs) in rootsAndLeafs) {
			foreach (Node leaf in leafs) {
				DuplicateBranch(root, leaf);
			}
		}

		return newNodes;
	}
	
	[MenuItem("Custom/Power Trails/Symmetry/Duplicate Nodes Across X")]
	public static void DuplicateAcrossX() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem s = selected.GetComponent<NodeSystem>();
			if (s != null) {
				Dictionary<Node, HashSet<Node>> rootsAndLeafs = s.FindRelativeRootsAndLeafsOfSelected(s.selectedNodes);
				
				HashSet<Node> newNodes = DuplicateBranches(rootsAndLeafs);

				foreach (Node newNode in newNodes) {
					newNode.pos.x *= -1f;
				}
				
				Debug.Log($"Duplicated {newNodes.Count} nodes across X-axis");
			}
		}
	}

	[MenuItem("Custom/Power Trails/Symmetry/Duplicate Nodes Across Y")]
	public static void DuplicateAcrossY() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem s = selected.GetComponent<NodeSystem>();
			if (s != null) {
				Dictionary<Node, HashSet<Node>> rootsAndLeafs = s.FindRelativeRootsAndLeafsOfSelected(s.selectedNodes);

				HashSet<Node> newNodes = DuplicateBranches(rootsAndLeafs);

				foreach (Node newNode in newNodes) {
					newNode.pos.y *= -1f;
				}
				
				Debug.Log($"Duplicated {newNodes.Count} nodes across Y-axis");
			}
		}
	}
	
	[MenuItem("Custom/Power Trails/Symmetry/Duplicate Nodes Across Z")]
	public static void DuplicateAcrossZ() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem s = selected.GetComponent<NodeSystem>();
			if (s != null) {
				Dictionary<Node, HashSet<Node>> rootsAndLeafs = s.FindRelativeRootsAndLeafsOfSelected(s.selectedNodes);

				HashSet<Node> newNodes = DuplicateBranches(rootsAndLeafs);

				foreach (Node newNode in newNodes) {
					newNode.pos.z *= -1f;
				}
				
				Debug.Log($"Duplicated {newNodes.Count} nodes across Z-axis");
			}
		}
	}
	
	[MenuItem("Custom/Power Trails/Symmetry/Mirror Across X")]
	public static void MirrorAcrossX() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem s = selected.GetComponent<NodeSystem>();
			if (s != null) {
				for (int i = 0; i < s.serializedNodes.Count; i++) {
					SerializableNode thisNode = s.serializedNodes[i];
					thisNode.pos.x *= -1;
					s.serializedNodes[i] = thisNode;
				}

				for (int i = 0; i < s.allNodes.Count; i++) {
					s.allNodes[i].pos.x *= -1f;
				}
			}
		}
	}

	[MenuItem("Custom/Power Trails/Symmetry/Mirror Across Y")]
	public static void MirrorAcrossY() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem s = selected.GetComponent<NodeSystem>();
			if (s != null) {
				for (int i = 0; i < s.serializedNodes.Count; i++) {
					SerializableNode thisNode = s.serializedNodes[i];
					thisNode.pos.y *= -1;
					s.serializedNodes[i] = thisNode;
				}

				for (int i = 0; i < s.allNodes.Count; i++) {
					s.allNodes[i].pos.y *= -1f;
				}
			}
		}
	}

	[MenuItem("Custom/Power Trails/Symmetry/Mirror Across Z")]
	public static void MirrorAcrossZ() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem s = selected.GetComponent<NodeSystem>();
			if (s != null) {
				for (int i = 0; i < s.serializedNodes.Count; i++) {
					SerializableNode thisNode = s.serializedNodes[i];
					thisNode.pos.z *= -1f;
					s.serializedNodes[i] = thisNode;
				}

				for (int i = 0; i < s.allNodes.Count; i++) {
					s.allNodes[i].pos.z *= -1f;
				}
			}
		}
	}

	[MenuItem("Custom/Power Trails/Select All Nodes")]
	public static void SelectAllNodes() {
		foreach (GameObject selected in Selection.gameObjects) {
			NodeSystem s = selected.GetComponent<NodeSystem>();
			if (s != null) {
				s.selectedNodes = new HashSet<Node>(s.allNodes);
			}
		}
	}

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
			if (ns != null && ns.selectedNodes.Count > 0) {
				HashSet<Node> newNodes = ns.AddNewChildToSelected();
				// Make it easy to do staircases:
				if (ns.buildAsStaircase) {
					foreach (Node newNode in newNodes) {
						newNode.pos += 0.5f * (temp ? ns.staircaseDirection1 : ns.staircaseDirection2);
					}
				}
				temp = !temp;
			}
		}
	}

	[MenuItem("Custom/Power Trails/Remove Child _F3")]
	public static void RemoveNode() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem ns = selected.GetComponent<NodeSystem>();
			if (ns != null) {
				ns.RemoveSelected();
			}
		}
	}
	
	[MenuItem("Custom/Power Trails/Insert Child _F4")]
	public static void InsertChild() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem ns = selected.GetComponent<NodeSystem>();
			if (ns != null) {
				ns.InsertNewChildAfterSelected();
			}
		}
	}

	// Call once to mark start of staircase, again to mark end of staircase
	[MenuItem("Custom/Power Trails/Mark Staircase _F5")]
	public static void MarkStaircaseSegment() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem ns = selected.GetComponent<NodeSystem>();

			if (ns.selectedNodesIndices.Count > 1) {
				Debug.LogWarning("Cannot mark multiple nodes as part of a staircase segment. Please select only one node.");
				return;
			}
			
			if (ns != null && ns.selectedNodesIndices.Count == 1) {
				int selectedNodeIndex = ns.selectedNodesIndices[0];
				if (ns.startOfStaircaseIndex >= 0) {
					MarkStaircase(ns, selectedNodeIndex, ns.startOfStaircaseIndex);
					ns.startOfStaircaseIndex = -1;
				}
				else {
					ns.startOfStaircaseIndex = selectedNodeIndex;
				}
			}
		}
	}
	
	// Call to mark selected node as part of staircase
	[MenuItem("Custom/Power Trails/Mark Staircase Segment _F6")]
	public static void MarkStaircase() {
		foreach (var selected in Selection.gameObjects) {
			NodeSystem ns = selected.GetComponent<NodeSystem>();
			if (ns != null) {
				foreach (Node selectedNode in ns.selectedNodes) {
					selectedNode.staircaseSegment = !selectedNode.staircaseSegment;
				}
			}
		}
	}

	// Works its way from end to start index by navigating through parents, marking each node as part of staircase
	private static void MarkStaircase(NodeSystem ns, int curIndex, int startIndex) {
		if (curIndex == -1) {
			return;
		}

		SerializableNode node = ns.serializedNodes[curIndex];
		// Allows for toggling back and forth
		bool staircaseSegment = !node.staircaseSegment;
		node.staircaseSegment = staircaseSegment;
		ns.allNodes[curIndex].staircaseSegment = staircaseSegment;
		ns.serializedNodes[curIndex] = node;

		if (curIndex == startIndex) return;
		MarkStaircase(ns, node.indexOfParent, startIndex);
	}
#endif
}
