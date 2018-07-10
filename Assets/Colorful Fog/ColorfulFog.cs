using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Rendering/Colorful Fog")]
public class ColorfulFog : MonoBehaviour
{
    public bool useCustomDepthTexture = true;
    public bool distanceFog = true;
    public bool useRadialDistance = false;
    public bool heightFog = false;

    public float height = 0.0f;
    [Range(0.001f, 10.0f)]
    public float heightDensity = 2.0f;
    public float startDistance = 0.0f;
    public FogMode fogMode = FogMode.Exponential;
    public float fogDensity = 0.01f;
    public float fogStart = 50f, fogEnd = 100f;

    public enum ColoringMode
    {
        Solid = 0, Cube, SimpleGradient, Gradient, GradientTexture
    };
    public ColoringMode coloringMode = ColoringMode.Solid;
    public Cubemap fogCube;
    public Color solidColor = Color.magenta; //solid color.
    public Color skyColor = Color.red, equatorColor = Color.green, groundColor = Color.blue; //simple gradient.
    public Gradient gradient; //gradient.
    public int gradientResolution = 100; //gradient resolution, higher value = more CPU time.
    public Texture2D gradientTexture; //gradient texture.
    public Shader fogShader;
    public Shader customDepthShader;
    //public Texture2D customDepthNoiseTexture;

    protected Texture2D tmpGradientTexture;

    private Material fogMaterial = null;

    private Material GetFogMaterial()
    {
        if (fogMaterial == null)
        {
            if (fogShader == null)
            {
                Debug.LogError("fogShader is not assigned");
                this.enabled = false;
                return null;
            }
            else
            {
                fogMaterial = new Material(fogShader);
            }
        }
        fogMaterial.hideFlags = HideFlags.HideAndDontSave;
        return fogMaterial;
    }

    private Camera cam;
    private Camera depthCamera;
    private RenderTexture depthTexture;
    private Vector2 cachedResolution = new Vector2(Screen.width, Screen.height);

    private bool CheckResources()
    {
        if (GetFogMaterial() == null)
        {
            return false;
        }
        return true;
    }
    void OnDisable()
    {
        //free resources.
        if (depthCamera != null)
            DestroyImmediate(depthCamera.gameObject);

        if (depthTexture != null)
        {
            RenderTexture.ReleaseTemporary(depthTexture);
            depthTexture = null;
        }

        if (tmpGradientTexture != null)
        {
            DestroyImmediate(tmpGradientTexture);
            tmpGradientTexture = null;
        }
    }

    //Query for resolution changes if we are in custom depth mode.
    private bool ScreenResolutionChanged()
    {

        Vector2 currentResolution = new Vector2(Screen.width, Screen.height);
        if (currentResolution == cachedResolution)
        {
            cachedResolution = currentResolution;
            return false;
        }
        //else
        cachedResolution = currentResolution;
        return true;
    }

    //render custom depth on platforms that doesn't have depthTexture by default.
    void OnPreRender()
    {
        if (!useCustomDepthTexture)
        {
            //destroy depthCamera.
            if (depthCamera != null)
            {
                DestroyImmediate(depthCamera.gameObject);
            }
            return;
        }

        if (!enabled)
            return;

        if (cam == null)
            cam = this.GetComponent<Camera>();

        if (ScreenResolutionChanged())
        {
            //resize/update rt if its not null.
            if (depthTexture != null)
            {
                RenderTexture.ReleaseTemporary(depthTexture);
                depthTexture = null;
                depthTexture = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 8, RenderTextureFormat.Default);
            }
        }

