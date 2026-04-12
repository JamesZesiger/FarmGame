using UnityEngine;
using UnityEngine.InputSystem;


public class Hammer : Tool
{
    public Camera cam;
    public FarmGrid grid;
    public LayerMask terrainMask;
    public float range = 100f;
    public GameObject preview;
    public HammerUI hammerUI;
    public StructureSet structureSet;

    public bool IsOpen { get; private set; }
    
    public override void Initialize(Camera cam, FarmGrid grid, GameObject preview)
    {
        //this.hammerUI = GetComponentInChildren<HammerUI>();
        this.cam = cam;
        this.grid = grid;
        this.preview = preview;
    }

     void Awake()
    {
        hammerUI.gameObject.SetActive(false);
        IsOpen = false;
        SetCursor(false);
    }

    void SetCursor(bool state)
    {
        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = state;
    }



    protected override void AltUse()
    {   
        if (IsOpen)
        {
           hammerUI.gameObject.SetActive(false); 
           SetCursor(false);
           IsOpen = false;
        }
        else
        {
            hammerUI.Init(structureSet);
            hammerUI.gameObject.SetActive(true);

            SetCursor(true);
            IsOpen = true;
        }
    }
    public override void Use()
    {
        if (IsOpen) return;

        if(hammerUI.selectedIndex == 0)
        {
            Vector2Int gridPos = grid.WorldToGrid(preview.transform.position);
            grid.RemoveStructure(gridPos.x, gridPos.y);
        }
        else
        {
            Vector2Int gridPos = grid.WorldToGrid(preview.transform.position);
            grid.SetTileType(gridPos.x, gridPos.y, TileType.Building, hammerUI.selectedIndex);
        }
    }
}