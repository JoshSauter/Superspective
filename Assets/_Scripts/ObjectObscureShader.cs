using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectObscureShader : MonoBehaviour {
    private static RenderTexture discardRenderTexture;
    private Camera obscureShaderCamera;
    private MeshRenderer thisMeshRenderer;

    int currentWidth;
    int currentHeight;

    private void Awake() {
        currentWidth = Screen.width;
        currentHeight = Screen.height;

        obscureShaderCamera = GameObject.Find("ObscureShaderCamera").GetComponent<Camera>();
        thisMeshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnEnable() {
        thisMeshRenderer.material.SetFloat("_ResolutionX", Screen.width);
        thisMeshRenderer.material.SetFloat("_ResolutionY", Screen.height);

        if (discardRenderTexture == null || !discardRenderTexture.IsCreated()) {
            CreateRenderTexture();
        }
    }

    private void OnDisable() {
        discardRenderTexture.Release();
    }
	
	// Update is called once per frame
	void Update () {
        // Update the resolution if necessary
        if (Screen.width != currentWidth || Screen.height != currentHeight) {
            discardRenderTexture.Release();

            currentWidth = Screen.width;
            currentHeight = Screen.height;
            CreateRenderTexture();

            thisMeshRenderer.material.SetFloat("_ResolutionX", currentWidth);
            thisMeshRenderer.material.SetFloat("_ResolutionY", currentHeight);
        }
        thisMeshRenderer.material.SetTexture("_DiscardTex", discardRenderTexture);
    }

    void CreateRenderTexture() {
        discardRenderTexture = new RenderTexture(currentWidth, currentHeight, 24);
        discardRenderTexture.enableRandomWrite = true;
        discardRenderTexture.Create();

        obscureShaderCamera.targetTexture = discardRenderTexture;
    }
}
