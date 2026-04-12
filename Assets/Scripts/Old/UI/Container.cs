using UnityEngine;

public class Container : MonoBehaviour, IInteractable
{
    public Inventory inventory;

    public void Interact(PlayerInteraction player)
    {
        if (player == null || player.UIManager == null) return;
        player.UIManager.OpenContainer(player.PlayerInventory, inventory);
    }
}
