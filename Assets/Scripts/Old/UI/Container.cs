using UnityEngine;

public class Container : MonoBehaviour, IInteractable
{
    public Inventory inventory;

    public void Interact(PlayerInteraction player)
    {
        player?.Controller?.UIManager?.OpenContainerForPlayer(player, inventory);
    }
}
