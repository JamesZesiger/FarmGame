using UnityEngine;

public class GridInteractable : MonoBehaviour, IInteractable
{
    public FarmGrid grid;

    public void Interact(PlayerInteraction player)
    {
        Vector2Int pos = grid.WorldToGrid(player.preview.transform.position);
        grid.TryHarvest(pos.x, pos.y, player.PlayerInventory);
    }
}
