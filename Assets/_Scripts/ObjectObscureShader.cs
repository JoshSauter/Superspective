using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectObscureShader : MonoBehaviour {
    private static RenderTexture discardRenderTexture;
    private Camera obscureShaderCamera;

    private void Awake() {
        obscureShaderCamera = GameObject.Find("ObscureShaderCamera").GetComponent<Camera>();
    }

    private void OnEnable() {
        if (discardRenderTexture == null || !discardRenderTexture.IsCreated()) {
            discardRenderTexture = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 24);
            discardRenderTexture.enableRandomWrite = true;
            discardRenderTexture.Create();
        }
        obscureShaderCamera.targetTexture = discardRenderTexture;
    }

    private void OnDisable() {
        discardRenderTexture.Release();
    }
	
	// Update is called once per frame
	void Update () {
        GetComponent<MeshRenderer>().material.SetTexture("_DiscardTex", discardRenderTexture);
    }
}
