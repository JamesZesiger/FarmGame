using UnityEngine;

public class FarmInteraction : MonoBehaviour
{
    public Camera cam;
    public FarmGrid grid;
    public FarmGridNetwork gridNetwork;
    public LayerMask terrainMask;
    public float range = 100f;
    public GameObject preview;
    PlayerController _playerController;

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
        ResolvePlayerController();
        if (grid == null || preview == null || _playerController == null) return;

        Vector2Int gridPos = grid.WorldToGrid(preview.transform.position);
        _playerController.RequestTillServerRpc(gridPos.x, gridPos.y);
    }

    void ResolvePlayerController()
    {
        if (_playerController == null)
        {
            _playerController = GetComponent<PlayerController>();
        }
    }
}
