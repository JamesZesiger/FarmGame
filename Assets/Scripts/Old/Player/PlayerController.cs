using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : NetworkBehaviour
{
    public static readonly List<PlayerController> ActivePlayers = new();
    public static PlayerController LocalPlayer { get; private set; }

    [Header("References")]
    public Animator animator;
    [SerializeField] Transform model;
    [SerializeField] Transform cameraTransform;
    [SerializeField] PlayerCamera playerCamera;
    public FarmInteraction farmInteraction;
    [SerializeField] ToolManager toolManager;
    [SerializeField] Inventory playerInventory;
    [SerializeField] Wallet playerWallet;
    [SerializeField] UIManager uiManager;

    [Header("Movement")]
    [SerializeField] float walkSpeed = 6f;
    [SerializeField] float sprintSpeed = 9f;
    [SerializeField] float acceleration = 14f;
    [SerializeField] float rotationSpeed = 10f;

    [Header("Jumping")]
    [SerializeField] float jumpHeight = 1.6f;
    [SerializeField] float gravity = -20f;

    [Header("Footstep Particles")]
    [SerializeField] GameObject footstepParticlePrefab;
    [SerializeField] Transform footPoint;
    [SerializeField] float spawnRate = 0.15f;

    CharacterController controller;
    PlayerInput playerInput;

    Vector2 moveInput;
    Vector3 velocity;
    float verticalVelocity;

    bool isSprinting;
    bool isGrounded;

    float nextSpawnTime;
    float syncedSpeed;
    bool syncedIsGrounded;
    float syncedVerticalVelocity;
    int lastJumpSerial;

    bool isSceneReady;

    readonly NetworkVariable<float> networkSpeed = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<bool> networkIsGrounded = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<float> networkVerticalVelocity = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<Quaternion> networkModelRotation = new(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<int> networkJumpSerial = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public Inventory PlayerInventory => playerInventory;
    public Wallet PlayerWallet => playerWallet;
    public Camera PlayerCamera => playerCamera != null ? playerCamera.cam : cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
    public ulong PlayerId => IsSpawned ? OwnerClientId : ulong.MaxValue;
    public UIManager UIManager => uiManager;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.enabled = false;
        playerInput = GetComponent<PlayerInput>();

        if (farmInteraction == null)
            farmInteraction = GetComponent<FarmInteraction>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<PlayerCamera>(true);

        if (uiManager == null)
            uiManager = GetComponentInChildren<UIManager>(true);

        if (cameraTransform == null && playerCamera != null && playerCamera.cam != null)
            cameraTransform = playerCamera.cam.transform;

        if (playerCamera != null)
            playerCamera.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        controller.enabled = false;

        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;

        if (!ActivePlayers.Contains(this))
            ActivePlayers.Add(this);

        if (uiManager != null)
            uiManager.Initialize(this);

        if (playerInput != null)
            playerInput.enabled = IsOwner;

        syncedSpeed = networkSpeed.Value;
        syncedIsGrounded = networkIsGrounded.Value;
        syncedVerticalVelocity = networkVerticalVelocity.Value;
        lastJumpSerial = networkJumpSerial.Value;

        if (!IsOwner)
        {
            if (playerCamera != null)
                playerCamera.enabled = false;
            return;
        }

        if (playerCamera != null)
            playerCamera.enabled = true;

        LocalPlayer = this;

        if (cameraTransform == null && playerCamera != null && playerCamera.cam != null)
            cameraTransform = playerCamera.cam.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnSceneLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsOwner) return;

        isSceneReady = true;
        controller.enabled = true;
        verticalVelocity = -2f;
    }

    public override void OnNetworkDespawn()
    {
        ActivePlayers.Remove(this);

        if (LocalPlayer == this)
            LocalPlayer = null;
    }

    void OnDestroy()
    {
        ActivePlayers.Remove(this);

        if (LocalPlayer == this)
            LocalPlayer = null;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
    }

    void Update()
    {
        if (!isSceneReady) return;

        if (IsOwner)
        {
            GroundCheck();
            HandleMovement();
            ApplyGravity();
            SyncAnimationState();
            ApplyAnimationState(velocity.magnitude / sprintSpeed, isGrounded, verticalVelocity);
            HandleFootsteps();
            return;
        }

        ApplyRemoteAnimationState();
    }

    void HandleMovement()
    {
        if (cameraTransform == null)
            return;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;
        float speed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = moveDir * speed;

        velocity = Vector3.Lerp(velocity, targetVelocity, acceleration * Time.deltaTime);

        controller.Move(velocity * Time.deltaTime);

        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);

            model.rotation = Quaternion.Slerp(model.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void ApplyGravity()
    {
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 gravityMove = Vector3.up * verticalVelocity;
        controller.Move(gravityMove * Time.deltaTime);
    }

    void GroundCheck()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;
    }

    void Jump()
    {
        if (!isGrounded) return;

        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        networkJumpSerial.Value++;
        animator.SetTrigger("jump");
    }

    void SyncAnimationState()
    {
        networkSpeed.Value = velocity.magnitude / sprintSpeed;
        networkIsGrounded.Value = isGrounded;
        networkVerticalVelocity.Value = verticalVelocity;
        if (model != null)
            networkModelRotation.Value = model.rotation;
    }

    void ApplyAnimationState(float speedPercent, bool grounded, float yVelocity)
    {
        animator.SetFloat("speed", speedPercent, 0.1f, Time.deltaTime);
        animator.SetBool("isGrounded", grounded);
        animator.SetFloat("yVelocity", yVelocity);
    }

    void ApplyRemoteAnimationState()
    {
        syncedSpeed = networkSpeed.Value;
        syncedIsGrounded = networkIsGrounded.Value;
        syncedVerticalVelocity = networkVerticalVelocity.Value;

        if (model != null)
            model.rotation = networkModelRotation.Value;

        ApplyAnimationState(syncedSpeed, syncedIsGrounded, syncedVerticalVelocity);

        int jumpSerial = networkJumpSerial.Value;
        if (jumpSerial != lastJumpSerial)
        {
            lastJumpSerial = jumpSerial;
            animator.SetTrigger("jump");
        }
    }

    void HandleFootsteps()
    {
        if (isGrounded && moveInput.magnitude > 0.1f)
        {
            if (Time.time >= nextSpawnTime)
            {
                GameObject particle = Instantiate(footstepParticlePrefab, footPoint.position, Quaternion.identity);
                Destroy(particle, 0.5f);
                nextSpawnTime = Time.time + spawnRate;
            }
        }
    }

    void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        moveInput = value.Get<Vector2>();
    }

    void OnSprint(InputValue value)
    {
        if (!IsOwner) return;
        isSprinting = value.isPressed;
    }

    void OnJump(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
            Jump();
    }

    void OnUseTool(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        toolManager.OnUse();
    }

    void OnNext(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        toolManager.NextTool();
    }

    void OnInventory(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        uiManager?.TogglePlayerInventory();
    }

    void OnAltUseTool(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        toolManager.AltUse();
    }

    void OnAlt(InputValue value)
    {
        if (!IsOwner) return;

        if (!Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
