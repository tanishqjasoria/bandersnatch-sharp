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
        Fr[] ys = { new Fr((ulong)1), new Fr((ulong)32) };
        Fr[][] fs =
        {
            polyEvalA.ToArray(), polyEvalB.ToArray()
        };
        ;
        Banderwagon[] cs =
        {
            cA, cB
        };

        Fr[] domain = new Fr[256];
        for (int i = 0; i < 256; i++)
        {
            domain[i] = new Fr((ulong)i);
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
