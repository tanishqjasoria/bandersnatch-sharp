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
        FrE.SetElement((ulong) 1),
        FrE.SetElement((ulong) 2),
        FrE.SetElement((ulong) 3),
        FrE.SetElement((ulong) 4),
        FrE.SetElement((ulong) 5),
        FrE.SetElement((ulong) 6),
        FrE.SetElement((ulong) 7),
        FrE.SetElement((ulong) 8),
        FrE.SetElement((ulong) 9),
        FrE.SetElement((ulong) 10),
        FrE.SetElement((ulong) 11),
        FrE.SetElement((ulong) 12),
        FrE.SetElement((ulong) 13),
        FrE.SetElement((ulong) 14),
        FrE.SetElement((ulong) 15),
        FrE.SetElement((ulong) 16),
        FrE.SetElement((ulong) 17),
        FrE.SetElement((ulong) 18),
        FrE.SetElement((ulong) 19),
        FrE.SetElement((ulong) 20),
        FrE.SetElement((ulong) 21),
        FrE.SetElement((ulong) 22),
        FrE.SetElement((ulong) 23),
        FrE.SetElement((ulong) 24),
        FrE.SetElement((ulong) 25),
        FrE.SetElement((ulong) 26),
        FrE.SetElement((ulong) 27),
        FrE.SetElement((ulong) 28),
        FrE.SetElement((ulong) 29),
        FrE.SetElement((ulong) 30),
        FrE.SetElement((ulong) 31),
        FrE.SetElement((ulong) 32),
    };


    [Test]
    public void TestBasicIpaProof()
    {
        Fr[] domain = new FrE[256];
        for (int i = 0; i < 256; i++)
        {
            domain[i] = FrE.SetElement((ulong)i);
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

        Fr inputPoint = FrE.SetElement((ulong)2101);
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
            FrE.SetElement((ulong) 1),
            FrE.SetElement((ulong) 2),
            FrE.SetElement((ulong) 3),
            FrE.SetElement((ulong) 4),
            FrE.SetElement((ulong) 5),
        };

        Fr[] b =
        {
            FrE.SetElement((ulong) 10),
            FrE.SetElement((ulong) 12),
            FrE.SetElement((ulong) 13),
            FrE.SetElement((ulong) 14),
            FrE.SetElement((ulong) 15),
        };

        Fr expectedResult = FrE.SetElement((ulong)204);

        Fr gotResult = IPA.InnerProduct(a, b);
        Assert.IsTrue(gotResult.Equals(expectedResult));
    }
}
