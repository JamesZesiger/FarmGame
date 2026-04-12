using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public InventoryUI playerUI;
    public InventoryUI containerUI;

    private readonly Dictionary<ulong, PlayerController> playersById = new();
    private PlayerController currentPlayer;
    private ulong currentPlayerId = ulong.MaxValue;
    public Inventory currentPlayerInventory;
    public Inventory currentContainerInventory;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (playerUI != null)
        {
            playerUI.uiManager = this;
            playerUI.gameObject.SetActive(false);
        }

        if (containerUI != null)
        {
            containerUI.uiManager = this;
            containerUI.gameObject.SetActive(false);
        }

        IsOpen = false;
        DiscoverPlayersOnLoad();
    }

    void OnEnable()
    {
        PlayerController.PlayerRegistered += RegisterPlayer;
        PlayerController.PlayerUnregistered += UnregisterPlayer;
        PlayerController.InventoryRequested += HandleInventoryRequested;
        DiscoverPlayersOnLoad();
    }

    void OnDisable()
    {
        PlayerController.PlayerRegistered -= RegisterPlayer;
        PlayerController.PlayerUnregistered -= UnregisterPlayer;
        PlayerController.InventoryRequested -= HandleInventoryRequested;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public PlayerController ResolveCurrentPlayer()
    {
        DiscoverPlayersOnLoad();

        if (currentPlayer != null &&
            currentPlayerId != ulong.MaxValue &&
            playersById.TryGetValue(currentPlayerId, out PlayerController trackedCurrent) &&
            trackedCurrent == currentPlayer)
            return currentPlayer;

        currentPlayer = null;
        currentPlayerId = ulong.MaxValue;

        if (PlayerController.LocalPlayer != null)
        {
            return SetCurrentPlayer(PlayerController.LocalPlayer);
        }

        return currentPlayer;
    }

    public bool TryGetPlayer(ulong playerId, out PlayerController player)
    {
        DiscoverPlayersOnLoad();
        return playersById.TryGetValue(playerId, out player);
    }

    void DiscoverPlayersOnLoad()
    {
        for (int i = 0; i < PlayerController.ActivePlayers.Count; i++)
        {
            RegisterPlayer(PlayerController.ActivePlayers[i]);
        }
    }

    void RegisterPlayer(PlayerController player)
    {
        if (player == null || !player.IsSpawned)
            return;

        playersById[player.PlayerId] = player;

        if (player.IsOwner || player == PlayerController.LocalPlayer)
            SetCurrentPlayer(player);
    }

    void UnregisterPlayer(PlayerController player)
    {
        if (player == null)
            return;

        playersById.Remove(player.PlayerId);

        if (currentPlayer == player)
        {
            currentPlayer = null;
            currentPlayerId = ulong.MaxValue;
            currentPlayerInventory = null;
        }
    }

    PlayerController SetCurrentPlayer(PlayerController player)
    {
        if (player == null)
            return null;

        currentPlayer = player;
        currentPlayerId = player.PlayerId;
        currentPlayerInventory = player.PlayerInventory;
        return currentPlayer;
    }

    void HandleInventoryRequested(PlayerController player)
    {
        if (player == null) return;
        RegisterPlayer(player);
        if (player != ResolveCurrentPlayer() && player != PlayerController.LocalPlayer) return;

        SetCurrentPlayer(player);
        TogglePlayerInventory();
    }

    public void OpenContainerForPlayer(PlayerInteraction playerInteraction, Inventory containerInventory)
    {
        if (playerInteraction == null) return;

        PlayerController player = playerInteraction.Controller != null
            ? playerInteraction.Controller
            : ResolveCurrentPlayer();

        if (player == null) return;

        SetCurrentPlayer(player);
        OpenContainer(player.PlayerInventory, containerInventory);
    }

    public void TogglePlayerInventory()
    {
        if (IsOpen)
        {
            CloseAll();
        }
        else
        {
            OpenPlayerInventory();
        }
    }

    public void OpenPlayerInventory()
    {
        PlayerController player = ResolveCurrentPlayer();
        Inventory playerInventory = player != null ? player.PlayerInventory : currentPlayerInventory;
        if (playerInventory == null) return;

        if (player != null)
            SetCurrentPlayer(player);
        else
            currentPlayerInventory = playerInventory;
        currentContainerInventory = null;

        if (playerUI != null)
        {
            playerUI.Init(playerInventory);
            playerUI.gameObject.SetActive(true);
        }

        if (containerUI != null)
            containerUI.gameObject.SetActive(false);

        SetCursor(true);
        IsOpen = true;
    }

    public void OpenContainer(Inventory playerInventory, Inventory containerInventory)
    {
        if (playerInventory == null || containerInventory == null) return;

        PlayerController player = ResolveCurrentPlayer();
        if (player != null)
            SetCurrentPlayer(player);
        else
            currentPlayerInventory = playerInventory;
        currentContainerInventory = containerInventory;

        if (playerUI != null)
        {
            playerUI.Init(playerInventory);
            playerUI.gameObject.SetActive(true);
        }

        if (containerUI != null)
        {
            containerUI.Init(containerInventory);
            containerUI.gameObject.SetActive(true);
        }

        SetCursor(true);
        IsOpen = true;
    }

    public void CloseAll()
    {
        ItemTransferHandler.Instance?.ClearSelection();

        if (playerUI != null)
            playerUI.gameObject.SetActive(false);

        if (containerUI != null)
            containerUI.gameObject.SetActive(false);

        currentContainerInventory = null;

        SetCursor(false);
        IsOpen = false;
    }

    void SetCursor(bool state)
    {
        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = state;
    }

    public Inventory GetOtherInventory(Inventory source)
    {
        if (source == currentPlayerInventory)
            return currentContainerInventory;

        if (source == currentContainerInventory)
            return currentPlayerInventory;

        return null;
    }

    public void RefreshAll()
    {
        if (playerUI != null && playerUI.gameObject.activeSelf)
            playerUI.UpdateUI();

        if (containerUI != null && containerUI.gameObject.activeSelf)
            containerUI.UpdateUI();
    }
}
