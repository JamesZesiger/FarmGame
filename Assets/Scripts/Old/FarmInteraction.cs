using UnityEngine;

public class FarmInteraction : MonoBehaviour
{
    public Camera cam;
    public FarmGrid grid;
    public LayerMask terrainMask;
    public float range = 100f;
    public GameObject preview;

    void Awake()
    {
        ResolveFarmGrid();
    }

    void OnEnable()
    {
        ResolveFarmGrid();
    }

    void Update()
    {
        ResolveFarmGrid();
    }

    void ResolveFarmGrid()
    {
        if (grid == null)
            grid = FindFirstObjectByType<FarmGrid>();
    }

    public void Interact()
    {
        if (grid == null || preview == null) return;

        Vector2Int gridPos = grid.WorldToGrid(preview.transform.position);
        Tile tile = grid.GetTile(gridPos.x, gridPos.y);
        if (tile != null)
            {
                grid.TillTile(gridPos.x,gridPos.y);
                tile.type = TileType.Tilled;
            }
    }
}
