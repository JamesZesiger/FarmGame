using UnityEngine;

public class HoeTool : Tool
{
    public Camera cam;
    public FarmGrid grid;
    public LayerMask terrainMask;
    public float range = 100f;
    public GameObject preview;

    public override void Initialize(Camera cam, FarmGrid grid, GameObject preview)
    {
        this.cam = cam;
        this.grid = grid;
        this.preview = preview;
    }
    public override void Use()
    {
        Debug.Log("hoe use");
        Vector2Int gridPos = grid.WorldToGrid(preview.transform.position);
        grid.SetTileType(gridPos.x, gridPos.y, TileType.Tilled);
    }
    protected override void AltUse(){}
}