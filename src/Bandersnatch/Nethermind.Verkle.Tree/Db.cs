// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

using System.IO.Abstractions;
using Nethermind.Config;
using Nethermind.Db;
using Nethermind.Db.Rocks;
using Nethermind.Db.Rocks.Config;
using Nethermind.Utils;

namespace Nethermind.Verkle.Tree;

public enum DiagnosticMode
{
    None,
    [ConfigItem(Description = "Diagnostics mode which uses an in-memory DB")]
    MemDb,
    [ConfigItem(Description = "Diagnostics mode which uses a remote DB")]
    RpcDb,
    [ConfigItem(Description = "Diagnostics mode which uses a read-only DB")]
    ReadOnlyDb,
    [ConfigItem(Description = "Just scan rewards for blocks + genesis")]
    VerifyRewards,
    [ConfigItem(Description = "Just scan and sum supply on all accounts")]
    VerifySupply,
    [ConfigItem(Description = "Verifies if full state is stored")]
    VerifyTrie
}

public class DbFactory
{
    private static (IDbProvider DbProvider, RocksDbFactory RocksDbFactory, MemDbFactory MemDbFactory) InitDbApi(DiagnosticMode diagnosticMode, string baseDbPath, bool storeReceipts)
    {
        DbConfig dbConfig = new DbConfig();
        DisposableStack disposeStack = new();
        IDbProvider dbProvider;
        RocksDbFactory rocksDbFactory;
        MemDbFactory memDbFactory;
        switch (diagnosticMode)
        {
            case DiagnosticMode.ReadOnlyDb:
                DbProvider rocksDbProvider = new DbProvider(DbModeHint.Persisted);
                dbProvider = new ReadOnlyDbProvider(rocksDbProvider, storeReceipts); // ToDo storeReceipts as createInMemoryWriteStore - bug?
                disposeStack.Push(rocksDbProvider);
                rocksDbFactory = new RocksDbFactory(dbConfig, Path.Combine(baseDbPath, "debug"));
                memDbFactory = new MemDbFactory();
                break;
            case DiagnosticMode.MemDb:
                dbProvider = new DbProvider(DbModeHint.Mem);
                rocksDbFactory = new RocksDbFactory(dbConfig, Path.Combine(baseDbPath, "debug"));
                memDbFactory = new MemDbFactory();
                break;
            case DiagnosticMode.None:
            case DiagnosticMode.RpcDb:
            case DiagnosticMode.VerifyRewards:
            case DiagnosticMode.VerifySupply:
            case DiagnosticMode.VerifyTrie:
                throw new ArgumentException();
            default:
                dbProvider = new DbProvider(DbModeHint.Persisted);
                rocksDbFactory = new RocksDbFactory(dbConfig, baseDbPath);
                memDbFactory = new MemDbFactory();
                break;
        }

        return (dbProvider, rocksDbFactory, memDbFactory);
    }

    public static async void InitDatabase()
    {
        (IDbProvider dbProvider, RocksDbFactory rocksDbFactory, MemDbFactory memDbFactory) = InitDbApi(DiagnosticMode.None, "testDb", true);
        StandardDbInitializer dbInitializer = new StandardDbInitializer(dbProvider, rocksDbFactory, memDbFactory, new FileSystem(), false);
        await dbInitializer.InitStandardDbsAsync(true);
    }

}
