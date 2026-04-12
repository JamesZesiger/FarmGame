using UnityEngine;
using UnityEngine.UI;

public class ItemTransferHandler : MonoBehaviour
{
    public static ItemTransferHandler Instance { get; private set; }

    [Header("Selection Visual (optional)")]
    public Image selectionHighlight; // Assign a highlight Image in inspector, or leave null

    private Inventory _sourceInventory;
    private InventorySlot _selectedSlot;
    private InventoryUI _sourceUI;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void OnSlotClicked(Inventory clickedInventory, InventorySlot clickedSlot, InventoryUI sourceUI)
    {
        // --- Case 1: Nothing selected yet ---
        if (_selectedSlot == null)
        {
            if (clickedSlot == null || clickedSlot.item == null) return; // Clicked empty slot

            _sourceInventory = clickedInventory;
            _selectedSlot = clickedSlot;
            _sourceUI = sourceUI;
            CursorItemPreview.Instance?.Show(clickedSlot.item.icon);
            SetHighlight(sourceUI, clickedSlot);
            return;
        }

        // --- Case 2: Clicking the same slot again → deselect ---
        if (_selectedSlot == clickedSlot)
        {
            ClearSelection();
            return;
        }

        // --- Case 3: Clicking a slot in the OTHER inventory → transfer ---
        if (clickedInventory != _sourceInventory)
        {
            _sourceInventory.TransferTo(clickedInventory, _selectedSlot);
            ClearSelection();

            // Refresh both UIs via UIManager
            UIManager uiManager = sourceUI.uiManager != null ? sourceUI.uiManager : UIManager.Instance;
            if (uiManager != null) uiManager.RefreshAll();
            return;
        }

        // --- Case 4: Clicking a different slot in the SAME inventory → re-select ---
        if (clickedSlot != null && clickedSlot.item != null)
        {
            _sourceInventory = clickedInventory;
            _selectedSlot = clickedSlot;
            _sourceUI = sourceUI;
            SetHighlight(sourceUI, clickedSlot);
        }
        else
        {
            ClearSelection(); // Clicked empty slot in same inventory
        }
    }

    public void ClearSelection()
    {
        _selectedSlot = null;
        _sourceInventory = null;
        _sourceUI = null;
        CursorItemPreview.Instance?.Hide();

        if (selectionHighlight != null)
            selectionHighlight.gameObject.SetActive(false);
    }

    private void SetHighlight(InventoryUI ui, InventorySlot slot)
    {
        // Optional: move a highlight image to sit over the selected slot
        // You can implement this per your UI setup
    }

    public bool IsSelected(InventorySlot slot) => _selectedSlot == slot;
}
