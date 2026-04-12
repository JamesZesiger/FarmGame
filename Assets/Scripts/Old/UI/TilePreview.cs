using UnityEngine;
using UnityEngine.InputSystem;

public class TilePreview : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject cam;
    public FarmGrid farmGrid;
    public LayerMask terrainMask;
    public float range = 10f;
    public float height = 0.36f;

    public bool useMousePosition = false;
    public bool isEnabled;

    private Renderer rend;
    private Material mat;

    void Awake()
    {
        ResolveCameraReferences();

        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            SetAlpha(0f);
        }

        isEnabled = false;
    }

    void Update()
    {
        ResolveCameraReferences();

        if (farmGrid == null)
        {
            SetAlpha(0f);
            isEnabled = false;
            return;
        }

        useMousePosition = Cursor.visible;
        Vector3 targetPos = Vector3.zero;
        bool hitDetected = false;

        if (useMousePosition)
        {
            if (Mouse.current != null && mainCamera != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                Ray ray = mainCamera.ScreenPointToRay(mousePos);

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainMask) &&
                    hit.collider.CompareTag("Ground"))
                {
                    hitDetected = true;
                    targetPos = hit.point;
                }
            }
        }
        else if (cam != null)
        {
            Vector3 dir = cam.transform.forward.normalized;

            if (Physics.Raycast(cam.transform.position, dir, out RaycastHit hit, range, terrainMask) &&
                hit.collider.CompareTag("Ground"))
            {
                hitDetected = true;
                targetPos = hit.point;
            }
        }

        if (hitDetected)
        {
            SetAlpha(0.5f);

            Vector2Int gridPos = farmGrid.WorldToGrid(targetPos);
            Vector3 worldPos = farmGrid.GridToWorld(gridPos.x, gridPos.y);
            worldPos.y += height;

            transform.position = worldPos;
            isEnabled = true;
        }
        else
        {
            SetAlpha(0f);
            isEnabled = false;
        }
    }

    void ResolveCameraReferences()
    {
        if (mainCamera == null && PlayerController.LocalPlayer != null)
            mainCamera = PlayerController.LocalPlayer.PlayerCamera;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cam == null && mainCamera != null)
            cam = mainCamera.gameObject;
    }

    void SetAlpha(float alpha)
    {
        if (mat == null) return;

        Color color = mat.color;
        color.a = alpha;
        mat.color = color;
    }
}
