using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteAlways]
public class RayMarchHelper : MonoBehaviour
{

    public float mass;
    public Vector2 ring_size;
    public int num_steps;
    public float change_size;
    public Vector3 disk_normal;
    RenderTexture outputTexture;
    public ComputeShader shader;
    [SerializeField] Shader accumulateShader;

    Material accumulateMaterial;
    RenderTexture accumulateTexture;

    // Cloud

    public Vector3 light_loc;
    public int fog_step_size;
    public float interpolation_factor;
    public Color colour_max;
    public Color colour_min;
    public Texture2D blueNoise;

    Camera cam;

    // public const float speed_of_light = 299792458f;
    // public const float G = 0.000000000066743f;
    public const float speed_of_light = 1.0f;
    public const float G = 1.0f;

    int kernel;
    float frame = 0.0f;
    int int_frame = 0;

    bool render = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.current;
        InitParameters();
        kernel = shader.FindKernel("CSMain");

        shader.SetTexture(kernel, "BlueNoise", blueNoise);
    }

    // Update is called once per frame
    void Update()
    {

        
    }

    public void Render(){

        if (render == false){
            change_size = 0.005f;
            num_steps = 20000;
        } else {
            change_size = 0.1f;
            num_steps = 300;
        }

        frame = 0.0f;
        int_frame = 0;
        render = !render;
    }

    void OnRenderImage(RenderTexture src, RenderTexture destination) {
        cam = Camera.current;
        bool isSceneCam = cam.name == "SceneCamera";

        if (! isSceneCam){
            UpdateCameraParams(cam);
            InitRenderTexture (ref outputTexture);
            InitParameters();

            if (render){
                InitMaterial(accumulateShader, ref accumulateMaterial);
                InitRenderTexture(ref accumulateTexture);
                

                // Create copy of prev frame
                RenderTexture prevFrameCopy = RenderTexture.GetTemporary(src.width, src.height, 0);
                Graphics.Blit(outputTexture, prevFrameCopy);

                shader.SetFloat("frame", frame);

                // Run the ray march shader and draw the result to a temp texture
                shader.SetTexture(kernel, "Result", accumulateTexture);

                int threadGroupsX = Mathf.CeilToInt (cam.pixelWidth / 8.0f);
                int threadGroupsY = Mathf.CeilToInt (cam.pixelHeight / 8.0f);
                shader.Dispatch (0, threadGroupsX, threadGroupsY, 1);
        
                // Accumulate
                accumulateMaterial.SetInt("_Frames", int_frame);
                accumulateMaterial.SetTexture("_PrevFrame", prevFrameCopy);
                Graphics.Blit(accumulateTexture, outputTexture, accumulateMaterial);

                // Draw result to screen
                Graphics.Blit(outputTexture, destination);

                // Release temps
                RenderTexture.ReleaseTemporary(prevFrameCopy);

                int_frame += 1;
                frame+=Time.deltaTime;
            } else {
                shader.SetFloat("frame", frame);

                shader.SetTexture(kernel, "Result", outputTexture);

                int threadGroupsX = Mathf.CeilToInt (cam.pixelWidth / 8.0f);
                int threadGroupsY = Mathf.CeilToInt (cam.pixelHeight / 8.0f);
                shader.Dispatch (kernel, threadGroupsX, threadGroupsY, 1);

                Graphics.Blit (outputTexture, destination);

                frame+=Time.deltaTime;
                
            }

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

    public static void InitMaterial(Shader shader, ref Material mat)
    {
        if (mat == null || (mat.shader != shader && shader != null))
        {
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            mat = new Material(shader);
        }
    }

    private void InitParameters() {

        var r = (2 * G * mass) / (speed_of_light * speed_of_light);
        
        shader.SetFloat("change_size", change_size);
        shader.SetFloat("radius", r);
        shader.SetFloat("mass", mass);
        shader.SetVector("ring_size",ring_size);
        shader.SetInt("num_steps", num_steps);
        shader.SetVector("disk_normal", Vector3.Normalize(disk_normal));

        shader.SetVector("light_loc",light_loc);
        shader.SetInt("fog_step_size",fog_step_size);
        shader.SetFloat("interpolation_factor",interpolation_factor);
        shader.SetVector("colour_max", colour_max);
        shader.SetVector("colour_min", colour_min);

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
