using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckNormalDirection : MonoBehaviour {

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	[ContextMenu("Print transform.up")]
	void PrintUp() {
		BetterVectorPrint(transform.up);
	}
	[ContextMenu("Print transform.forward")]
	void PrintForward() {
		BetterVectorPrint(transform.forward);
	}
	[ContextMenu("Print transform.right")]
	void PrintRight() {
		BetterVectorPrint(transform.right);
	}

	void BetterVectorPrint(Vector3 p) {
		print("(" + p.x.ToString("F6") + ",  " + p.y.ToString("F6") + ",  " + p.z.ToString("F6") + ")");
	}
}
