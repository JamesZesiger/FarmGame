using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    public int index;
    public InventoryUI parentUI;

    [Header("Visuals")]
    public Image icon;
    public TextMeshProUGUI quantityText;
    public Image selectionBorder;

    public void SetSlot(InventorySlot slot)
    {
        bool hasItem = slot != null && slot.item != null;

        if (icon != null)
        {
            icon.sprite = hasItem ? slot.item.icon : null;
            icon.enabled = hasItem;
        }

        if (quantityText != null)
        {
            quantityText.text = (hasItem && slot.quantity > 1) ? slot.quantity.ToString() : "";
        }

        UpdateSelectionVisual(slot);
    }

    public void UpdateSelectionVisual(InventorySlot slot)
    {
        if (selectionBorder == null) return;
        ItemTransferHandler transferHandler = parentUI != null && parentUI.uiManager != null
            ? parentUI.uiManager.TransferHandler
            : null;
        bool selected = slot != null && transferHandler != null && transferHandler.IsSelected(slot);
        selectionBorder.enabled = selected;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentUI == null) return;

        bool shiftHeld = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        if (shiftHeld)
        {
            parentUI.HandleShiftClick(index);
        }
        else
        {
            parentUI.HandleClick(index);
        }
    }
}
