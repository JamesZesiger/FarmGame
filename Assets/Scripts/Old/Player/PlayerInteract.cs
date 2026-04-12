using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInteraction : MonoBehaviour
{
    public FarmGrid grid;
    public GameObject preview;

    public GameObject projector;
    public float interactRange = 5f;
    public LayerMask interactMask;
    public Inventory inventory;

    private PlayerController controller;

    public Inventory PlayerInventory => inventory != null ? inventory : controller != null ? controller.PlayerInventory : null;
    public UIManager UIManager => controller != null ? controller.UIManager : null;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public void OnInteract()
    {
        Vector3 dir = projector.transform.forward;
        dir.Normalize();

        // 🔹 Try world interaction first
        if (Physics.Raycast(projector.transform.position, dir, out RaycastHit hit, interactRange, interactMask))
        {
            IInteractable interactable = hit.transform.gameObject.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
                return; // STOP here → don't hit grid
            }
        }

        // 🔹 Fallback → grid interaction
        Vector2Int pos = grid.WorldToGrid(preview.transform.position);
        grid.TryHarvest(pos.x, pos.y, PlayerInventory);
    }
}





