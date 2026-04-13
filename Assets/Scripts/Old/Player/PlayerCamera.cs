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
    [SerializeField] float smoothSpeed = 5f;

    float cameraXRotation;
    public Camera cam;
    Vector2 lookInput;
    AudioListener audioListener;

    public bool lockOveride = false;
    private bool isCameraLocked;
    private Quaternion targetRotation;
    float yaw;

    void Awake()
    {
        isCameraLocked = lockOveride;

        if (cam == null)
            cam = GetComponentInChildren<Camera>(true);

        if (audioListener == null)
            audioListener = GetComponentInChildren<AudioListener>(true);

        if (cameraPivot == null)
            cameraPivot = transform;

        targetRotation = cameraPivot.localRotation;
        SetCameraActive(false);
    }

    public override void OnNetworkSpawn()
    {
        enabled = IsOwner;
        SetCameraActive(IsOwner);

        if (!IsOwner)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraPivot == null)
            cameraPivot = transform;

        targetRotation = cameraPivot.localRotation;
    }

    public override void OnNetworkDespawn()
    {
        SetCameraActive(false);
    }

    void Update()
    {
        HandleLook();
        SmoothRotate();
    }

    void HandleLook()
    {
        if (isCameraLocked) return;

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        yaw += mouseX;
        cameraXRotation -= mouseY;
        cameraXRotation = Mathf.Clamp(cameraXRotation, -verticalClamp, verticalClamp);
        cameraPivot.localRotation = Quaternion.Euler(cameraXRotation, yaw, 0);
        targetRotation = cameraPivot.localRotation;
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

    void SetCameraActive(bool isActive)
    {
        if (cam != null)
            cam.enabled = isActive;

        if (audioListener != null)
            audioListener.enabled = isActive;
    }
}
