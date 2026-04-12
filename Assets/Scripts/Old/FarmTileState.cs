using Unity.Collections;
using Unity.Netcode;

public struct FarmTileState : INetworkSerializable
{
    public int x;
    public int y;
    public TileType tileType;
    public bool isWatered;
    public float waterTimer;
    public bool hasStructure;
    public int structureIndex;
    public bool hasCrop;
    public FixedString64Bytes cropName;
    public CropState cropState;
    public float cropTimer;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int tileTypeValue = (int)tileType;
        int cropStateValue = (int)cropState;

        serializer.SerializeValue(ref x);
        serializer.SerializeValue(ref y);
        serializer.SerializeValue(ref tileTypeValue);
        serializer.SerializeValue(ref isWatered);
        serializer.SerializeValue(ref waterTimer);
        serializer.SerializeValue(ref hasStructure);
        serializer.SerializeValue(ref structureIndex);
        serializer.SerializeValue(ref hasCrop);
        serializer.SerializeValue(ref cropName);
        serializer.SerializeValue(ref cropStateValue);
        serializer.SerializeValue(ref cropTimer);

        tileType = (TileType)tileTypeValue;
        cropState = (CropState)cropStateValue;
    }
}
