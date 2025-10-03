using UnityEngine;
using UnityEngine.InputSystem; // <-- new system


public class CameraOnKey : MonoBehaviour
{

    public Camera cam;
    public RenderTexture target;

    // Inline action: Space by default. You can rebind in code or inspector.
    [SerializeField] private InputAction renderAction =
        new InputAction("Render", InputActionType.Button, "<Keyboard>/space");

    void OnEnable()
    {
        if (!cam) cam = Camera.main;
        if (cam) cam.enabled = false;
        if (cam && target) cam.targetTexture = target;

        renderAction.Enable();
        renderAction.performed += OnRenderPerformed;
    }

    void OnDisable()
    {
        renderAction.performed -= OnRenderPerformed;
        renderAction.Disable();
    }

    private void OnRenderPerformed(InputAction.CallbackContext _)
    {
        // Render exactly one frame on demand
        if (cam) cam.Render();
    }
}
