﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.HexPillarRoom {
	public class HexPillarFloat : ObjectHover {

		// Use this for initialization
		protected override void Awake() {
			base.Awake();
			yAmplitude = Mathf.Log10(transform.localScale.magnitude) / 2;
			period = yAmplitude * 150;
		}
	}
}