using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : NetworkBehaviour
{
    public static readonly System.Collections.Generic.List<PlayerController> ActivePlayers = new();
    public static PlayerController LocalPlayer { get; private set; }
    public static event System.Action<PlayerController> PlayerRegistered;
    public static event System.Action<PlayerController> PlayerUnregistered;
    public static event System.Action<PlayerController> InventoryRequested;

    [Header("References")]
    public Animator animator;
    [SerializeField] Transform model; // visual mesh
    [SerializeField] Transform cameraTransform;
    [SerializeField] PlayerCamera playerCamera;
    public FarmInteraction farmInteraction;
    [SerializeField] ToolManager toolManager;
    [SerializeField] Inventory playerInventory;
    [SerializeField] Wallet playerWallet;

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

    public Inventory PlayerInventory => playerInventory;
    public Wallet PlayerWallet => playerWallet;
    public Camera PlayerCamera => playerCamera != null ? playerCamera.cam : cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
    public ulong PlayerId => IsSpawned ? OwnerClientId : ulong.MaxValue;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (farmInteraction == null)
            farmInteraction = GetComponent<FarmInteraction>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<PlayerCamera>();

        if (playerCamera != null)
            playerCamera.enabled = false;

    }

    public override void OnNetworkSpawn()
    {
        if (!ActivePlayers.Contains(this))
        {
            ActivePlayers.Add(this);
            PlayerRegistered?.Invoke(this);
        }

        if (playerInput != null)
            playerInput.enabled = IsOwner;

        if (!IsOwner)
        {
            enabled = false;
            if (playerCamera != null)
                playerCamera.enabled = false;
            return;
        }

        if (playerCamera != null)
            playerCamera.enabled = true;

        if (IsOwner)
        {
            LocalPlayer = this;

            Debug.Log($"LocalPlayer set. Camera = {PlayerCamera}");
        }

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnNetworkDespawn()
    {
        if (ActivePlayers.Remove(this))
            PlayerUnregistered?.Invoke(this);

        if (LocalPlayer == this)
            LocalPlayer = null;
    }

    void OnDestroy()
    {
        if (ActivePlayers.Remove(this))
            PlayerUnregistered?.Invoke(this);

        if (LocalPlayer == this)
            LocalPlayer = null;
    }

    void Update()
    {
        if (!IsOwner) return;
        GroundCheck();
        HandleMovement();
        ApplyGravity();
        HandleAnimations();
        HandleFootsteps();
    }

    // ---------------- MOVEMENT ----------------

    void HandleMovement()
    {
        // Camera-relative movement
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 targetVelocity = moveDir * speed;

        velocity = Vector3.Lerp(
            velocity,
            targetVelocity,
            acceleration * Time.deltaTime
        );

        controller.Move(velocity * Time.deltaTime);

        // Smooth model rotation
        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);

            model.rotation = Quaternion.Slerp(
                model.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    // ---------------- GRAVITY ----------------

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
        {
            verticalVelocity = -2f;
        }
    }

    // ---------------- JUMP ----------------

    void Jump()
    {
        if (!isGrounded) return;

        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        animator.SetTrigger("jump");
    }

    // ---------------- ANIMATIONS ----------------

    void HandleAnimations()
    {
        float speedPercent = velocity.magnitude / sprintSpeed;

        animator.SetFloat("speed", speedPercent, 0.1f, Time.deltaTime);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("yVelocity", verticalVelocity);
    }

    // ---------------- FOOTSTEPS ----------------

    void HandleFootsteps()
    {
        if (isGrounded && moveInput.magnitude > 0.1f)
        {
            if (Time.time >= nextSpawnTime)
            {
                GameObject particle = Instantiate(
                    footstepParticlePrefab,
                    footPoint.position,
                    Quaternion.identity
                );

                Destroy(particle, 0.5f);
                nextSpawnTime = Time.time + spawnRate;
            }
        }
    }

    // ---------------- INPUT ----------------

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
        InventoryRequested?.Invoke(this);
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