        if (depthTexture == null) //create rt.
            depthTexture = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 8, RenderTextureFormat.Default);

        if (!depthCamera)
        {
            GameObject go = new GameObject("numYbmEEUj1dGQT6ybw9EPd2qr7ISj");
            depthCamera = go.AddComponent<Camera>();
            depthCamera.enabled = false;
            depthCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            //shaderCamera.hideFlags = HideFlags.HideAndDontSave;
        }

        depthCamera.CopyFrom(cam);
        depthCamera.backgroundColor = new Color(0, 0, 0, 0);
        depthCamera.clearFlags = CameraClearFlags.SolidColor;
        depthCamera.targetTexture = depthTexture;
        depthCamera.hideFlags = HideFlags.DontSave;
        //Shader.SetGlobalTexture("_NoiseTex", customDepthNoiseTexture);

        depthCamera.RenderWithShader(customDepthShader, "");
        Shader.SetGlobalTexture("_CustomDepthTexture", depthTexture);
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //just blit source -> dest.
        if (CheckResources() == false || (!distanceFog && !heightFog))
        {
            Graphics.Blit(source, destination);
            return;
        }
        //Camera cam = GetComponent<Camera>();
        if (cam == null)
            cam = this.GetComponent<Camera>();
        if (!useCustomDepthTexture)
            cam.depthTextureMode |= DepthTextureMode.Depth;

        Transform camtr = cam.transform;
        float camNear = cam.nearClipPlane;
        float camFar = cam.farClipPlane;
        float camFov = cam.fieldOfView;
        float camAspect = cam.aspect;

        Matrix4x4 frustumCorners = Matrix4x4.identity;

        float fovWHalf = camFov * 0.5f;

        Vector3 toRight = camtr.right * camNear * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * camAspect;
        Vector3 toTop = camtr.up * camNear * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

        Vector3 topLeft = (camtr.forward * camNear - toRight + toTop);
        float camScale = topLeft.magnitude * camFar / camNear;

        topLeft.Normalize();
        topLeft *= camScale;

        Vector3 topRight = (camtr.forward * camNear + toRight + toTop);
        topRight.Normalize();
        topRight *= camScale;

        Vector3 bottomRight = (camtr.forward * camNear + toRight - toTop);
        bottomRight.Normalize();
        bottomRight *= camScale;

        Vector3 bottomLeft = (camtr.forward * camNear - toRight - toTop);
        bottomLeft.Normalize();
        bottomLeft *= camScale;

        frustumCorners.SetRow(0, topLeft);
        frustumCorners.SetRow(1, topRight);
        frustumCorners.SetRow(2, bottomRight);
        frustumCorners.SetRow(3, bottomLeft);

        var camPos = camtr.position;
        float FdotC = camPos.y - height;
        float paramK = (FdotC <= 0.0f ? 1.0f : 0.0f);
        fogMaterial.SetMatrix("_FrustumCornersWS", frustumCorners);
        fogMaterial.SetVector("_CameraWS", camPos);
        fogMaterial.SetVector("_HeightParams", new Vector4(height, FdotC, paramK, heightDensity * 0.5f));
        fogMaterial.SetVector("_DistanceParams", new Vector4(-Mathf.Max(startDistance, 0.0f), 0, 0, 0));



        Vector4 sceneParams;
        bool linear = (fogMode == FogMode.Linear);
        float diff = linear ? fogEnd - fogStart : 0.0f;
        float invDiff = Mathf.Abs(diff) > 0.0001f ? 1.0f / diff : 0.0f;
        sceneParams.x = fogDensity * 1.2011224087f; // density / sqrt(ln(2)), used by Exp2 fog mode
        sceneParams.y = fogDensity * 1.4426950408f; // density / ln(2), used by Exp fog mode
        sceneParams.z = linear ? -invDiff : 0.0f;
        sceneParams.w = linear ? fogEnd * invDiff : 0.0f;
        fogMaterial.SetVector("_SceneFogParams", sceneParams);
        fogMaterial.SetVector("_SceneFogMode", new Vector4((int)fogMode, useRadialDistance ? 1 : 0, 0, 0));

        int pass = 0;
        if (distanceFog && heightFog)
            pass = 0; // distance + height
        else if (distanceFog)
            pass = 4; // distance only
        else
            pass = 8; // height only

        //+0 : solid color, +1 : cubemap, +2 : gradient, +3 : gradient texture
        pass += Mathf.Clamp((int)coloringMode, 0, 3);

        //set colors
        fogMaterial.SetTexture("_Cube", fogCube);

        Matrix4x4 colors = Matrix4x4.identity;
        if (coloringMode == ColoringMode.Solid)
            colors.SetRow(0, solidColor);
        else
            colors.SetRow(0, skyColor);
        colors.SetRow(1, equatorColor);
        colors.SetRow(2, groundColor);

        fogMaterial.SetMatrix("_Colors", colors);
        //fogMaterial.SetInt("_ColorMode", (int)coloringMode);
        fogMaterial.SetInt("_UseCustomDepth", System.Convert.ToInt32(useCustomDepthTexture));
        if (coloringMode == ColoringMode.GradientTexture)
            fogMaterial.SetTexture("_Gradient", gradientTexture);
        else if (coloringMode == ColoringMode.Gradient)
        {
            if (tmpGradientTexture == null)
            {
                ApplyGradientChanges();
            }
            fogMaterial.SetTexture("_Gradient", tmpGradientTexture);
        }
        CustomGraphicsBlit(source, destination, fogMaterial, pass);
    }

    //used to pass indices as z val of quad.(this is then corrected after indices has been read in shader)
    static void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material fxMaterial, int passNr)
    {
        RenderTexture.active = dest;

        fxMaterial.SetTexture("_MainTex", source);

        GL.PushMatrix();
        GL.LoadOrtho();

        fxMaterial.SetPass(passNr);

        GL.Begin(GL.QUADS);

        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f); // TL

        GL.End();
        GL.PopMatrix();
    }

    //recreates gradient texture from ColorFulFog.gradient
    //should be called when changes to the gradient has been done.
    public void ApplyGradientChanges()
    {
        //if(Application.isPlaying)
        DestroyImmediate(tmpGradientTexture);
        tmpGradientTexture = null;
        tmpGradientTexture = GetGradientTexture(gradient, gradientResolution);
        tmpGradientTexture.hideFlags = HideFlags.HideAndDontSave;
    }
    public void NullTmpGradTex()
    {
        DestroyImmediate(tmpGradientTexture);
        tmpGradientTexture = null;
    }
    private Texture2D GetGradientTexture(Gradient gradient, int resolution = 10)
    {
        Texture2D result = new Texture2D(resolution, 1, TextureFormat.RGB24, false);
        Color[] colors = new Color[resolution];
        for (int i = 0; i < colors.Length; i++)
        {
            float eval = Mathf.Clamp((float)i / (float)resolution, 0f, 1f);
            colors[i] = gradient.Evaluate(eval);
        }
        result.SetPixels(colors);
        result.wrapMode = TextureWrapMode.Clamp;
        result.Apply(false);
        return result;
    }
}
