using UnityEngine;

public class Wateringcan : Tool
{
    private FarmGrid grid;
    private GameObject preview;
    private FarmGridNetwork _gridNetwork;
    public override void Initialize(Camera cam, FarmGrid grid, GameObject preview)
    {
        this.grid = grid;
        this.preview = preview;
        ResolveGridNetwork();

    }


    public override void Use()
    {
        if (grid == null || preview == null || _gridNetwork == null)
        {
            return;
        }

        Vector2Int gridPos = grid.WorldToGrid(preview.transform.position);
        Tile tile = grid.GetTile(gridPos.x, gridPos.y);

        if (tile != null && tile.type != TileType.Building)
        {
            _gridNetwork.WaterServerRpc(gridPos.x, gridPos.y);

        }
    }
    protected override void AltUse(){}

    void ResolveGridNetwork()
    {
        if (_gridNetwork == null)
        {
            _gridNetwork = FarmGridNetwork.Instance;
        }
    }
}
