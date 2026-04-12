using UnityEngine;

public class UIManager : MonoBehaviour
{
    public InventoryUI playerUI;
    public InventoryUI containerUI;

    private PlayerController currentPlayer;
    public Inventory currentPlayerInventory;
    public Inventory currentContainerInventory;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (playerUI != null)
            playerUI.gameObject.SetActive(false);

        if (containerUI != null)
            containerUI.gameObject.SetActive(false);

        IsOpen = false;
    }

    public void BindPlayer(PlayerController player)
    {
        currentPlayer = player;
        currentPlayerInventory = player != null ? player.PlayerInventory : null;
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
        Inventory playerInventory = currentPlayer != null ? currentPlayer.PlayerInventory : currentPlayerInventory;
        if (playerInventory == null) return;

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

        currentPlayer = currentPlayer == null ? PlayerController.LocalPlayer : currentPlayer;
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
