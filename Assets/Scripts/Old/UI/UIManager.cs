using UnityEngine;

public class UIManager : MonoBehaviour
{
    public InventoryUI playerUI;
    public InventoryUI containerUI;

    private PlayerController ownerPlayer;
    private ItemTransferHandler transferHandler;

    public Inventory currentPlayerInventory { get; private set; }
    public Inventory currentContainerInventory { get; private set; }
    public bool IsOpen { get; private set; }
    public PlayerController OwnerPlayer => ownerPlayer;
    public ItemTransferHandler TransferHandler => transferHandler;

    void Awake()
    {
        transferHandler = GetComponent<ItemTransferHandler>();

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
    }

    public void Initialize(PlayerController player)
    {
        ownerPlayer = player;
        currentPlayerInventory = ownerPlayer != null ? ownerPlayer.PlayerInventory : null;
        currentContainerInventory = null;

        bool isLocalOwner = ownerPlayer != null && ownerPlayer.IsOwner;
        enabled = isLocalOwner;

        if (!isLocalOwner)
        {
            if (playerUI != null)
                playerUI.gameObject.SetActive(false);

            if (containerUI != null)
                containerUI.gameObject.SetActive(false);

            IsOpen = false;
            return;
        }

        if (playerUI != null)
            playerUI.uiManager = this;

        if (containerUI != null)
            containerUI.uiManager = this;
    }

    public void OpenContainerForPlayer(PlayerInteraction playerInteraction, Inventory containerInventory)
    {
        if (ownerPlayer == null || !ownerPlayer.IsOwner || playerInteraction == null)
            return;

        if (playerInteraction.Controller != ownerPlayer)
            return;

        OpenContainer(ownerPlayer.PlayerInventory, containerInventory);
    }

    public void TogglePlayerInventory()
    {
        if (ownerPlayer == null || !ownerPlayer.IsOwner)
            return;

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
        if (ownerPlayer == null || !ownerPlayer.IsOwner)
            return;

        Inventory playerInventory = ownerPlayer.PlayerInventory;
        if (playerInventory == null)
            return;

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
        if (ownerPlayer == null || !ownerPlayer.IsOwner)
            return;

        if (playerInventory == null || containerInventory == null)
            return;

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
        transferHandler?.ClearSelection();

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
