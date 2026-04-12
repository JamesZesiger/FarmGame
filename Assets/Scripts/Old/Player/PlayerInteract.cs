using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public FarmGrid grid;
    public FarmGridNetwork gridNetwork;
    public GameObject preview;

    public GameObject projector;
    public float interactRange = 5f;
    public LayerMask interactMask;
    public Inventory inventory;

    private PlayerController controller;

    public PlayerController Controller => controller;
    public Inventory PlayerInventory => inventory != null ? inventory : controller != null ? controller.PlayerInventory : null;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        ResolveFarmGrid();
    }

    void OnEnable()
    {
        ResolveFarmGrid();
    }

    public void OnInteract()
    {
        ResolveFarmGrid();
        if (grid == null || gridNetwork == null || preview == null || projector == null) return;

        Vector3 dir = projector.transform.forward;
        dir.Normalize();

        if (Physics.Raycast(projector.transform.position, dir, out RaycastHit hit, interactRange, interactMask))
        {
            IInteractable interactable = hit.transform.gameObject.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
                return;
            }
        }

        Vector2Int pos = grid.WorldToGrid(preview.transform.position);
        gridNetwork.HarvestServerRpc(pos.x, pos.y);
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
}
