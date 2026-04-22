using UnityEngine;

public class GridInteractable : MonoBehaviour, IInteractable
{
    public FarmGrid grid;
    public FarmGridNetwork gridNetwork;

    public void Interact(PlayerInteraction player)
    {
        if (player == null || player.preview == null) return;

        ResolveGrid();
        if (grid == null || player.Controller == null) return;

        Vector2Int pos = grid.WorldToGrid(player.preview.transform.position);
        player.Controller.RequestHarvestServerRpc(pos.x, pos.y);
    }

    void ResolveGrid()
    {
        if (grid == null)
        {
            grid = FindFirstObjectByType<FarmGrid>();
        }

        if (gridNetwork == null)
        {
            gridNetwork = FindFirstObjectByType<FarmGridNetwork>();
        }
    }
}
