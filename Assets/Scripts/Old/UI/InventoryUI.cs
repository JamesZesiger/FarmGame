using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public GameObject slotPrefab;
    public GameObject inventoryPanel;
    public Transform gridParent;
    public UIManager uiManager;
    public Inventory currentInventory;
    public bool isPlayerInventory;

    private GameObject[] slots;
    private bool isOpen = false;

    public void Init(Inventory inventory)
    {
        currentInventory = inventory;

        // Rebuild slots if needed
        if (slots != null)
        {
            foreach (var s in slots)
                Destroy(s);
        }

        slots = new GameObject[inventory.size];

        for (int i = 0; i < inventory.size; i++)
        {
            GameObject slot = Instantiate(slotPrefab, gridParent);

            var slotUI = slot.GetComponent<InventorySlotUI>();
            slotUI.index = i;
            slotUI.parentUI = this;

            slots[i] = slot;
        }

        UpdateUI();
    }

    public void Awake()
    {
        this.gameObject.SetActive(isOpen);
    }

    public void UpdateUI()
    {
        if (currentInventory == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            var icon = slots[i].transform.GetChild(0).GetComponent<UnityEngine.UI.Image>();
            var text = slots[i].transform.GetChild(1).GetComponent<TMP_Text>();

            if (i < currentInventory.items.Count)
            {
                text.text = $"{currentInventory.items[i].quantity}";
                icon.sprite = currentInventory.items[i].item.icon;
                icon.enabled = true;
            }
            else
            {
                text.text = "";
                icon.enabled = false;
            }
        }
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);
        HandleCursor();
    }

    void HandleCursor()
    {
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;
    }

    public void HandleShiftClick(int index)
    {
        if (currentInventory == null) return;
        if (index >= currentInventory.items.Count) return;

        InventorySlot slot = currentInventory.items[index];

        Inventory target = isPlayerInventory
            ? uiManager.currentContainerInventory
            : uiManager.currentPlayerInventory;

        if (target == null) return;

        currentInventory.TransferTo(target, slot);

        uiManager.RefreshAll();
    }

    public void HandleClick(int index)
    {
        Debug.Log("click");
        if (currentInventory == null) return;
        Debug.Log("clicked");
        InventorySlot slot = index < currentInventory.items.Count ? currentInventory.items[index] : null;
        ItemTransferHandler.Instance.OnSlotClicked(currentInventory, slot, this);
    }
}