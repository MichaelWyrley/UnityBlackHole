using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteAlways]
public class RayMarchHelper : MonoBehaviour
{

    public float mass;
    public int num_steps;
    public float change_size;
    public Vector3 disk_normal;
    RenderTexture outputTexture;
    public ComputeShader shader;

    Camera cam;

    // public const float speed_of_light = 299792458f;
    // public const float G = 0.000000000066743f;
    public const float speed_of_light = 1.0f;
    public const float G = 1.0f;

    int kernel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.current;
        InitParameters();
        kernel = shader.FindKernel("CSMain");

        Screen.SetResolution(800, 600, false);
        
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

            // shader.SetFloat("dt", Time.deltaTime);


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

        var r = (2 * G * mass) / (speed_of_light * speed_of_light);
        shader.SetFloat("change_size", change_size);
        shader.SetFloat("radius", r);
        shader.SetFloat("mass", mass);
        shader.SetInt("num_steps", num_steps);
        shader.SetVector("disk_normal", Vector3.Normalize(disk_normal));
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
