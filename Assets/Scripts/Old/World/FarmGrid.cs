using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FarmGrid : MonoBehaviour
{
    private static readonly int TileColorID = Shader.PropertyToID("_TileColor");
    private static MaterialPropertyBlock mpb;

    [Header("Grid Settings")]
    public int width = 50;
    public int height = 50;
    public float tileSize = 1f;
    public float dryTime = 60f;

    [Header("Tile Sets (per tool)")]
    public TileSet hoeTileSet;
    public StructureSet hammerTileSet;

    [Header("References")]
    public Transform tileParent;
    public Vector3 originPosition;
    public GameObject progressUIPrefab;

    private Tile[,] tiles;
    private ToolType currentTool = ToolType.Hoe;
    private TileSet activeTileSet;
    private StructureSet activeStructureSet;

    private List<Vector2Int> activeCropTiles = new();
    private List<Vector2Int> wateredTiles = new();

    // ================================
    // POOLING
    // ================================
    private Dictionary<GameObject, Queue<GameObject>> pool = new();

    void Awake()
    {
        tiles = new Tile[width, height];
        activeTileSet = hoeTileSet;
        InitializeGrid();
    }

    void Start()
    {
        if (hoeTileSet != null) hoeTileSet.BuildTileLookup();
        if (hammerTileSet != null) hammerTileSet.BuildTileLookup();

        foreach (var cropData in Resources.LoadAll<CropData>(""))
        {
            foreach (var prefab in cropData.GetAllPrefabs())
                if (prefab != null)
                    PrewarmPool(prefab, 20);
        }

        foreach (var tileSet in Resources.LoadAll<TileSet>(""))
        {
            foreach (var prefab in tileSet.GetAllPrefabs())
                if (prefab != null)
                    PrewarmPool(prefab, 20);
        }
    }

    void InitializeGrid()
    {
        Debug.Log("initgrid");
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            tiles[x, y] = new Tile();
            tiles[x, y].worldPosition = GridToWorld(x, y);
        }
    }

    void Update()
    {
        GrowCrops(Time.deltaTime);
        DryTile(Time.deltaTime);
    }

    // ================================
    // TOOL SWITCHING
    // ================================
    public void SetTool(ToolType tool)
    {
        currentTool = tool;

        if (tool == ToolType.Hoe)
        {
            activeTileSet = hoeTileSet;
            activeStructureSet = null;
        }
        else if (tool == ToolType.Hammer)
        {
            activeStructureSet = hammerTileSet;
            activeTileSet = null;
        }
    }

    // ================================
    // PLACE TILE
    // ================================
    public void PlaceTile(int x, int y)
    {
        if (!InBounds(x, y)) return;

        tiles[x, y].active = true;
        UpdateTileAndNeighbors(x, y);
    }

    void UpdateTileAndNeighbors(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            int nx = x + dx;
            int ny = y + dy;

            if (InBounds(nx, ny) && tiles[nx, ny].active)
                UpdateTileVisual(nx, ny);
        }
    }

    // ================================
    // VISUAL
    // ================================
    void SetTileColor(Renderer rend, Color color)
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();

        rend.GetPropertyBlock(mpb);
        mpb.SetColor(TileColorID, color);
        rend.SetPropertyBlock(mpb);
    }

    void UpdateTileVisual(int x, int y)
    {
        Tile tile = tiles[x, y];
        if (tile.type == TileType.Empty) return;

        if (tile.type != TileType.Building)
        {
            int mask = GetBitmask(x, y, tile.tileSet);
            TileVisual visual = tile.tileSet.GetTileVisual(mask);

            if (tile.instance == null || tile.sourcePrefab != visual.prefab)
            {
                if (tile.instance != null)
                    ReturnToPoolPrefab(tile.instance, tile.sourcePrefab);

                Vector3 pos = tile.worldPosition;
                pos.y -= 0.1f;

                tile.instance = SpawnFromPool(visual.prefab, pos, visual.rotation, tileParent);
                tile.sourcePrefab = visual.prefab;

                tile.renderer = tile.instance.GetComponent<Renderer>();
            }

            if (tile.renderer != null)
            {
                SetTileColor(tile.renderer,
                    tile.isWatered ? tile.tileSet.colorWet : tile.tileSet.color);
            }
        }
        else
        {
            TileVisual visual = tile.structureSet.GetStructureVisual(tile.structureIndex);

            if (tile.instance == null || tile.sourcePrefab != visual.prefab)
            {
                if (tile.instance != null)
                    ReturnToPoolPrefab(tile.instance, tile.sourcePrefab);

                Vector3 pos = tile.worldPosition;
                pos.y -= 0.1f;

                tile.instance = SpawnFromPool(visual.prefab, pos, visual.rotation, tileParent);
                tile.sourcePrefab = visual.prefab;

                tile.renderer = tile.instance.GetComponent<Renderer>();
            }
        }
    }

    // ================================
    // BITMASKING
    // ================================
    int GetBitmask(int x, int z, TileSet tileSet)
    {
        bool top = tileSet.type.Contains(GetTile(x, z + 1)?.type ?? TileType.Empty);
        bool right = tileSet.type.Contains(GetTile(x + 1, z)?.type ?? TileType.Empty);
        bool bottom = tileSet.type.Contains(GetTile(x, z - 1)?.type ?? TileType.Empty);
        bool left = tileSet.type.Contains(GetTile(x - 1, z)?.type ?? TileType.Empty);

        bool topRight = tileSet.type.Contains(GetTile(x + 1, z + 1)?.type ?? TileType.Empty) && top && right;
        bool bottomRight = tileSet.type.Contains(GetTile(x + 1, z - 1)?.type ?? TileType.Empty) && bottom && right;
        bool bottomLeft = tileSet.type.Contains(GetTile(x - 1, z - 1)?.type ?? TileType.Empty) && bottom && left;
        bool topLeft = tileSet.type.Contains(GetTile(x - 1, z + 1)?.type ?? TileType.Empty) && top && left;

        int mask = 0;
        if (top) mask |= 1;
        if (topRight) mask |= 2;
        if (right) mask |= 4;
        if (bottomRight) mask |= 8;
        if (bottom) mask |= 16;
        if (bottomLeft) mask |= 32;
        if (left) mask |= 64;
        if (topLeft) mask |= 128;

        return mask;
    }

    bool HasTile(int x, int y, TileType type)
    {
        return InBounds(x, y) && tiles[x, y].type == type;
    }

    bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    // ================================
    // TILE TYPE
    // ================================
    public void SetTileType(int x, int y, TileType type, int? index = null)
    {
        if (!InBounds(x, y)) 
        {
            return;
        }

        Tile tile = GetTile(x, y);
        if (tile == null)
        {
            return;
        }

        if (tile.type != TileType.Empty) return;

        tile.type = type;
        tile.active = type != TileType.Empty;

        if(index != null)
        {
            tile.structureIndex = index;
            tile.structureSet = activeStructureSet;
        }
        else
            tile.tileSet = activeTileSet;

        UpdateTileAndNeighbors(x, y);
    }

    public void TillTile(int x, int y)
    {
        if (!InBounds(x, y)) return;

        Tile tile = GetTile(x, y);

        if (tile.type == TileType.Empty)
        {
            tile.type = TileType.Tilled;
            tile.active = true;

            UpdateTileAndNeighbors(x, y);
        }
    }

    public void UntillTile(int x, int y)
    {
        if (!InBounds(x, y)) return;

        Tile tile = tiles[x, y];
        if (tile.type == TileType.Empty) return;

        tile.type = TileType.Empty;
        tile.active = false;

        if (tile.crop != null)
        {
            CropInstance crop = tile.crop;

            if (crop.visual != null)
                ReturnToPoolPrefab(crop.visual, crop.sourcePrefab);

            if (crop.progressUI != null)
            {
                ReturnToPoolPrefab(crop.progressUI.gameObject, progressUIPrefab);
                crop.progressUI = null;
            }

            tile.crop = null;
        }

        if (tile.instance != null)
            ReturnToPoolPrefab(tile.instance, tile.sourcePrefab);

        tile.instance = null;
        tile.sourcePrefab = null;
        tile.renderer = null;

        UpdateTileAndNeighbors(x, y);
    }

    // ================================
    // CROPS
    // ================================
    public bool PlantCrop(int x, int z, CropData cropData)
    {
        Tile tile = GetTile(x, z);

        if (tile == null || tile.type != TileType.Tilled || tile.crop != null)
            return false;

        CropInstance crop = new CropInstance(cropData);
        tile.crop = crop;
        tile.type = TileType.Planted;

        activeCropTiles.Add(new Vector2Int(x, z));

        SpawnCropVisual(x, z);
        return true;
    }

    void SpawnCropVisual(int x, int z)
    {
        Tile tile = GetTile(x, z);
        if (tile?.crop == null) return;

        CropInstance crop = tile.crop;

        if (crop.visual != null)
            ReturnToPoolPrefab(crop.visual, crop.sourcePrefab);

        GameObject prefab = null;

        switch (crop.state)
        {
            case CropState.Seed:
                prefab = crop.data.seedPrefab;
                break;
            case CropState.Growing:
            case CropState.ReGrowing:
                prefab = crop.data.growingPrefab;
                break;
            case CropState.Ready:
                prefab = crop.data.readyPrefab;
                break;
        }

        Vector3 pos = tile.worldPosition;
        pos.y += 0.1f;

        GameObject obj = GetFromPool(prefab);
        obj.transform.SetPositionAndRotation(pos, Quaternion.identity);

        crop.visual = obj;
        crop.sourcePrefab = prefab;

        obj.transform.localScale =
            (crop.state == CropState.Growing) ? Vector3.one * 0.5f : Vector3.one;

        if (crop.progressUI != null)
        {
            ReturnToPoolPrefab(crop.progressUI.gameObject, progressUIPrefab);
            crop.progressUI = null;
        }

        GameObject uiObj = GetFromPool(progressUIPrefab);
        CropProgressUI ui = uiObj.GetComponent<CropProgressUI>();
        ui.Initialize(crop.visual.transform);

        crop.progressUI = ui;
    }

    void GrowCrops(float deltaTime)
    {
        for (int i = activeCropTiles.Count - 1; i >= 0; i--)
        {
            var pos = activeCropTiles[i];
            Tile tile = tiles[pos.x, pos.y];

            if (tile.crop == null)
            {
                activeCropTiles.RemoveAt(i);
                continue;
            }

            if (!tile.isWatered) continue;

            CropInstance crop = tile.crop;
            crop.timer += deltaTime;

            float t = 0f;

            switch (crop.state)
            {
                case CropState.Seed:
                    t = crop.timer / crop.data.seedToGrowingTime;
                    if (crop.timer >= crop.data.seedToGrowingTime)
                    {
                        crop.timer = 0f;
                        crop.state = CropState.Growing;
                        SpawnCropVisual(pos.x, pos.y);
                    }
                    break;

                case CropState.Growing:
                    t = crop.timer / crop.data.growingToReadyTime;

                    if (crop.visual != null)
                    {
                        float scale = Mathf.Lerp(0.5f, 1f, Mathf.SmoothStep(0, 1, t));
                        crop.visual.transform.localScale = Vector3.one * scale;
                    }

                    if (crop.timer >= crop.data.growingToReadyTime)
                    {
                        crop.timer = 0f;
                        crop.state = CropState.Ready;
                        SpawnCropVisual(pos.x, pos.y);
                    }
                    break;

                case CropState.ReGrowing:
                    t = crop.timer / crop.data.growingToReadyTime;

                    if (crop.timer >= crop.data.growingToReadyTime)
                    {
                        crop.timer = 0f;
                        crop.state = CropState.Ready;
                        SpawnCropVisual(pos.x, pos.y);
                    }
                    break;

                case CropState.Ready:
                    t = 1f;
                    break;
            }

            if (crop.progressUI != null)
                crop.progressUI.SetProgress(t);
        }
    }

    public bool TryHarvest(int x, int z, Inventory targetInventory)
    {
        if (targetInventory == null) return false;

        Tile tile = GetTile(x, z);
        if (tile?.crop == null) return false;

        CropInstance crop = tile.crop;
        if (!crop.IsReady()) return false;

        if (crop.data.item != null)
        {
            int leftover = targetInventory.AddItem(crop.data.item, 1);
            if (leftover > 0) return false;
        }

        if (crop.data.regrowable)
        {
            crop.state = CropState.ReGrowing;
            crop.timer = 0f;
            SpawnCropVisual(x, z);
        }
        else
        {
            if (crop.visual != null)
                ReturnToPoolPrefab(crop.visual, crop.sourcePrefab);

            if (crop.progressUI != null)
                ReturnToPoolPrefab(crop.progressUI.gameObject, progressUIPrefab);

            tile.crop = null;
            tile.type = TileType.Tilled;
        }

        return true;
    }

    // ================================
    // WATER
    // ================================
    public void WaterTile(int x, int z)
    {
        if (!InBounds(x, z)) return;

        Tile tile = GetTile(x, z);
        if (tile.type != TileType.Tilled && tile.type != TileType.Planted) return;

        if (!tile.isWatered)
            wateredTiles.Add(new Vector2Int(x, z));

        tile.isWatered = true;

        if (tile.renderer != null)
            SetTileColor(tile.renderer, hoeTileSet.colorWet);
    }

    void DryTile(float deltaTime)
    {
        for (int i = wateredTiles.Count - 1; i >= 0; i--)
        {
            var pos = wateredTiles[i];
            Tile tile = tiles[pos.x, pos.y];

            if (!tile.isWatered)
            {
                wateredTiles.RemoveAt(i);
                continue;
            }

            tile.waterTimer += deltaTime;

            if (tile.waterTimer >= dryTime)
            {
                tile.waterTimer = 0;
                tile.isWatered = false;

                UpdateTileVisual(pos.x, pos.y);
                wateredTiles.RemoveAt(i);
            }
        }
    }

    // ================================
    // GRID HELPERS (UNCHANGED API)
    // ================================
    Vector3 GridToWorldFromTile(Tile tile)
    {
        return tile.worldPosition;
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - originPosition.x) / tileSize);
        int z = Mathf.FloorToInt((worldPos.z - originPosition.z) / tileSize);
        return new Vector2Int(x, z);
    }

    public Vector3 GridToWorld(int x, int z)
    {
        Vector3 pos = new Vector3(
            originPosition.x + x * tileSize + tileSize / 2f,
            0,
            originPosition.z + z * tileSize + tileSize / 2f);

        if (Terrain.activeTerrain != null)
            pos.y = Terrain.activeTerrain.SampleHeight(pos);

        return pos;
    }

    public Tile GetTile(int x, int z)
    {
        if (x < 0 || z < 0 || x >= width || z >= height) return null;
        return tiles[x, z];
    }

    // ================================
    // STRUCTURES
    // ================================
    public void RemoveStructure(int x, int y)
    {
        if (!InBounds(x, y)) return;

        Tile tile = tiles[x, y];
        if (tile.type != TileType.Building) return;

        tile.type = TileType.Empty;
        tile.active = false;
        tile.structureIndex = null;
        tile.structureSet = null;

        if (tile.instance != null)
            ReturnToPoolPrefab(tile.instance, tile.sourcePrefab);

        tile.instance = null;
        tile.renderer = null;

        UpdateTileAndNeighbors(x, y);
    }

    // ================================
    // POOLING
    // ================================
    GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (!pool.TryGetValue(prefab, out Queue<GameObject> q))
            pool[prefab] = q = new Queue<GameObject>();

        if (q.Count > 0)
        {
            var obj = q.Dequeue();
            obj.SetActive(true);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.transform.parent = parent;
            return obj;
        }

        return Instantiate(prefab, position, rotation, parent);
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if (pool.TryGetValue(prefab, out Queue<GameObject> q) && q.Count > 0)
        {
            var obj = q.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        return Instantiate(prefab);
    }

    void ReturnToPoolPrefab(GameObject obj, GameObject prefab)
    {
        obj.SetActive(false);

        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<GameObject>();

        pool[prefab].Enqueue(obj);
    }

    void PrewarmPool(GameObject prefab, int count)
    {
        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool[prefab].Enqueue(obj);
        }
    }
}
