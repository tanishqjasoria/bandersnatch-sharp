using System.Diagnostics;
using Nethermind.Serialization.Rlp;

namespace Nethermind.Verkle.Tree;
using LeafStore = Dictionary<byte[], byte[]?>;
using SuffixStore = Dictionary<byte[], SuffixTree?>;
using BranchStore = Dictionary<byte[], InternalNode?>;


public class DiffLayer
{
    public Dictionary<long, byte[]> Forward { get; }
    public Dictionary<long, byte[]> Reverse { get; }
    public DiffLayer()
    {
        Forward = new Dictionary<long, byte[]>();
        Reverse = new Dictionary<long, byte[]>();
    }
}

public class VerkleDb : IVerkleDb
{
    private long FullStateBlock { get; set; }
    private MemoryStateDb Storage { get; }
    private MemoryStateDb Batch { get; }
    private MemoryStateDb Cache { get; }
    private DiffLayer History { get; }

    public VerkleDb()
    {
        Storage = new MemoryStateDb();
        Batch = new MemoryStateDb();
        Cache = new MemoryStateDb();
        History = new DiffLayer();
        FullStateBlock = 0;
    }

    public void InitRootHash()
    {
        Batch.BranchTable[Array.Empty<byte>()] = new BranchNode();
    }

    public byte[]? GetLeaf(byte[] key)
    {
        if (Cache.LeafTable.TryGetValue(key, out byte[]? value)) return value;
        if (Batch.LeafTable.TryGetValue(key, out value)) return value;
        return Storage.LeafTable.TryGetValue(key, out value) ? value : null;
    }

    public SuffixTree? GetStem(byte[] key)
    {
        if (Cache.StemTable.TryGetValue(key, out SuffixTree? value)) return value;
        if (Batch.StemTable.TryGetValue(key, out value)) return value;
        return Storage.StemTable.TryGetValue(key, out value) ? value : null;
    }

    public InternalNode? GetBranch(byte[] key)
    {
        if (Cache.BranchTable.TryGetValue(key, out InternalNode? value)) return value;
        if (Batch.BranchTable.TryGetValue(key, out value)) return value;
        return Storage.BranchTable.TryGetValue(key, out value) ? value : null;
    }

    public void SetLeaf(byte[] leafKey, byte[] leafValue)
    {
        Cache.LeafTable[leafKey] = leafValue;
        Batch.LeafTable[leafKey] = leafValue;
    }

    public void SetStem(byte[] stemKey, SuffixTree suffixTree)
    {
        Cache.StemTable[stemKey] = suffixTree;
        Batch.StemTable[stemKey] = suffixTree;
    }

    public void SetBranch(byte[] branchKey, InternalNode internalNodeValue)
    {
        Cache.BranchTable[branchKey] = internalNodeValue;
        Batch.BranchTable[branchKey] = internalNodeValue;
    }

    public void Flush(long blockNumber)
    {
        // we should not have any null values in the Batch db - because deletion of values from verkle tree is not allowed
        // nullable values are allowed in MemoryStateDb only for reverse diffs.
        MemoryStateDb reverseDiff = new MemoryStateDb();

        foreach (KeyValuePair<byte[], byte[]?> entry in Batch.LeafTable)
        {
            Debug.Assert(entry.Key is not null, "nullable value only for reverse diff");
            if (Storage.LeafTable.TryGetValue(entry.Key, out byte[]? node)) reverseDiff.LeafTable[entry.Key] = node;
            else reverseDiff.LeafTable[entry.Key] = null;

            Storage.LeafTable[entry.Key] = entry.Value;
        }

        foreach (KeyValuePair<byte[], SuffixTree?> entry in Batch.StemTable)
        {
            Debug.Assert(entry.Key is not null, "nullable value only for reverse diff");
            if (Storage.StemTable.TryGetValue(entry.Key, out SuffixTree? node)) reverseDiff.StemTable[entry.Key] = node;
            else reverseDiff.StemTable[entry.Key] = null;

            Storage.StemTable[entry.Key] = entry.Value;
        }

        foreach (KeyValuePair<byte[], InternalNode?> entry in Batch.BranchTable)
        {
            Debug.Assert(entry.Key is not null, "nullable value only for reverse diff");
            if (Storage.BranchTable.TryGetValue(entry.Key, out InternalNode? node)) reverseDiff.BranchTable[entry.Key] = node;
            else reverseDiff.BranchTable[entry.Key] = null;

            Storage.BranchTable[entry.Key] = entry.Value;
        }

        History.Forward[blockNumber] = Batch.Encode();
        History.Reverse[blockNumber] = reverseDiff.Encode();
        FullStateBlock = blockNumber;
    }

    public void ReverseState()
    {
        byte[] reverseDiffByte = History.Reverse[FullStateBlock];
        MemoryStateDb reverseDiff = MemoryStateDb.Decode(reverseDiffByte);

        foreach (KeyValuePair<byte[], byte[]?> entry in reverseDiff.LeafTable)
        {
            reverseDiff.LeafTable.TryGetValue(entry.Key, out byte[]? node);
            if (node is null)
            {
                Cache.LeafTable.Remove(entry.Key);
                Storage.LeafTable.Remove(entry.Key);
            }
            else
            {
                Cache.LeafTable[entry.Key] = node;
                Storage.LeafTable[entry.Key] = node;
            }
        }

        foreach (KeyValuePair<byte[], SuffixTree?> entry in reverseDiff.StemTable)
        {
            reverseDiff.StemTable.TryGetValue(entry.Key, out SuffixTree? node);
            if (node is null)
            {
                Cache.StemTable.Remove(entry.Key);
                Storage.StemTable.Remove(entry.Key);
            }
            else
            {
                Cache.StemTable[entry.Key] = node;
                Storage.StemTable[entry.Key] = node;
            }
        }

        foreach (KeyValuePair<byte[], InternalNode?> entry in reverseDiff.BranchTable)
        {
            reverseDiff.BranchTable.TryGetValue(entry.Key, out InternalNode? node);
            if (node is null)
            {
                Cache.BranchTable.Remove(entry.Key);
                Storage.BranchTable.Remove(entry.Key);
            }
            else
            {
                Cache.BranchTable[entry.Key] = node;
                Storage.BranchTable[entry.Key] = node;
            }
        }
    }
}

public interface IVerkleDb
{
    void InitRootHash();
    byte[]? GetLeaf(byte[] key);
    SuffixTree? GetStem(byte[] key);
    InternalNode? GetBranch(byte[] key);
    void SetLeaf(byte[] leafKey, byte[] leafValue);
    void SetStem(byte[] stemKey, SuffixTree suffixTree);
    void SetBranch(byte[] branchKey, InternalNode internalNodeValue);
    void Flush(long blockNumber);
    void ReverseState();
}
