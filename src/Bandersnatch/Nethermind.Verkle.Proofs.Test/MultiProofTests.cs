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

public class MultiProofTests
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
    public void TestBasicMultiProof()
    {
        List<Fr> polyEvalA = new();
        List<Fr> polyEvalB = new();

        for (int i = 0; i < 8; i++)
        {
            polyEvalA.AddRange(_poly);
            polyEvalB.AddRange(_poly.Reverse());
        }
        CRS crs = CRS.Default();
        Banderwagon cA = crs.Commit(polyEvalA.ToArray());
        Banderwagon cB = crs.Commit(polyEvalB.ToArray());

        Fr[] zs =
        {
            Fr.Zero,
            Fr.Zero
        };
        Fr[] ys = { FrE.SetElement((ulong)1), FrE.SetElement((ulong)32) };
        Fr[][] fs =
        {
            polyEvalA.ToArray(), polyEvalB.ToArray()
        };
        ;
        Banderwagon[] cs =
        {
            cA, cB
        };

        Fr[] domain = new FrE[256];
        for (int i = 0; i < 256; i++)
        {
            domain[i] = FrE.SetElement((ulong)i);
        }

        MultiProofProverQuery queryA = new MultiProofProverQuery(new LagrangeBasis(fs[0], domain), cs[0], zs[0], ys[0]);
        MultiProofProverQuery queryB = new MultiProofProverQuery(new LagrangeBasis(fs[1], domain), cs[1], zs[1], ys[1]);

        MultiProof multiproof = new MultiProof(domain, crs);

        Transcript proverTranscript = new Transcript("test");
        MultiProofProverQuery[] queries =
        {
            queryA, queryB
        };
        MultiProofStruct proof = multiproof.MakeMultiProof(proverTranscript, queries);
        Fr pChallenge = proverTranscript.ChallengeScalar("state");

        Assert.IsTrue(Convert.ToHexString(pChallenge.ToBytes()).ToLower()
            .SequenceEqual("eee8a80357ff74b766eba39db90797d022e8d6dee426ded71234241be504d519"));

        Transcript verifierTranscript = new Transcript("test");
        MultiProofVerifierQuery queryAx = new MultiProofVerifierQuery(cs[0], zs[0], ys[0]);
        MultiProofVerifierQuery queryBx = new MultiProofVerifierQuery(cs[1], zs[1], ys[1]);

        MultiProofVerifierQuery[] queriesX =
        {
            queryAx, queryBx
        };
        bool ok = multiproof.CheckMultiProof(verifierTranscript, queriesX, proof);
        Assert.IsTrue(ok);

        Fr vChallenge = verifierTranscript.ChallengeScalar("state");
        Assert.IsTrue(vChallenge.Equals(pChallenge));
    }
}
