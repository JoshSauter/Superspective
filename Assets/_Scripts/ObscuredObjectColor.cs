using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObscuredObjectColor : RecolorOnTeleport {
	public Color color;
	public Material[] unlitMaterials;
	Renderer _renderer;
	MaterialPropertyBlock propBlock;

	protected override void Awake() {
		base.Awake();
		propBlock = new MaterialPropertyBlock();
		_renderer = GetComponent<Renderer>();

		SetColor();
	}

	public override void SetColor() {
		_renderer.GetPropertyBlock(propBlock);

		if (unlitMaterials != null && unlitMaterials.Length > 0) {
			Debug.AssertFormat(unlitMaterials.Length == NUM_MATERIALS, "There should be 4 material colors");
			propBlock.SetColor("_Color", unlitMaterials[index].GetColor("_Color"));
		}
		else {
			propBlock.SetColor("_Color", color);
		}
		_renderer.SetPropertyBlock(propBlock);
	}
}
