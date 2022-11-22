using Nethermind.Field;
using Nethermind.Int256;
using Nethermind.MontgomeryField;
using Nethermind.Verkle.Curve;
using NUnit.Framework;

namespace Nethermind.Verkle.Polynomial.Test;
using Fr = FrE;

public class LagrangeBasisTests
{
    [Test]
    public void test_add_sub()
    {
        Fr[] domain = new[]
        {
            FrE.SetElement((ulong)0),
            FrE.SetElement((ulong)1),
            FrE.SetElement((ulong)2),
            FrE.SetElement((ulong)3),
            FrE.SetElement((ulong)4),
            FrE.SetElement((ulong)5)
        };

        Fr[] domainSq = new[]
        {
            FrE.SetElement((ulong)0),
            FrE.SetElement((ulong)1),
            FrE.SetElement((ulong)4),
            FrE.SetElement((ulong)9),
            FrE.SetElement((ulong)16),
            FrE.SetElement((ulong)25)
        };

        Fr[] domain_2 = new[]
        {
            FrE.SetElement((ulong)2),
            FrE.SetElement((ulong)3),
            FrE.SetElement((ulong)4),
            FrE.SetElement((ulong)5),
            FrE.SetElement((ulong)6),
            FrE.SetElement((ulong)7)
        };

        LagrangeBasis a = new LagrangeBasis(domainSq, domain);
        LagrangeBasis b = new LagrangeBasis(domain_2, domain);

        Fr[] expected = new[]
        {
            FrE.SetElement((ulong)2),
            FrE.SetElement((ulong)4),
            FrE.SetElement((ulong)8),
            FrE.SetElement((ulong)14),
            FrE.SetElement((ulong)22),
            FrE.SetElement((ulong)32)
        };
        LagrangeBasis ex = new LagrangeBasis(expected, domain);
        LagrangeBasis result = a + b;

        for (int i = 0; i < ex.Evaluations.Length; i++)
        {
            Assert.IsTrue(ex.Evaluations[i].Equals(result.Evaluations[i]));
        }
        ex -= b;
        for (int i = 0; i < ex.Evaluations.Length; i++)
        {
            Assert.IsTrue(ex.Evaluations[i].Equals(a.Evaluations[i]));
        }
    }

    [Test]
    public void test_mul()
    {
        Fr[] domain = new[]
        {
            FrE.SetElement((ulong)0),
            FrE.SetElement((ulong)1),
            FrE.SetElement((ulong)2),
            FrE.SetElement((ulong)3),
            FrE.SetElement((ulong)4),
            FrE.SetElement((ulong)5)
        };

        Fr[] domainSq = new[]
        {
            FrE.SetElement((ulong)0),
            FrE.SetElement((ulong)1),
            FrE.SetElement((ulong)4),
            FrE.SetElement((ulong)9),
            FrE.SetElement((ulong)16),
            FrE.SetElement((ulong)25)
        };
        Fr[] domainPow4 = new[]
        {
            FrE.SetElement((ulong)0),
            FrE.SetElement((ulong)1),
            FrE.SetElement((ulong)16),
            FrE.SetElement((ulong)81),
            FrE.SetElement((ulong)256),
            FrE.SetElement((ulong)625)
        };


        LagrangeBasis a = new LagrangeBasis(domainSq, domain);
        LagrangeBasis result = a * a;

        LagrangeBasis ex = new LagrangeBasis(domainPow4, domain);

        for (int i = 0; i < ex.Evaluations.Length; i++)
        {
            Assert.IsTrue(ex.Evaluations[i].Equals(result.Evaluations[i]));
        }
    }

    [Test]
    public void test_scale()
    {
        Fr[] domain = new[]
        {
            FrE.SetElement((ulong)0),
            FrE.SetElement((ulong)1),
            FrE.SetElement((ulong)2),
            FrE.SetElement((ulong)3),
            FrE.SetElement((ulong)4),
            FrE.SetElement((ulong)5)
        };

        Fr[] domainSq = new[]
        {
            FrE.SetElement((ulong)0),
            FrE.SetElement((ulong)1),
            FrE.SetElement((ulong)4),
            FrE.SetElement((ulong)9),
            FrE.SetElement((ulong)16),
            FrE.SetElement((ulong)25)
        };

        Fr constant = FrE.SetElement((ulong)10);

        LagrangeBasis a = new LagrangeBasis(domainSq, domain);
        LagrangeBasis result = a * constant;

        Fr[] expected = new[]
        {
            FrE.SetElement((ulong)0),
            FrE.SetElement((ulong)10),
            FrE.SetElement((ulong)40),
            FrE.SetElement((ulong)90),
            FrE.SetElement((ulong)160),
            FrE.SetElement((ulong)250)
        };
        LagrangeBasis ex = new LagrangeBasis(expected, domain);

        for (int i = 0; i < ex.Evaluations.Length; i++)
        {
            Assert.IsTrue(ex.Evaluations[i].Equals(result.Evaluations[i]));
        }
    }

    [Test]
    public void test_interpolation()
    {
        Fr[] domain = new[]
        {
            FrE.SetElement((ulong)0),
            FrE.SetElement((ulong)1),
            FrE.SetElement((ulong)2),
            FrE.SetElement((ulong)3),
            FrE.SetElement((ulong)4),
            FrE.SetElement((ulong)5)
        };

        Fr[] domainSq = new[]
        {
            FrE.SetElement((ulong)0),
            FrE.SetElement((ulong)1),
            FrE.SetElement((ulong)4),
            FrE.SetElement((ulong)9),
            FrE.SetElement((ulong)16),
            FrE.SetElement((ulong)25)
        };

        LagrangeBasis xSquaredLagrange = new LagrangeBasis(domainSq, domain);
        MonomialBasis xSquaredCoeff = xSquaredLagrange.Interpolate();

        MonomialBasis expectedXSquaredCoeff = new MonomialBasis(
            new[] { Fr.Zero, Fr.Zero, Fr.One });

        for (int i = 0; i < expectedXSquaredCoeff._coeffs.Length; i++)
        {
            Assert.IsTrue(expectedXSquaredCoeff._coeffs[i].Equals(xSquaredCoeff._coeffs[i]));
        }
    }
}
