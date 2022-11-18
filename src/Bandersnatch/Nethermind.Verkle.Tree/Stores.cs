// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

using Nethermind.Serialization.Rlp;
using Nethermind.Utils.Extensions;
using Nethermind.Verkle.Curve;
using Nethermind.Verkle.Utils;

namespace Nethermind.Verkle.Tree;

using LeafStore = Dictionary<byte[], byte[]?>;
using SuffixStore = Dictionary<byte[], SuffixTree?>;
using BranchStore = Dictionary<byte[], InternalNode?>;

public class MemoryStateDb
{
    public Dictionary<byte[], byte[]?> LeafTable { get; }
    public Dictionary<byte[], SuffixTree?> StemTable { get; }
    public Dictionary<byte[], InternalNode?> BranchTable { get; }

    public MemoryStateDb()
    {
        LeafTable = new Dictionary<byte[], byte[]?>(Bytes.EqualityComparer);
        StemTable = new Dictionary<byte[], SuffixTree?>(Bytes.EqualityComparer);
        BranchTable = new Dictionary<byte[], InternalNode?>(Bytes.EqualityComparer);
    }

    public MemoryStateDb(LeafStore leafTable, SuffixStore stemTable, BranchStore branchTable)
    {
        LeafTable = leafTable;
        StemTable = stemTable;
        BranchTable = branchTable;
    }

    public byte[] Encode()
    {
        int contentLength = MemoryStateDbSerializer.Instance.GetLength(this, RlpBehaviors.None);
        RlpStream stream = new RlpStream(Rlp.LengthOfSequence(contentLength));
        stream.StartSequence(contentLength);
        MemoryStateDbSerializer.Instance.Encode(stream, this);
        return stream.Data?? throw new ArgumentException();
    }

    public static MemoryStateDb Decode(byte[] data)
    {
        RlpStream stream = data.AsRlpStream();
        stream.ReadSequenceLength();
        return MemoryStateDbSerializer.Instance.Decode(stream);
    }
}

public class SuffixTreeSerializer : IRlpStreamDecoder<SuffixTree>
{
    public static SuffixTreeSerializer Instance => new SuffixTreeSerializer();
    public int GetLength(SuffixTree item, RlpBehaviors rlpBehaviors)
    {
        return 31 + 32 + 32 + 32;
    }

    public int GetLength(SuffixTree item, RlpBehaviors rlpBehaviors, out int contentLength)
    {
        contentLength = GetLength(item, rlpBehaviors);
        return Rlp.LengthOfSequence(contentLength);
    }

