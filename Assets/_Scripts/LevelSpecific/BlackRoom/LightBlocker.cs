using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using EpitaphUtils;

namespace LevelSpecific.BlackRoom {
	public class LightBlocker : MonoBehaviour {
		ColorPuzzle puzzle;

		public bool DEBUG = false;
		public Gradient DEBUGGRADIENT;
		[System.Serializable]
		public struct LightSourceBlocker {
			public string name;
			public UnityEngine.Transform source;
			public Material lightBlockerMaterial;
			public Mesh mesh;
			public List<Vector3> vertsToUse;
			public GameObject blocker;
		}

		public LightSourceBlocker[] blockers;
		public UnityEngine.Transform redSource;
		public UnityEngine.Transform greenSource;
		public UnityEngine.Transform blueSource;

		Mesh thisMesh;
		MeshRenderer thisRenderer;

		// Use this for initialization
		void Start() {
			thisRenderer = GetComponent<MeshRenderer>();
			thisMesh = GetComponent<MeshFilter>().mesh;

			puzzle = GetComponentInParent<ColorPuzzle>();
		}

		void Update() {
			foreach (var b in blockers) {
				if (b.blocker != null) {
					b.blocker.SetActive(puzzle.isActive);
				}
			}
		}

		// Update is called once per frame
		void LateUpdate() {
			Bounds bounds = thisRenderer.bounds;
			List<Vector3> vertices = new List<Vector3>();
			thisMesh.GetVertices(vertices);
			Vector3 center = thisRenderer.bounds.center;
			vertices = vertices.Distinct().Select(v => transform.TransformPoint(v * 1.001f)).ToList();

			for (int i = 0; i < blockers.Length; i++) {
				blockers[i].vertsToUse = GetVertsToUseForSource(blockers[i].source, vertices);
				BuildMeshForBlocker(ref blockers[i]);
			}
		}

		List<Vector3> GetVertsToUseForSource(UnityEngine.Transform lightSource, List<Vector3> vertices) {
			return vertices.Where(v => {
				Vector3 ray = v - lightSource.position;
				RaycastHit hitInfo;
				Physics.Raycast(lightSource.position, ray, out hitInfo, ray.magnitude * 2);
				return (hitInfo.collider == null || hitInfo.collider.gameObject != gameObject);
			}).Select(v => lightSource.InverseTransformPoint(v)).ToList();
		}

		public class VertexComparer : IComparer<Vector3> {
			Vector3 normal;

			public VertexComparer(Vector3 normal) {
				this.normal = normal.normalized;
			}

			public int Compare(Vector3 a, Vector3 b) {
				Vector3 projA = Vector3.ProjectOnPlane(a, normal);
				Vector3 projB = Vector3.ProjectOnPlane(b, normal);
				Vector3 projX = Vector3.ProjectOnPlane(Vector3.right, normal);
				Vector3 projY = Vector3.Cross(normal, projX);

				Vector2 a2D = new Vector2(Vector3.Dot(projA, projX), Vector3.Dot(projA, projY));
				Vector2 b2D = new Vector2(Vector3.Dot(projB, projX), Vector3.Dot(projB, projY));

				PolarCoordinate aPolar = PolarCoordinate.CartesianToPolar(new Vector3(a2D.x, 0, a2D.y));
				PolarCoordinate bPolar = PolarCoordinate.CartesianToPolar(new Vector3(b2D.x, 0, b2D.y));

				if (aPolar.angle.radians == bPolar.angle.radians) return 0;
				else return (aPolar.angle.radians > bPolar.angle.radians) ? 1 : -1;
			}
		}

		void BuildMeshForBlocker(ref LightSourceBlocker blocker) {
			if (blocker.mesh == null) {
				UnityEngine.Transform source = blocker.source;
				GameObject go = new GameObject();
				go.layer = source.gameObject.layer;
				go.name = gameObject.name + " " + blocker.name;
				go.transform.SetParent(source, false);
				blocker.mesh = new Mesh();
				go.AddComponent<MeshFilter>().mesh = blocker.mesh;
				go.AddComponent<MeshRenderer>().material = blocker.lightBlockerMaterial;
				blocker.blocker = go;
			}

			List<Vector3> vertsToUse = blocker.vertsToUse;
			vertsToUse = vertsToUse.OrderByDescending(v => v, new VertexComparer(blocker.source.InverseTransformVector(transform.position - blocker.source.position))).ToList();
			if (DEBUG) {
				for (int i = 0; i < vertsToUse.Count; i++) {
					Debug.DrawRay(blocker.source.position, blocker.source.TransformVector(vertsToUse[i]), DEBUGGRADIENT.Evaluate(((float)i) / vertsToUse.Count));
				}
			}
			vertsToUse.Add(Vector3.zero);
			vertsToUse.Add(blocker.source.InverseTransformPoint(transform.position));
			List<int> indices = new List<int>();
			// Indices for sides of shape
			for (int i = 0; i < vertsToUse.Count - 3; i++) {
				indices.Add(i);
				indices.Add(i + 1);
				indices.Add(vertsToUse.Count - 2);
			}
			indices.Add(vertsToUse.Count - 3);
			indices.Add(0);
			indices.Add(vertsToUse.Count - 2);
			// Indices for end of shape
			for (int i = 0; i < vertsToUse.Count - 3; i++) {
				indices.Add(i + 1);
				indices.Add(i);
				indices.Add(vertsToUse.Count - 1);
			}
			indices.Add(0);
			indices.Add(vertsToUse.Count - 3);
			indices.Add(vertsToUse.Count - 1);

			blocker.mesh.triangles = new int[0];
			blocker.mesh.SetVertices(vertsToUse.Select(v => v * 3).ToList());
			blocker.mesh.triangles = indices.ToArray();
		}
	}
}