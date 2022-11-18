// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

namespace Nethermind.Verkle.Tree.VerkleStateDb;
using LeafStore = Dictionary<byte[], byte[]?>;
using SuffixStore = Dictionary<byte[], SuffixTree?>;
using BranchStore = Dictionary<byte[], InternalNode?>;


public interface IVerkleDb
{
    byte[]? GetLeaf(byte[] key);
    SuffixTree? GetStem(byte[] key);
    InternalNode? GetBranch(byte[] key);
    void SetLeaf(byte[] leafKey, byte[] leafValue);
    void SetStem(byte[] stemKey, SuffixTree suffixTree);
    void SetBranch(byte[] branchKey, InternalNode internalNodeValue);

    void BatchLeafInsert(IEnumerable<(byte[] key, byte[]? value)> keyLeaf);
    void BatchStemInsert(IEnumerable<(byte[] key, SuffixTree? value)> suffixLeaf);
    void BatchBranchInsert(IEnumerable<(byte[] key, InternalNode? value)> branchLeaf);
}
