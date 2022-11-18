// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

namespace Nethermind.Verkle.Tree.VerkleStateDb;

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