    public SuffixTree Decode(RlpStream rlpStream, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
    {
        return new SuffixTree(
            rlpStream.Read(31).ToArray(),
            rlpStream.Read(32).ToArray(),
            rlpStream.Read(32).ToArray(),
            rlpStream.Read(32).ToArray());
    }
    public void Encode(RlpStream stream, SuffixTree item, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
    {
        stream.Write(item.Stem);
        stream.Write(item.C1.Point.ToBytes());
        stream.Write(item.C2.Point.ToBytes());
        stream.Write(item.ExtensionCommitment.Point.ToBytes());
    }
}

public class InternalNodeSerializer : IRlpStreamDecoder<InternalNode>
{
    public static InternalNodeSerializer Instance => new InternalNodeSerializer();
    public int GetLength(InternalNode item, RlpBehaviors rlpBehaviors)
    {
        return item.NodeType switch
        {
            NodeType.BranchNode => 32 + 1,
            NodeType.StemNode => 32 + 31 + 1,
            var _ => throw new ArgumentOutOfRangeException()
        };
    }

    public InternalNode Decode(RlpStream rlpStream, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
    {
        NodeType nodeType = (NodeType)rlpStream.ReadByte();
        switch (nodeType)
        {
            case NodeType.BranchNode:
                InternalNode node = new InternalNode(NodeType.BranchNode);
                node.UpdateCommitment(new Banderwagon(rlpStream.Read(32).ToArray()));
                return node;
            case NodeType.StemNode:
                return new InternalNode(NodeType.StemNode, rlpStream.Read(31).ToArray(), new Commitment(new Banderwagon(rlpStream.Read(32).ToArray())));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    public void Encode(RlpStream stream, InternalNode item, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
    {
        switch (item.NodeType)
        {
            case NodeType.BranchNode:
                stream.WriteByte((byte)NodeType.BranchNode);
                stream.Write(item._internalCommitment.Point.ToBytes());
                break;
            case NodeType.StemNode:
                stream.WriteByte((byte)NodeType.StemNode);
                stream.Write(item.Stem);
                stream.Write(item._internalCommitment.Point.ToBytes());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public class MemoryStateDbSerializer: IRlpStreamDecoder<MemoryStateDb>
{
    public static MemoryStateDbSerializer Instance => new MemoryStateDbSerializer();

    public int GetLength(MemoryStateDb item, RlpBehaviors rlpBehaviors)
    {
        int length = 0;
        length += Rlp.LengthOfSequence(LeafStoreSerializer.Instance.GetLength(item.LeafTable, RlpBehaviors.None));
        length += Rlp.LengthOfSequence(SuffixStoreSerializer.Instance.GetLength(item.StemTable, RlpBehaviors.None));
        length += Rlp.LengthOfSequence(BranchStoreSerializer.Instance.GetLength(item.BranchTable, RlpBehaviors.None));
        return length;
    }

    public int GetLength(MemoryStateDb item, RlpBehaviors rlpBehaviors, out int contentLength)
    {
        contentLength = GetLength(item, rlpBehaviors);
        return Rlp.LengthOfSequence(contentLength);
    }

    public MemoryStateDb Decode(RlpStream rlpStream, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
    {
        return new MemoryStateDb(
            LeafStoreSerializer.Instance.Decode(rlpStream),
            SuffixStoreSerializer.Instance.Decode(rlpStream),
            BranchStoreSerializer.Instance.Decode(rlpStream)
        );
    }
    public void Encode(RlpStream stream, MemoryStateDb item, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
    {
        LeafStoreSerializer.Instance.Encode(stream, item.LeafTable);
        SuffixStoreSerializer.Instance.Encode(stream, item.StemTable);
        BranchStoreSerializer.Instance.Encode(stream, item.BranchTable);
    }
}

public class LeafStoreSerializer: IRlpStreamDecoder<LeafStore>
{
    public static LeafStoreSerializer Instance => new LeafStoreSerializer();
    public int GetLength(LeafStore item, RlpBehaviors rlpBehaviors)
    {
        int length = Rlp.LengthOf(item.Count);
        foreach (KeyValuePair<byte[], byte[]?> pair in item)
        {
            length += Rlp.LengthOf(pair.Key);
            length += Rlp.LengthOf(pair.Value);
        }
        return length;
    }

    public LeafStore Decode(RlpStream rlpStream, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
    {
        LeafStore item = new LeafStore();
        int length = rlpStream.DecodeInt();
        for (int i = 0; i < length; i++)
        {
            item[rlpStream.DecodeByteArray()] = rlpStream.DecodeByteArray();
        }
        return item;
    }

    public void Encode(RlpStream stream, LeafStore item, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
    {
        stream.Encode(item.Count);
        foreach (KeyValuePair<byte[], byte[]?> pair in item)
        {
            stream.Encode(pair.Key);
            stream.Encode(pair.Value);
        }
    }
}

public class SuffixStoreSerializer: IRlpStreamDecoder<SuffixStore>
{
    private static SuffixTreeSerializer SuffixTreeSerializer => SuffixTreeSerializer.Instance;

    public static SuffixStoreSerializer Instance => new SuffixStoreSerializer();

    public int GetLength(SuffixStore item, RlpBehaviors rlpBehaviors)
    {
        int length = Rlp.LengthOf(item.Count);
        foreach (KeyValuePair<byte[], SuffixTree?> pair in item)
        {
            length += Rlp.LengthOf(pair.Key);
            length += pair.Value == null? Rlp.EmptyArrayByte: SuffixTreeSerializer.GetLength(pair.Value, RlpBehaviors.None);
        }
        return length;
    }

    public SuffixStore Decode(RlpStream rlpStream, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
    {
        SuffixStore item = new SuffixStore();
        int length = rlpStream.DecodeInt();
        for (int i = 0; i < length; i++)
        {
            byte[] key = rlpStream.DecodeByteArray();
            if (rlpStream.PeekNextItem().Length == 0)
            {
                item[key] = null;
                rlpStream.SkipItem();
            }
            else
            {
                item[key] = SuffixTreeSerializer.Decode(rlpStream);
            }
        }
        return item;
    }
    public void Encode(RlpStream stream, SuffixStore item, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
    {
        stream.Encode(item.Count);
        foreach (KeyValuePair<byte[], SuffixTree?> pair in item)
        {
            stream.Encode(pair.Key);
            if (pair.Value is null) stream.EncodeEmptyByteArray();
            else SuffixTreeSerializer.Encode(stream, pair.Value);
        }
    }
}


public class BranchStoreSerializer: IRlpStreamDecoder<BranchStore>
{
    private static InternalNodeSerializer InternalNodeSerializer => InternalNodeSerializer.Instance;

    public static BranchStoreSerializer Instance => new BranchStoreSerializer();
    public int GetLength(BranchStore item, RlpBehaviors rlpBehaviors)
    {
        int length = Rlp.LengthOf(item.Count);
        foreach (KeyValuePair<byte[], InternalNode?> pair in item)
        {
            length += Rlp.LengthOf(pair.Key);
            length += pair.Value == null? Rlp.EmptyArrayByte: InternalNodeSerializer.GetLength(pair.Value, RlpBehaviors.None);
        }
        return length;
    }

    public BranchStore Decode(RlpStream rlpStream, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
    {
        BranchStore item = new BranchStore();
        int length = rlpStream.DecodeInt();
        for (int i = 0; i < length; i++)
        {
            byte[] key = rlpStream.DecodeByteArray();
            if (rlpStream.PeekNextItem().Length == 0)
            {
                item[key] = null;
                rlpStream.SkipItem();
            }
            else
            {
                item[key] = InternalNodeSerializer.Decode(rlpStream);
            }
        }
        return item;
    }
    public void Encode(RlpStream stream, BranchStore item, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
    {
        stream.Encode(item.Count);
        foreach (KeyValuePair<byte[], InternalNode?> pair in item)
        {
            stream.Encode(pair.Key);
            if (pair.Value is null) stream.EncodeEmptyByteArray();
            else InternalNodeSerializer.Encode(stream, pair.Value);
        }
    }
}
