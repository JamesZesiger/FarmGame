using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] Transform cameraPivot;

    [Header("Mouse Look")]
    [SerializeField] float mouseSensitivity = 0.12f;
    [SerializeField] float verticalClamp = 80f;
    [SerializeField] float smoothSpeed = 5f; // rotation smoothing speed

    float cameraXRotation;
    public Camera cam;
    Vector2 lookInput;

    public bool lockOveride = false;
    private bool isCameraLocked;

    private Quaternion targetRotation; // smooth rotation target

    void Awake()
    {
        isCameraLocked = lockOveride;
        cam = Camera.main ?? cam;

        if (cameraPivot == null)
            cameraPivot = transform;

        targetRotation = cameraPivot.localRotation;
    }

    public override void OnNetworkSpawn()
    {
        enabled = IsOwner;
        if (!IsOwner)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraPivot == null)
            cameraPivot = transform;

        targetRotation = cameraPivot.localRotation;
    }

    void Update()
    {
        HandleLook();
        SmoothRotate();
    }

    float yaw; // horizontal rotation

    void HandleLook()
    {
        if (isCameraLocked) return;

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        // Horizontal rotation (orbit around player)
        yaw += mouseX;

        // Vertical rotation (up/down)
        cameraXRotation -= mouseY;
        cameraXRotation = Mathf.Clamp(cameraXRotation, -verticalClamp, verticalClamp);

        // Apply rotations
        cameraPivot.localRotation = Quaternion.Euler(cameraXRotation, yaw, 0);
        targetRotation = cameraPivot.localRotation; // update target so smooth rotation doesn't fight mouse
    }

    void SmoothRotate()
    {
        if (cameraPivot.localRotation != targetRotation)
        {
            cameraPivot.localRotation = Quaternion.RotateTowards(
                cameraPivot.localRotation,
                targetRotation,
                smoothSpeed * 100f * Time.deltaTime
            );
        }
    }

    void OnLook(InputValue value)
    {
        if (!IsOwner) return;
        lookInput = value.Get<Vector2>();
    }

    public void ToggleCameraLock(bool lockState)
    {
        if (lockOveride) return;
        isCameraLocked = lockState;
    }

    public void OnCameraRight(InputValue value)
    {
        if (!IsOwner || !lockOveride) return;
        targetRotation *= Quaternion.Euler(0, 90f, 0);
    }

    public void OnCameraLeft(InputValue value)
    {
        if (!IsOwner || !lockOveride) return;
        targetRotation *= Quaternion.Euler(0, -90f, 0);
    }
}