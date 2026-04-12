using UnityEngine;

public class Container : MonoBehaviour, IInteractable
{
    public Inventory inventory;

    public void Interact(PlayerInteraction player)
    {
        UIManager.Instance?.OpenContainerForPlayer(player, inventory);
    }
}
