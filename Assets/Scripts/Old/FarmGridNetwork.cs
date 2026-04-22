using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FarmGridNetwork : NetworkBehaviour
{
    public static FarmGridNetwork Instance { get; private set; }

    public FarmGrid grid;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ResolveGrid();
    }

    public override void OnNetworkSpawn()
    {
        ResolveGrid();
        SubscribeToGrid();

        if (grid != null)
        {
            grid.SetSimulationEnabled(IsServer);
        }

        Debug.Log($"FarmGridNetwork OnNetworkSpawn: IsServer={IsServer}, IsClient={IsClient}, IsHost={IsHost}");

        if (IsClient && !IsServer)
        {
            RequestFullSyncServerRpc();
        }
    }

    public override void OnNetworkDespawn()
    {
        UnsubscribeFromGrid();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestFullSyncServerRpc(ServerRpcParams rpcParams = default)
    {
        ResolveGrid();
        if (grid == null) return;

        ulong senderId = rpcParams.Receive.SenderClientId;

        ApplyFullStateClientRpc(
            grid.CaptureActiveTileStates(),
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { senderId }
                }
            }
        );
    }

    [ServerRpc(RequireOwnership = false)]
    public void TillServerRpc(int x, int y)
    {
        TillOnServer(x, y);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UntillServerRpc(int x, int y)
    {
        UntillOnServer(x, y);
    }

    [ServerRpc(RequireOwnership = false)]
    public void WaterServerRpc(int x, int y)
    {
        WaterOnServer(x, y);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlantServerRpc(int x, int y, string cropName)
    {
        PlantOnServer(x, y, cropName);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HarvestServerRpc(int x, int y, ServerRpcParams rpcParams = default)
    {
        Inventory inventory = ResolveInventory(rpcParams.Receive.SenderClientId);
        HarvestOnServer(x, y, inventory, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveStructureServerRpc(int x, int y)
    {
        RemoveStructureOnServer(x, y);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlaceStructureServerRpc(int x, int y, int structureIndex)
    {
        PlaceStructureOnServer(x, y, structureIndex);
    }

    public bool TillOnServer(int x, int y)
    {
        ResolveGrid();
        if (grid == null) return false;

        Debug.Log($"Server: Tilling tile at {x}, {y}");
        return grid.TryTillTile(x, y);
    }

    public bool UntillOnServer(int x, int y)
    {
        ResolveGrid();
        if (grid == null) return false;

        return grid.TryUntillTile(x, y);
    }

    public bool WaterOnServer(int x, int y)
    {
        ResolveGrid();
        if (grid == null) return false;

        return grid.WaterTile(x, y);
    }

    public bool PlantOnServer(int x, int y, string cropName)
    {
        ResolveGrid();
        if (grid == null) return false;

        CropData cropData = grid.GetCropData(cropName);
        if (cropData == null) return false;

        return grid.PlantCrop(x, y, cropData);
    }

    public bool HarvestOnServer(int x, int y, Inventory inventory, ulong targetClientId)
    {
        ResolveGrid();
        Debug.Log("harvest on server:1");
        if (grid == null) return false;
    Debug.Log("harvest on server:2");
        if (inventory == null) return false;
Debug.Log("harvest on server:3");
        Tile tile = grid.GetTile(x, y);
        CropInstance crop = tile != null ? tile.crop : null;
        Item harvestedItem = crop != null && crop.data != null ? crop.data.item : null;
Debug.Log("harvest on server:4");
        if (!grid.TryHarvest(x, y, inventory)) return false;
Debug.Log("harvest on server:5");
        if (harvestedItem != null)
        {
            Debug.Log("harvest on server:6");
            SyncHarvestedItemClientRpc(
                harvestedItem.itemName,
                1,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { targetClientId }
                    }
                }
            );
            Debug.Log("harvest on server:7");
        }

        return true;
    }

    public bool RemoveStructureOnServer(int x, int y)
    {
        ResolveGrid();
        Debug.Log("1");
        if (grid == null) return false;
        Debug.Log("2");
        return grid.RemoveStructure(x, y);
    }

    public bool PlaceStructureOnServer(int x, int y, int structureIndex)
    {
        ResolveGrid();
        if (grid == null) return false;

        return grid.TryPlaceStructure(x, y, structureIndex);
    }

    public void LoadSceneForAll(string sceneName)
    {
        if (!IsServer || string.IsNullOrWhiteSpace(sceneName)) return;

        LoadSceneClientRpc(sceneName);
    }

    [ClientRpc]
    void ApplyTileStateClientRpc(FarmTileState state)
    {
        if (IsServer) return;

        ResolveGrid();
        if (grid == null) return;

        Debug.Log($"Client: Applying tile state at {state.x}, {state.y}");
        grid.ApplyTileState(state);
    }

    [ClientRpc]
    void ApplyFullStateClientRpc(FarmTileState[] states, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return;

        ResolveGrid();
        if (grid == null) return;

        grid.ApplyFullState(states);
    }

    [ClientRpc]
    void SyncHarvestedItemClientRpc(string itemName, int amount, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return;
        if (string.IsNullOrWhiteSpace(itemName) || amount <= 0) return;

        Inventory inventory = ResolveInventory(NetworkManager.LocalClientId);
        if (inventory == null) return;

        Item item = inventory.GetItemByName(itemName);
        if (item == null) return;

        inventory.AddItem(item, amount);

        PlayerController player = PlayerController.LocalPlayer;
        if (player != null)
        {
            player.UIManager?.RefreshAll();
        }
    }

    [ClientRpc]
    void LoadSceneClientRpc(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;
        if (SceneManager.GetActiveScene().name == sceneName) return;

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    void ResolveGrid()
    {
        if (grid == null)
        {
            grid = FindFirstObjectByType<FarmGrid>();
            Debug.Log($"Resolved grid: {grid != null}");
        }
    }

    public static FarmGridNetwork ResolveActive()
    {
        if (Instance != null)
        {
            return Instance;
        }

        return FindFirstObjectByType<FarmGridNetwork>();
    }

    void SubscribeToGrid()
    {
        if (grid == null || !IsServer) return;

        grid.TileStateChanged -= OnTileStateChanged;
        grid.TileStateChanged += OnTileStateChanged;
    }

    void UnsubscribeFromGrid()
    {
        if (grid == null) return;

        grid.TileStateChanged -= OnTileStateChanged;
    }

    void OnTileStateChanged(FarmTileState state)
    {
        ApplyTileStateClientRpc(state);
    }

    Inventory ResolveInventory(ulong clientId)
    {
        for (int i = 0; i < PlayerController.ActivePlayers.Count; i++)
        {
            PlayerController player = PlayerController.ActivePlayers[i];
            if (player != null && player.PlayerId == clientId)
            {
                return player.PlayerInventory;
            }
        }

        return null;
    }
}
