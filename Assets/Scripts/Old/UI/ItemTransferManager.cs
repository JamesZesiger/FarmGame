using UnityEngine;
using UnityEngine.UI;

public class ItemTransferHandler : MonoBehaviour
{
    public Image selectionHighlight;

    private Inventory _sourceInventory;
    private InventorySlot _selectedSlot;
    private InventoryUI _sourceUI;
    private CursorItemPreview cursorItemPreview;

    void Awake()
    {
        cursorItemPreview = GetComponentInChildren<CursorItemPreview>(true);
    }

    public void OnSlotClicked(Inventory clickedInventory, InventorySlot clickedSlot, InventoryUI sourceUI)
    {
        if (_selectedSlot == null)
        {
            if (clickedSlot == null || clickedSlot.item == null) return;

            _sourceInventory = clickedInventory;
            _selectedSlot = clickedSlot;
            _sourceUI = sourceUI;
            cursorItemPreview?.Show(clickedSlot.item.icon);
            SetHighlight(sourceUI, clickedSlot);
            return;
        }

        if (_selectedSlot == clickedSlot)
        {
            ClearSelection();
            return;
        }

        if (clickedInventory != _sourceInventory)
        {
            _sourceInventory.TransferTo(clickedInventory, _selectedSlot);
            ClearSelection();

            UIManager uiManager = sourceUI.uiManager;
            if (uiManager != null)
                uiManager.RefreshAll();
            return;
        }

        if (clickedSlot != null && clickedSlot.item != null)
        {
            _sourceInventory = clickedInventory;
            _selectedSlot = clickedSlot;
            _sourceUI = sourceUI;
            SetHighlight(sourceUI, clickedSlot);
        }
        else
        {
            ClearSelection();
        }
    }

    public void ClearSelection()
    {
        _selectedSlot = null;
        _sourceInventory = null;
        _sourceUI = null;
        cursorItemPreview?.Hide();

        if (selectionHighlight != null)
            selectionHighlight.gameObject.SetActive(false);
    }

    private void SetHighlight(InventoryUI ui, InventorySlot slot)
    {
    }

    public bool IsSelected(InventorySlot slot) => _selectedSlot == slot;
}
