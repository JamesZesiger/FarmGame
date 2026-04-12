using Unity.Netcode;
using UnityEngine;

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
            grid.CaptureAllTileStates(),
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
        ResolveGrid();
        if (grid == null) return;

        Debug.Log($"Server: Tilling tile at {x}, {y}");
        grid.TryTillTile(x, y);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UntillServerRpc(int x, int y)
    {
        ResolveGrid();
        if (grid == null) return;

        grid.TryUntillTile(x, y);
    }

    [ServerRpc(RequireOwnership = false)]
    public void WaterServerRpc(int x, int y)
    {
        ResolveGrid();
        if (grid == null) return;

        grid.WaterTile(x, y);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlantServerRpc(int x, int y, string cropName)
    {
        ResolveGrid();
        if (grid == null) return;
        CropData cropData = grid.GetCropData(cropName);
        if (cropData == null) return;

        grid.PlantCrop(x, y, cropData);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HarvestServerRpc(int x, int y, ServerRpcParams rpcParams = default)
    {
        ResolveGrid();
        if (grid == null) return;

        ulong senderId = rpcParams.Receive.SenderClientId;

        Inventory inventory = ResolveInventory(senderId);
        if (inventory == null) return;

        grid.TryHarvest(x, y, inventory);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveStructureServerRpc(int x, int y)
    {
        ResolveGrid();
        if (grid == null) return;

        grid.RemoveStructure(x, y);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlaceStructureServerRpc(int x, int y, int structureIndex)
    {
        ResolveGrid();
        if (grid == null) return;

        grid.TryPlaceStructure(x, y, structureIndex);
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

    void ResolveGrid()
    {
        if (grid == null)
        {
            grid = FindFirstObjectByType<FarmGrid>();
            Debug.Log($"Resolved grid: {grid != null}");
        }
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