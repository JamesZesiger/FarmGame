using UnityEngine;

public class FarmInteraction : MonoBehaviour
{
    public Camera cam;
    public FarmGrid grid;
    public FarmGridNetwork gridNetwork;
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
        {
            grid = FindFirstObjectByType<FarmGrid>();
        }

        if (gridNetwork == null)
        {
            gridNetwork = FindFirstObjectByType<FarmGridNetwork>();
        }
    }

    public void Interact()
    {
        if (grid == null || gridNetwork == null || preview == null) return;

        Vector2Int gridPos = grid.WorldToGrid(preview.transform.position);
        gridNetwork.TillServerRpc(gridPos.x, gridPos.y);
    }
}
