using UnityEngine;

public class SeedTool : Tool
{
    private FarmGrid grid;
    private GameObject preview;
    private FarmGridNetwork _gridNetwork;
    private PlayerController _playerController;

    public CropData cropToPlant;

    public override void Initialize(Camera cam, FarmGrid grid, GameObject preview)
    {
        this.grid = grid;
        this.preview = preview;
        ResolveGridNetwork();
    }

    public override void Use()
    {
        ResolvePlayerController();
        if (grid == null || preview == null || _playerController == null || cropToPlant == null) return;

        Vector2Int pos = grid.WorldToGrid(preview.transform.position);
        Tile tile = grid.GetTile(pos.x, pos.y);
        if (tile == null || tile.type != TileType.Tilled || tile.crop != null) return;

        _playerController.RequestPlantServerRpc(pos.x, pos.y, cropToPlant.cropName);
        if (isConsumable) numUses -= 1;
    }
    protected override void AltUse(){}

    void ResolveGridNetwork()
    {
        if (_gridNetwork == null)
        {
            _gridNetwork = FarmGridNetwork.Instance;
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
