using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// For use when the default materials aren't good enough (i.e. Purple)
public class ObscuringObjectColor : MonoBehaviour {
	public Color color;
	Renderer _renderer;
	MaterialPropertyBlock propBlock;

	void Awake() {
		propBlock = new MaterialPropertyBlock();
		_renderer = GetComponent<Renderer>();

		SetColor();
	}

	public void SetColor() {
		_renderer.GetPropertyBlock(propBlock);
		propBlock.SetColor("_Color", color);
		_renderer.SetPropertyBlock(propBlock);
	}
}
