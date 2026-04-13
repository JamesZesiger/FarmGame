using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public int size = 20;
    public List<InventorySlot> items = new List<InventorySlot>();

    public int AddItem(Item item, int amount = 1)
    {
        if (item == null) return amount;

        int remaining = amount;

        // 1. Fill existing stacks
        if (item.isStackable)
        {
            foreach (var slot in items)
            {
                if (slot.item == item && slot.quantity < item.maxStack)
                {
                    int space = item.maxStack - slot.quantity;
                    int toAdd = Mathf.Min(space, remaining);

                    slot.quantity += toAdd;
                    remaining -= toAdd;

                    if (remaining <= 0)
                        return 0;
                }
            }
        }

        // 2. Create new stacks
        while (remaining > 0 && items.Count < size)
        {
            int toAdd = item.isStackable
                ? Mathf.Min(item.maxStack, remaining)
                : 1;

            items.Add(new InventorySlot(item, toAdd));
            remaining -= toAdd;
        }

        return remaining; // leftover if inventory is full
    
    
    }

    public Item GetItemByName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return null;
        }

        InventorySlot slot = items.FirstOrDefault(slot => slot != null && slot.item != null && slot.item.itemName == itemName);
        if (slot != null)
        {
            return slot.item;
        }

        CropData[] crops = Resources.LoadAll<CropData>("CropData");
        for (int i = 0; i < crops.Length; i++)
        {
            CropData crop = crops[i];
            if (crop != null && crop.item != null && crop.item.itemName == itemName)
            {
                return crop.item;
            }
        }

        return null;
    }

    public int TransferTo(Inventory target, InventorySlot slot)
    {
        if (slot == null || slot.item == null) return 0;
        
        int amount = slot.quantity;
        int leftover = target.AddItem(slot.item, amount);
        int moved = amount - leftover;
        slot.quantity -= moved;
        if (slot.quantity <= 0)
        {
            items.Remove(slot);
        }

        return moved;
    }
}
