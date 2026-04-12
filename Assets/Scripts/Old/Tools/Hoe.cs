using UnityEngine;

public class HoeTool : Tool
{
    public Camera cam;
    public FarmGrid grid;
    public FarmGridNetwork gridNetwork;
    public LayerMask terrainMask;
    public float range = 100f;
    public GameObject preview;

    public override void Initialize(Camera cam, FarmGrid grid, GameObject preview)
    {
        this.cam = cam;
        this.grid = grid;
        this.preview = preview;
        ResolveGridNetwork();
    }
    public override void Use()
    {
        if (grid == null || preview == null || gridNetwork == null) return;

        Vector2Int gridPos = grid.WorldToGrid(preview.transform.position);
        Debug.Log($"Client: Calling TillServerRpc for {gridPos.x}, {gridPos.y}");
        gridNetwork.TillServerRpc(gridPos.x, gridPos.y);
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
}
