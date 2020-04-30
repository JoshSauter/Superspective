using System.Collections;
using UnityEditor;
using UnityEngine;
using PowerTrailMechanics;

[CustomEditor(typeof(NodeSystem))]
public class NodeEditor : Editor {
	SceneView sv;
	Camera sceneViewCam;

	void OnSceneGUI() {
		// get the chosen game object
		NodeSystem t = target as NodeSystem;

		if (sv == null || sceneViewCam == null) {
			sv = EditorWindow.GetWindow<SceneView>();
			sceneViewCam = sv.camera;
		}

		if (t == null || t.parentNode == null) {
			return;
		}

		Event current = Event.current;
		int controlID = GUIUtility.GetControlID(t.GetHashCode(), FocusType.Passive);
		var nodeToSelect = NodeToSelect(t);
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
			if (nodeToSelect != null) {
				t.selectedNode = nodeToSelect;
				Event.current.Use();
			}
		}
		if (current.type == EventType.Layout) {
			if (nodeToSelect != null) {
				HandleUtility.AddDefaultControl(controlID);
			}
		}

		DrawLinesRecursively(t, t.parentNode);
		EditorGUI.BeginChangeCheck();
		Vector3 newPosition = t.transform.TransformPoint(t.selectedNode.pos);
		if (t.selectedNode != null) {
			newPosition = Handles.PositionHandle(t.transform.TransformPoint(t.selectedNode.pos), t.transform.rotation);
		}
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(t, "Change node position");
			t.selectedNode.pos = t.transform.InverseTransformPoint(newPosition);
		}
	}

	void DrawLinesRecursively(NodeSystem system, Node curNode) {
		foreach (int childId in curNode.childrenIds) {
			Node child = system.GetNode(childId);
			if (child != null) {
				Handles.DrawLine(system.transform.TransformPoint(curNode.pos), system.transform.TransformPoint(child.pos));
				DrawLinesRecursively(system, child);
			}
		}
	}

	Node NodeToSelect(NodeSystem t) {
		Vector2 guiPosition = Event.current.mousePosition;
		Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);

		foreach (var node in t.GetAllNodes()) {
			if (HitGizmoSphere(t.transform.TransformPoint(node.pos), ray)) {
				return node;
			}
		}

		return null;
	}

	bool HitGizmoSphere(Vector3 center, Ray ray) {
		Vector3 oc = ray.origin - center;
		float a = Vector3.Dot(ray.direction, ray.direction);
		float b = 2.0f * Vector3.Dot(oc, ray.direction);
		float c = Vector3.Dot(oc, oc) - PowerTrailMechanics.PowerTrail.gizmoSphereSize * PowerTrailMechanics.PowerTrail.gizmoSphereSize;
		float discriminant = b * b - 4 * a * c;
		return (discriminant > 0);
	}
}