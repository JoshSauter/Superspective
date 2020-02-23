﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class InteractableGlowController : Singleton<InteractableGlowController> {

	private CommandBuffer _commandBuffer;

	private HashSet<InteractableGlow> _glowableObjects = new HashSet<InteractableGlow>();

	public int blurIterations = 4;

	private Material prePassMaterial;
	private Material _blurMaterial;
	private Vector2 _blurTexelSize;

	private int _prePassRenderTexID;
	private int _blurPassRenderTexID;
	private int _tempRenderTexID;
	private int _blurSizeID;
	private int _glowColorID;

	public void Add(InteractableGlow glowObj) {
		_glowableObjects.Add(glowObj);
	}

	public void Remove(InteractableGlow glowObj) {
		_glowableObjects.Remove(glowObj);
	}

	/// <summary>
	/// On Awake, we cache various values and setup our command buffer to be called Before Image Effects.
	/// </summary>
	private void Awake() {
		prePassMaterial = new Material(Shader.Find("Hidden/GlowCmdShader"));
		_blurMaterial = new Material(Shader.Find("Hidden/Blur"));

		_prePassRenderTexID = Shader.PropertyToID("_GlowPrePassTex");
		_blurPassRenderTexID = Shader.PropertyToID("_GlowBlurredTex");
		_tempRenderTexID = Shader.PropertyToID("_TempTex0");
		_blurSizeID = Shader.PropertyToID("_BlurSize");
		_glowColorID = Shader.PropertyToID("_GlowColor");

		_commandBuffer = new CommandBuffer();
		_commandBuffer.name = "Glowing Objects Buffer"; // This name is visible in the Frame Debugger, so make it a descriptive!
		GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeImageEffects, _commandBuffer);
	}

	/// <summary>
	/// Adds all the commands, in order, we want our command buffer to execute.
	/// Similar to calling sequential rendering methods insde of OnRenderImage().
	/// </summary>
	private void RebuildCommandBuffer() {
		_commandBuffer.Clear();

		_commandBuffer.GetTemporaryRT(_prePassRenderTexID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, QualitySettings.antiAliasing);
		_commandBuffer.SetRenderTarget(_prePassRenderTexID);
		_commandBuffer.ClearRenderTarget(true, true, Color.clear);

		//Debug.Log("Glowable obj count: " + _glowableObjects.Count);
		foreach (var glowObject in _glowableObjects) {
			_commandBuffer.SetGlobalColor(_glowColorID, glowObject.CurrentColor);

			for (int j = 0; j < glowObject.Renderers.Length; j++) {
				//Debug.Log(glowObject.name + "length: " + glowObject.Renderers.Length);
				_commandBuffer.DrawRenderer(glowObject.Renderers[j], prePassMaterial);
			}
		}

		_commandBuffer.GetTemporaryRT(_blurPassRenderTexID, Screen.width >> 1, Screen.height >> 1, 0, FilterMode.Bilinear);
		_commandBuffer.GetTemporaryRT(_tempRenderTexID, Screen.width >> 1, Screen.height >> 1, 0, FilterMode.Bilinear);
		_commandBuffer.Blit(_prePassRenderTexID, _blurPassRenderTexID);

		_blurTexelSize = new Vector2(1.5f / (Screen.width >> 1), 1.5f / (Screen.height >> 1));
		_commandBuffer.SetGlobalVector(_blurSizeID, _blurTexelSize);

		for (int i = 0; i < blurIterations; i++) {
			_commandBuffer.Blit(_blurPassRenderTexID, _tempRenderTexID, _blurMaterial, 0);
			_commandBuffer.Blit(_tempRenderTexID, _blurPassRenderTexID, _blurMaterial, 1);
		}
	}

	/// <summary>
	/// Rebuild the Command Buffer each frame to account for changes in color.
	/// This could be improved to only rebuild when necessary when colors are changing.
	/// 
	/// Could be further optimized to not include objects which are currently black and not
	/// affect thing the glow image.
	/// </summary>
	private void Update() {
		RebuildCommandBuffer();
	}
}