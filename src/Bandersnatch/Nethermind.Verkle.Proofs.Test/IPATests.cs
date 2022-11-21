using System;
using System.Collections.Generic;
using System.Linq;
using Nethermind.Field;
using Nethermind.Int256;
using Nethermind.MontgomeryField;
using Nethermind.Verkle.Curve;
using Nethermind.Verkle.Polynomial;
using NUnit.Framework;

namespace Nethermind.Verkle.Proofs.Test;
using Fr = FrE;

public class IPATests
{
    private readonly Fr[] _poly =
    {
        new Fr((ulong) 1),
        new Fr((ulong) 2),
        new Fr((ulong) 3),
        new Fr((ulong) 4),
        new Fr((ulong) 5),
        new Fr((ulong) 6),
        new Fr((ulong) 7),
        new Fr((ulong) 8),
        new Fr((ulong) 9),
        new Fr((ulong) 10),
        new Fr((ulong) 11),
        new Fr((ulong) 12),
        new Fr((ulong) 13),
        new Fr((ulong) 14),
        new Fr((ulong) 15),
        new Fr((ulong) 16),
        new Fr((ulong) 17),
        new Fr((ulong) 18),
        new Fr((ulong) 19),
        new Fr((ulong) 20),
        new Fr((ulong) 21),
        new Fr((ulong) 22),
        new Fr((ulong) 23),
        new Fr((ulong) 24),
        new Fr((ulong) 25),
        new Fr((ulong) 26),
        new Fr((ulong) 27),
        new Fr((ulong) 28),
        new Fr((ulong) 29),
        new Fr((ulong) 30),
        new Fr((ulong) 31),
        new Fr((ulong) 32),
    };


    [Test]
    public void TestBasicIpaProof()
    {
        Fr[] domain = new Fr[256];
        for (int i = 0; i < 256; i++)
        {
            domain[i] = new Fr((ulong)i);
        }

        PreComputeWeights weights = PreComputeWeights.Init(domain);

        List<Fr> lagrangePoly = new();

        for (int i = 0; i < 8; i++)
        {
            lagrangePoly.AddRange(_poly);
        }

        CRS crs = CRS.Default();
        Banderwagon commitment = crs.Commit(lagrangePoly.ToArray());

        Assert.IsTrue(Convert.ToHexString(commitment.ToBytes()).ToLower()
            .SequenceEqual("1b9dff8f5ebbac250d291dfe90e36283a227c64b113c37f1bfb9e7a743cdb128"));

        Transcript proverTranscript = new Transcript("test");

        Fr inputPoint = new Fr((ulong)2101);
        Fr[] b = weights.BarycentricFormulaConstants(inputPoint);
        ProverQuery query = new ProverQuery(lagrangePoly.ToArray(), commitment, inputPoint, b);

        byte[] hash =
        {
            59, 242, 0, 139, 181, 46, 10, 203, 105, 140, 230, 43, 108, 173, 120, 136, 17, 42, 116, 137, 73, 212, 87,
            150, 5, 145, 25, 202, 179, 251, 7, 191
        };
        List<byte> cache = new();
        foreach (Fr i in lagrangePoly)
        {
            cache.AddRange(i.ToBytes().ToArray());
        }
        cache.AddRange(commitment.ToBytes());
        cache.AddRange(inputPoint.ToBytes().ToArray());
        foreach (Fr i in b)
        {
            cache.AddRange(i.ToBytes().ToArray());
        }

        (Fr outputPoint, ProofStruct proof) = IPA.MakeIpaProof(crs, proverTranscript, query);
        Fr pChallenge = proverTranscript.ChallengeScalar("state");

        Assert.IsTrue(Convert.ToHexString(pChallenge.ToBytes()).ToLower()
            .SequenceEqual("0a81881cbfd7d7197a54ebd67ed6a68b5867f3c783706675b34ece43e85e7306"));

        Transcript verifierTranscript = new Transcript("test");

        VerifierQuery queryX = new VerifierQuery(commitment, inputPoint, b, outputPoint, proof);

        bool ok = IPA.CheckIpaProof(crs, verifierTranscript, queryX);

        Assert.IsTrue(ok);
    }

    [Test]
    public void TestInnerProduct()
    {
        Fr[] a =
        {
            new Fr((ulong) 1),
            new Fr((ulong) 2),
            new Fr((ulong) 3),
            new Fr((ulong) 4),
            new Fr((ulong) 5),
        };

        Fr[] b =
        {
            new Fr((ulong) 10),
            new Fr((ulong) 12),
            new Fr((ulong) 13),
            new Fr((ulong) 14),
            new Fr((ulong) 15),
        };

        Fr expectedResult = new Fr((ulong)204);

        Fr gotResult = IPA.InnerProduct(a, b);
        Assert.IsTrue(gotResult.Equals(expectedResult));
    }
}
