using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    private Dictionary<string, CropData> cropLookup = new();
    private Dictionary<GameObject, Queue<GameObject>> pool = new();
    private bool _lookupsInitialized;

    public bool simulateStateLocally = true;
    public event System.Action<FarmTileState> TileStateChanged;

    void Awake()
    {
        tiles = new Tile[width, height];
        activeTileSet = hoeTileSet;
        InitializeGrid();
        EnsureAssetLookups();
    }

    void Start()
    {
        EnsureAssetLookups();

        foreach (var cropData in cropLookup.Values)
        {
            foreach (var prefab in cropData.GetAllPrefabs())
            {
                if (prefab != null)
                {
                    PrewarmPool(prefab, 20);
                }
            }
        }

        foreach (var tileSet in Resources.LoadAll<TileSet>(""))
        {
            foreach (var prefab in tileSet.GetAllPrefabs())
            {
                if (prefab != null)
                {
                    PrewarmPool(prefab, 20);
                }
            }
        }
    }

    void Update()
    {
        if (simulateStateLocally)
        {
            GrowCrops(Time.deltaTime);
            DryTile(Time.deltaTime);
        }
        else
        {
            UpdateCropVisuals(Time.deltaTime);
        }
    }

    void InitializeGrid()
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            tiles[x, y] = new Tile();
            tiles[x, y].worldPosition = GridToWorld(x, y);
        }
    }

    void EnsureAssetLookups()
    {
        if (_lookupsInitialized) return;

        if (hoeTileSet != null) hoeTileSet.BuildTileLookup();
        if (hammerTileSet != null) hammerTileSet.BuildTileLookup();

        cropLookup.Clear();

        foreach (var cropData in Resources.LoadAll<CropData>("CropData"))
        {
            if (cropData != null && !string.IsNullOrWhiteSpace(cropData.cropName))
            {
                cropLookup[cropData.cropName] = cropData;
            }
        }

        _lookupsInitialized = true;
    }

    public void SetSimulationEnabled(bool enabled)
    {
        simulateStateLocally = enabled;
    }

    public CropData GetCropData(string cropName)
    {
        EnsureAssetLookups();

        if (string.IsNullOrWhiteSpace(cropName))
        {
            return null;
        }

        cropLookup.TryGetValue(cropName, out CropData cropData);
        return cropData;
    }

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
            {
                UpdateTileVisual(nx, ny);
            }
        }
    }

    void SetTileColor(Renderer rend, Color color)
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();

        rend.GetPropertyBlock(mpb);
        mpb.SetColor(TileColorID, color);
        rend.SetPropertyBlock(mpb);
    }

    void UpdateTileVisual(int x, int y)
    {
        EnsureAssetLookups();

        Tile tile = tiles[x, y];
        if (tile.type == TileType.Empty) return;

        if (tile.type != TileType.Building)
        {
            int mask = GetBitmask(x, y, tile.tileSet);
            TileVisual visual = tile.tileSet.GetTileVisual(mask);

            if (tile.instance == null || tile.sourcePrefab != visual.prefab)
            {
                if (tile.instance != null)
                {
                    ReturnToPoolPrefab(tile.instance, tile.sourcePrefab);
                }

                Vector3 pos = tile.worldPosition;
                pos.y -= 0.1f;

                tile.instance = SpawnFromPool(visual.prefab, pos, visual.rotation, tileParent);
                tile.sourcePrefab = visual.prefab;
                tile.renderer = tile.instance.GetComponent<Renderer>();
            }

            if (tile.renderer != null)
            {
                SetTileColor(tile.renderer, tile.isWatered ? tile.tileSet.colorWet : tile.tileSet.color);
            }
        }
        else
        {
            TileVisual visual = tile.structureSet.GetStructureVisual(tile.structureIndex);

            if (tile.instance == null || tile.sourcePrefab != visual.prefab)
            {
                if (tile.instance != null)
                {
                    ReturnToPoolPrefab(tile.instance, tile.sourcePrefab);
                }

                Vector3 pos = tile.worldPosition;
                pos.y -= 0.1f;

                tile.instance = SpawnFromPool(visual.prefab, pos, visual.rotation, tileParent);
                tile.sourcePrefab = visual.prefab;
                tile.renderer = tile.instance.GetComponent<Renderer>();
            }
        }
    }

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

    bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public void SetTileType(int x, int y, TileType type, int? index = null)
    {
        if (type == TileType.Tilled)
        {
            TryTillTile(x, y);
            return;
        }

        if (type == TileType.Building && index.HasValue)
        {
            TryPlaceStructure(x, y, index.Value);
        }
    }

    public bool TryTillTile(int x, int y)
    {
        if (!InBounds(x, y)) return false;

        Tile tile = GetTile(x, y);
        if (tile == null || tile.type != TileType.Empty) return false;

        tile.type = TileType.Tilled;
        tile.active = true;
        tile.tileSet = hoeTileSet;
        tile.structureSet = null;
        tile.structureIndex = null;

        UpdateTrackedCollections(x, y, tile);
        UpdateTileAndNeighbors(x, y);
        NotifyTileStateChanged(x, y);
        return true;
    }

    public void TillTile(int x, int y)
    {
        TryTillTile(x, y);
    }

    public bool TryUntillTile(int x, int y)
    {
        if (!InBounds(x, y)) return false;

        Tile tile = tiles[x, y];
        if (tile.type == TileType.Empty) return false;

        ClearTileState(tile);
        UpdateTrackedCollections(x, y, tile);
        UpdateTileAndNeighbors(x, y);
        NotifyTileStateChanged(x, y);
        return true;
    }

    public void UntillTile(int x, int y)
    {
        TryUntillTile(x, y);
    }

    public bool PlantCrop(int x, int z, CropData cropData)
    {
        EnsureAssetLookups();

        Tile tile = GetTile(x, z);
        if (cropData == null || tile == null || tile.type != TileType.Tilled || tile.crop != null)
        {
            return false;
        }

        CropInstance crop = new CropInstance(cropData);
        tile.crop = crop;
        tile.type = TileType.Planted;
        tile.tileSet = hoeTileSet;

        UpdateTrackedCollections(x, z, tile);
        UpdateTileAndNeighbors(x, z);
        SpawnCropVisual(x, z);
        NotifyTileStateChanged(x, z);
        return true;
    }

    void SpawnCropVisual(int x, int z)
    {
        Tile tile = GetTile(x, z);
        if (tile?.crop == null) return;

        CropInstance crop = tile.crop;

        if (crop.visual != null)
        {
            ReturnToPoolPrefab(crop.visual, crop.sourcePrefab);
        }

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

        if (prefab == null) return;

        Vector3 pos = tile.worldPosition;
        pos.y += 0.1f;

        Debug.Log($"Spawning crop visual at {pos} for tile {x},{z}");

        GameObject obj = GetFromPool(prefab);
        obj.transform.SetPositionAndRotation(pos, Quaternion.identity);

        crop.visual = obj;
        crop.sourcePrefab = prefab;
        obj.transform.localScale = crop.state == CropState.Growing ? Vector3.one * 0.5f : Vector3.one;

        if (crop.progressUI != null)
        {
            ReturnToPoolPrefab(crop.progressUI.gameObject, progressUIPrefab);
            crop.progressUI = null;
        }

        GameObject uiObj = GetFromPool(progressUIPrefab);
        uiObj.transform.SetParent(null, true);

        CropProgressUI ui = uiObj.GetComponent<CropProgressUI>();
        ui.Initialize(crop.visual.transform);

        crop.progressUI = ui;
        UpdateCropPresentation(crop);
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
            bool stateChanged = false;

            switch (crop.state)
            {
                case CropState.Seed:
                    t = crop.timer / crop.data.seedToGrowingTime;
                    if (crop.timer >= crop.data.seedToGrowingTime)
                    {
                        crop.timer = 0f;
                        crop.state = CropState.Growing;
                        stateChanged = true;
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
                        stateChanged = true;
                    }
                    break;

                case CropState.ReGrowing:
                    t = crop.timer / crop.data.growingToReadyTime;

                    if (crop.timer >= crop.data.growingToReadyTime)
                    {
                        crop.timer = 0f;
                        crop.state = CropState.Ready;
                        stateChanged = true;
                    }
                    break;

                case CropState.Ready:
                    t = 1f;
                    break;
            }

            if (stateChanged)
            {
                SpawnCropVisual(pos.x, pos.y);
                NotifyTileStateChanged(pos.x, pos.y);
            }

            if (crop.progressUI != null)
            {
                crop.progressUI.SetProgress(t);
            }
        }
    }

    void UpdateCropVisuals(float deltaTime)
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

            CropInstance crop = tile.crop;

            if (tile.isWatered && crop.state != CropState.Ready)
            {
                crop.timer += deltaTime;
            }

            float duration = GetCropStateDuration(crop);
            if (duration > 0f)
            {
                crop.timer = Mathf.Clamp(crop.timer, 0f, duration);
            }

            UpdateCropPresentation(crop);
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
            {
                ReturnToPoolPrefab(crop.visual, crop.sourcePrefab);
            }

            if (crop.progressUI != null)
            {
                ReturnToPoolPrefab(crop.progressUI.gameObject, progressUIPrefab);
            }

            tile.crop = null;
            tile.type = TileType.Tilled;
        }

        UpdateTrackedCollections(x, z, tile);
        UpdateTileAndNeighbors(x, z);
        NotifyTileStateChanged(x, z);
        return true;
    }

    public bool WaterTile(int x, int z)
    {
        if (!InBounds(x, z)) return false;

        Tile tile = GetTile(x, z);
        if (tile.type != TileType.Tilled && tile.type != TileType.Planted) return false;

        tile.isWatered = true;
        tile.waterTimer = 0f;

        UpdateTrackedCollections(x, z, tile);

        if (tile.renderer != null)
        {
            SetTileColor(tile.renderer, hoeTileSet.colorWet);
        }

        NotifyTileStateChanged(x, z);
        return true;
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
                tile.waterTimer = 0f;
                tile.isWatered = false;

                UpdateTileVisual(pos.x, pos.y);
                wateredTiles.RemoveAt(i);
                NotifyTileStateChanged(pos.x, pos.y);
            }
        }
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
        {
            pos.y = Terrain.activeTerrain.SampleHeight(pos);
        }

        return pos;
    }

    public Tile GetTile(int x, int z)
    {
        if (x < 0 || z < 0 || x >= width || z >= height) return null;
        return tiles[x, z];
    }

    public bool TryPlaceStructure(int x, int y, int structureIndex)
    {
        if (!InBounds(x, y)) return false;

        Tile tile = tiles[x, y];
        if (tile.type != TileType.Empty) return false;

        tile.type = TileType.Building;
        tile.active = true;
        tile.tileSet = null;
        tile.structureSet = hammerTileSet;
        tile.structureIndex = structureIndex;
        tile.isWatered = false;
        tile.waterTimer = 0f;

        UpdateTrackedCollections(x, y, tile);
        UpdateTileAndNeighbors(x, y);
        NotifyTileStateChanged(x, y);
        return true;
    }

    public bool RemoveStructure(int x, int y)
    {
        if (!InBounds(x, y)) return false;

        Tile tile = tiles[x, y];
        if (tile.type != TileType.Building) return false;

        ClearTileState(tile);
        UpdateTrackedCollections(x, y, tile);
        UpdateTileAndNeighbors(x, y);
        NotifyTileStateChanged(x, y);
        return true;
    }

    public FarmTileState CaptureTileState(int x, int y)
    {
        Tile tile = GetTile(x, y);
        CropInstance crop = tile != null ? tile.crop : null;

        return new FarmTileState
        {
            x = x,
            y = y,
            tileType = tile != null ? tile.type : TileType.Empty,
            isWatered = tile != null && tile.isWatered,
            waterTimer = tile != null ? tile.waterTimer : 0f,
            hasStructure = tile != null && tile.structureIndex.HasValue,
            structureIndex = tile != null && tile.structureIndex.HasValue ? tile.structureIndex.Value : -1,
            hasCrop = crop != null && crop.data != null,
            cropName = (crop != null && crop.data != null)
                ? new Unity.Collections.FixedString64Bytes(crop.data.cropName)
                : new Unity.Collections.FixedString64Bytes(""),
            cropState = crop != null ? crop.state : CropState.Seed,
            cropTimer = crop != null ? crop.timer : 0f
        };
    }

    public FarmTileState[] CaptureAllTileStates()
    {
        FarmTileState[] states = new FarmTileState[width * height];
        int index = 0;

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            states[index++] = CaptureTileState(x, y);
        }

        return states;
    }

    public FarmTileState[] CaptureActiveTileStates()
    {
        List<FarmTileState> states = new();

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            Tile tile = tiles[x, y];
            if (tile == null || tile.type == TileType.Empty)
            {
                continue;
            }

            states.Add(CaptureTileState(x, y));
        }

        return states.ToArray();
    }

    public void ApplyTileState(FarmTileState state)
    {
        if (!InBounds(state.x, state.y)) return;

        Tile tile = tiles[state.x, state.y];
        ClearTileState(tile);

        tile.type = state.tileType;
        tile.active = state.tileType != TileType.Empty;
        tile.isWatered = state.isWatered;
        tile.waterTimer = state.waterTimer;
        tile.tileSet = state.tileType == TileType.Tilled || state.tileType == TileType.Planted ? hoeTileSet : null;
        tile.structureSet = state.tileType == TileType.Building && state.hasStructure ? hammerTileSet : null;
        tile.structureIndex = state.tileType == TileType.Building && state.hasStructure ? state.structureIndex : null;

        if (state.hasCrop)
        {
            string cropName = state.cropName.ToString();
            if (string.IsNullOrEmpty(cropName)) return;
            CropData cropData = GetCropData(cropName);
            if (cropData != null)
            {
                tile.crop = new CropInstance(cropData)
                {
                    state = state.cropState,
                    timer = state.cropTimer
                };
                tile.type = TileType.Planted;
                tile.active = true;
                tile.tileSet = hoeTileSet;
            }
        }

        UpdateTrackedCollections(state.x, state.y, tile);
        UpdateTileAndNeighbors(state.x, state.y);

        if (tile.crop != null)
        {
            SpawnCropVisual(state.x, state.y);
        }
    }

    public void ApplyFullState(FarmTileState[] states)
    {
        if (states == null) return;

        ClearAllTileStates();

        for (int i = 0; i < states.Length; i++)
        {
            ApplyTileState(states[i]);
        }
    }

    GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (!pool.TryGetValue(prefab, out Queue<GameObject> q))
        {
            pool[prefab] = q = new Queue<GameObject>();
        }

        if (q.Count > 0)
        {
            GameObject obj = q.Dequeue();
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
            GameObject obj = q.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        return Instantiate(prefab);
    }

    void ReturnToPoolPrefab(GameObject obj, GameObject prefab)
    {
        if (obj == null || prefab == null) return;

        obj.SetActive(false);

        if (!pool.ContainsKey(prefab))
        {
            pool[prefab] = new Queue<GameObject>();
        }

        pool[prefab].Enqueue(obj);
    }

    void PrewarmPool(GameObject prefab, int count)
    {
        if (!pool.ContainsKey(prefab))
        {
            pool[prefab] = new Queue<GameObject>();
        }

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool[prefab].Enqueue(obj);
        }
    }

    void ClearTileState(Tile tile)
    {
        if (tile.crop != null)
        {
            CropInstance crop = tile.crop;

            if (crop.visual != null)
            {
                ReturnToPoolPrefab(crop.visual, crop.sourcePrefab);
            }

            if (crop.progressUI != null)
            {
                ReturnToPoolPrefab(crop.progressUI.gameObject, progressUIPrefab);
            }

            tile.crop = null;
        }

        if (tile.instance != null)
        {
            ReturnToPoolPrefab(tile.instance, tile.sourcePrefab);
        }

        tile.active = false;
        tile.instance = null;
        tile.sourcePrefab = null;
        tile.renderer = null;
        tile.type = TileType.Empty;
        tile.tileSet = null;
        tile.structureSet = null;
        tile.structureIndex = null;
        tile.isWatered = false;
        tile.waterTimer = 0f;
    }

    void ClearAllTileStates()
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            ClearTileState(tiles[x, y]);
        }

        activeCropTiles.Clear();
        wateredTiles.Clear();
    }

    void UpdateTrackedCollections(int x, int y, Tile tile)
    {
        Vector2Int pos = new Vector2Int(x, y);

        UpdateTrackedCollection(activeCropTiles, pos, tile.crop != null);
        UpdateTrackedCollection(wateredTiles, pos, tile.isWatered);
    }

    void UpdateTrackedCollection(List<Vector2Int> collection, Vector2Int pos, bool shouldContain)
    {
        bool contains = collection.Contains(pos);

        if (shouldContain && !contains)
        {
            collection.Add(pos);
        }
        else if (!shouldContain && contains)
        {
            collection.Remove(pos);
        }
    }

    void NotifyTileStateChanged(int x, int y)
    {
        TileStateChanged?.Invoke(CaptureTileState(x, y));
    }

    void UpdateCropPresentation(CropInstance crop)
    {
        if (crop == null) return;

        float t = 0f;

        switch (crop.state)
        {
            case CropState.Seed:
                t = crop.data.seedToGrowingTime > 0f ? crop.timer / crop.data.seedToGrowingTime : 0f;
                break;

            case CropState.Growing:
            case CropState.ReGrowing:
                t = crop.data.growingToReadyTime > 0f ? crop.timer / crop.data.growingToReadyTime : 0f;
                break;

            case CropState.Ready:
                t = 1f;
                break;
        }

        t = Mathf.Clamp01(t);

        if (crop.visual != null && crop.state == CropState.Growing)
        {
            float scale = Mathf.Lerp(0.5f, 1f, Mathf.SmoothStep(0, 1, t));
            crop.visual.transform.localScale = Vector3.one * scale;
        }

        if (crop.progressUI != null)
        {
            crop.progressUI.SetProgress(t);
        }
    }

    float GetCropStateDuration(CropInstance crop)
    {
        if (crop == null || crop.data == null)
            return 0f;

        switch (crop.state)
        {
            case CropState.Seed:
                return crop.data.seedToGrowingTime;
            case CropState.Growing:
            case CropState.ReGrowing:
                return crop.data.growingToReadyTime;
            default:
                return 0f;
        }
    }
}
