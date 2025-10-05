using UnityEngine;
using static UnityEngine.Mathf;
using UnityEngine.Rendering.HighDefinition;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

[ExecuteAlways]
public class Fog : MonoBehaviour
{
    [Range(0, 1)]
    public float density;
    public float step_size;
    public int num_steps;
    public int shadow_steps;
    public float light_intensity;
    public float exp_factor;
    public float noise_factor;
    public Vector3 light_loc;
    RenderTexture outputTexture;
    public ComputeShader shader;

    public float interpolation_factor;
    public Color colour_max;
    public Color colour_min;

    public Texture2D blueNoise;

    private float time = 0.0f;

    Camera cam;


    int kernel;

    private static Vector2 GetRandomDirection()
	{
		return new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized;
	}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.current;
        InitParameters();
        kernel = shader.FindKernel("CSMain");


        shader.SetTexture(kernel, "BlueNoise", blueNoise);

        // ComputeBuffer gradients = new ComputeBuffer(256, sizeof(float) * 2);
		// gradients.SetData(Enumerable.Range(0, 256).Select((i) => GetRandomDirection()).ToArray());
        // shader.SetBuffer(kernel, "gradients", gradients);
    }

    // Update is called once per frame
    void Update()
    {

        
    }

    void OnRenderImage(RenderTexture src, RenderTexture destination) {
        cam = Camera.current;
        bool isSceneCam = cam.name == "SceneCamera";

        if (! isSceneCam){
            UpdateCameraParams(cam);
            InitRenderTexture (ref outputTexture);
            InitParameters();
            time += Time.deltaTime;
            shader.SetFloat("time", time);


            shader.SetTexture(kernel, "Result", outputTexture);

            int threadGroupsX = Mathf.CeilToInt (cam.pixelWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt (cam.pixelHeight / 8.0f);
            shader.Dispatch (kernel, threadGroupsX, threadGroupsY, 1);

            Graphics.Blit (outputTexture, destination);

        }

    }

    void InitRenderTexture (ref RenderTexture tex) {
        if (tex == null || tex.width != cam.pixelWidth || tex.height != cam.pixelHeight) {
            if (tex != null) {
                tex.Release ();
            }
            tex = new RenderTexture (cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            tex.enableRandomWrite = true;
            tex.Create ();
        }
    }

    private void InitParameters() {
        shader.SetInt("num_steps", num_steps);
        shader.SetFloat("step_size", step_size);
        shader.SetFloat("cloud_density", density);
        shader.SetVector("light_loc", light_loc);
        shader.SetFloat("light_intensity", light_intensity);
        shader.SetFloat("exp_factor", exp_factor);
        shader.SetInt("shadow_steps",shadow_steps);
        shader.SetFloat("noise_factor",noise_factor);
        shader.SetVector("colour_min", colour_min);
        shader.SetVector("colour_max", colour_max);
        shader.SetFloat("interpolation_factor", interpolation_factor);

    }

    void UpdateCameraParams(Camera camera)
	{
		float planeHeight = 1 * Tan(camera.fieldOfView * 0.5f * Deg2Rad) * 2;
		float planeWidth = planeHeight * camera.aspect;
		// Send data to shader
		shader.SetVector("_CamPlane", new Vector3(planeWidth, planeHeight, 1));
		shader.SetMatrix("_CamToWorldMatrix", camera.transform.localToWorldMatrix);
	}
}
