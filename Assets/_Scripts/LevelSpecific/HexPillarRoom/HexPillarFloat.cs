using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexPillarFloat : ObjectHover {

	// Use this for initialization
	void Awake () {
		maxDisplacementUp = Mathf.Log10(transform.localScale.magnitude) / 2;
		period = maxDisplacementUp * 150;
	}
}
