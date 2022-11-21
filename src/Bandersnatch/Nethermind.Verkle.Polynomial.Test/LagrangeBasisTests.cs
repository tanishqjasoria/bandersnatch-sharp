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
            new Fr((ulong)0),
            new Fr((ulong)1),
            new Fr((ulong)2),
            new Fr((ulong)3),
            new Fr((ulong)4),
            new Fr((ulong)5)
        };

        Fr[] domainSq = new[]
        {
            new Fr((ulong)0),
            new Fr((ulong)1),
            new Fr((ulong)4),
            new Fr((ulong)9),
            new Fr((ulong)16),
            new Fr((ulong)25)
        };

        Fr[] domain_2 = new[]
        {
            new Fr((ulong)2),
            new Fr((ulong)3),
            new Fr((ulong)4),
            new Fr((ulong)5),
            new Fr((ulong)6),
            new Fr((ulong)7)
        };

        LagrangeBasis a = new LagrangeBasis(domainSq, domain);
        LagrangeBasis b = new LagrangeBasis(domain_2, domain);

        Fr[] expected = new[]
        {
            new Fr((ulong)2),
            new Fr((ulong)4),
            new Fr((ulong)8),
            new Fr((ulong)14),
            new Fr((ulong)22),
            new Fr((ulong)32)
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
            new Fr((ulong)0),
            new Fr((ulong)1),
            new Fr((ulong)2),
            new Fr((ulong)3),
            new Fr((ulong)4),
            new Fr((ulong)5)
        };

        Fr[] domainSq = new[]
        {
            new Fr((ulong)0),
            new Fr((ulong)1),
            new Fr((ulong)4),
            new Fr((ulong)9),
            new Fr((ulong)16),
            new Fr((ulong)25)
        };
        Fr[] domainPow4 = new[]
        {
            new Fr((ulong)0),
            new Fr((ulong)1),
            new Fr((ulong)16),
            new Fr((ulong)81),
            new Fr((ulong)256),
            new Fr((ulong)625)
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
            new Fr((ulong)0),
            new Fr((ulong)1),
            new Fr((ulong)2),
            new Fr((ulong)3),
            new Fr((ulong)4),
            new Fr((ulong)5)
        };

        Fr[] domainSq = new[]
        {
            new Fr((ulong)0),
            new Fr((ulong)1),
            new Fr((ulong)4),
            new Fr((ulong)9),
            new Fr((ulong)16),
            new Fr((ulong)25)
        };

        Fr constant = new Fr((ulong)10);

        LagrangeBasis a = new LagrangeBasis(domainSq, domain);
        LagrangeBasis result = a * constant;

        Fr[] expected = new[]
        {
            new Fr((ulong)0),
            new Fr((ulong)10),
            new Fr((ulong)40),
            new Fr((ulong)90),
            new Fr((ulong)160),
            new Fr((ulong)250)
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
            new Fr((ulong)0),
            new Fr((ulong)1),
            new Fr((ulong)2),
            new Fr((ulong)3),
            new Fr((ulong)4),
            new Fr((ulong)5)
        };

        Fr[] domainSq = new[]
        {
            new Fr((ulong)0),
            new Fr((ulong)1),
            new Fr((ulong)4),
            new Fr((ulong)9),
            new Fr((ulong)16),
            new Fr((ulong)25)
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
