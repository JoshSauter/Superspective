using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyImageEffect : MonoBehaviour {
    PlayerMovement player;
    public Shader shader;
    public Material mat;

    float minIntensity = 0.05f;
    float maxIntensity = 5f;
    float desiredIntensity;
    float curIntensity;
    float lerpSpeedUp = .5f;
    float lerpSpeedDown = 5f;

	// Use this for initialization
	void Start () {
        if (mat == null && shader != null)
            mat = new Material(shader);

        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
	}

    private void Update() {
       desiredIntensity = Mathf.Lerp(minIntensity, maxIntensity, Mathf.InverseLerp(player.walkSpeed, player.runSpeed, player.curVelocity.magnitude-2.5f));

        float lerpSpeed = (curIntensity < desiredIntensity) ? lerpSpeedUp : lerpSpeedDown;
        curIntensity = Mathf.Lerp(curIntensity, desiredIntensity, Time.deltaTime * lerpSpeed);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        mat.SetFloat("_Intensity", curIntensity);
        Graphics.Blit(source, destination, mat);
    }
}
