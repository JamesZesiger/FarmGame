using UnityEngine;

public class HoeTool : Tool
{
    public Camera cam;
    public FarmGrid grid;
    public FarmGridNetwork gridNetwork;
    public LayerMask terrainMask;
    public float range = 100f;
    public GameObject preview;
    PlayerController _playerController;

    public override void Initialize(Camera cam, FarmGrid grid, GameObject preview)
    {
        this.cam = cam;
        this.grid = grid;
        this.preview = preview;
        ResolveGridNetwork();
    }
    public override void Use()
    {
        ResolvePlayerController();
        if (grid == null || preview == null || _playerController == null) return;

        Vector2Int gridPos = grid.WorldToGrid(preview.transform.position);
        Debug.Log($"Client: Requesting till for {gridPos.x}, {gridPos.y}");
        _playerController.RequestTillServerRpc(gridPos.x, gridPos.y);
    }
    protected override void AltUse(){}

    void ResolveGridNetwork()
    {
        if (gridNetwork == null)
        {
            gridNetwork = FarmGridNetwork.Instance;
            Debug.Log($"Resolved gridNetwork: {gridNetwork != null}");
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
