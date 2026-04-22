using UnityEngine;

public class ShovelTool : Tool
{
    private FarmGrid grid;
    private GameObject preview;
    private FarmGridNetwork _gridNetwork;
    private PlayerController _playerController;
    public override void Initialize(Camera cam, FarmGrid grid, GameObject preview)
    {
        this.grid = grid;
        this.preview = preview;
        ResolveGridNetwork();

    }

    public override void Use()
    {
        ResolvePlayerController();
        if (grid == null || preview == null || _playerController == null)
        {
            return;
        }

        Vector2Int gridPos = grid.WorldToGrid(preview.transform.position);
        Tile tile = grid.GetTile(gridPos.x, gridPos.y);

        if (tile != null && tile.type != TileType.Building)
        {
            _playerController.RequestUntillServerRpc(gridPos.x, gridPos.y);
        }
    }

    protected override void AltUse(){}

    void ResolveGridNetwork()
    {
        if (_gridNetwork == null)
        {
            _gridNetwork = FindFirstObjectByType<FarmGridNetwork>();
        }
    }

    void ResolvePlayerController()
    {
        if (_playerController == null)
        {
            _playerController = GetComponentInParent<PlayerController>();
        }
    }
}
