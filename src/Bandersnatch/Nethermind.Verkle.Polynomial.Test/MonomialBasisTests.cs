using Nethermind.Field;
using Nethermind.Int256;
using Nethermind.MontgomeryField;
using Nethermind.Verkle.Curve;
using NUnit.Framework;

namespace Nethermind.Verkle.Polynomial.Test;
using Fr = FrE;

public class MonomialBasisTests
{
    [Test]
    public void test_vanishing_poly()
    {
        Fr[] xs = new[]
        {
            new Fr((ulong)0),
            new Fr((ulong)1),
            new Fr((ulong)2),
            new Fr((ulong)3),
            new Fr((ulong)4),
            new Fr((ulong)5)
        };

        MonomialBasis z = MonomialBasis.VanishingPoly(xs);

        foreach (Fr x in xs)
        {
            Assert.IsTrue(z.Evaluate(x).IsZero);
        }
    }

    [Test]
    public void test_poly_div()
    {
        Fr[] aL = new[]
        {
            new Fr((ulong)2),
            new Fr((ulong)3),
            new Fr((ulong)1),
        };
        MonomialBasis a = new MonomialBasis(aL);
        Fr[] bL = new[]
        {
            new Fr((ulong)1),
            new Fr((ulong)1),
        };
        MonomialBasis b = new MonomialBasis(bL);

        MonomialBasis result = a / b;
        Assert.IsTrue(result._coeffs[0].Equals(new Fr((ulong)2)));
        Assert.IsTrue(result._coeffs[1].Equals(new Fr((ulong)1)));
    }

    [Test]
    public void test_derivative()
    {
        Fr[] aL = new[]
        {
            new Fr((ulong)9),
            new Fr((ulong)20),
            new Fr((ulong)10),
            new Fr((ulong)5),
            new Fr((ulong)6),
        };
        MonomialBasis a = new MonomialBasis(aL);
        Fr[] bL = new[]
        {
            new Fr((ulong)20),
            new Fr((ulong)20),
            new Fr((ulong)15),
            new Fr((ulong)24),
        };
        MonomialBasis b = new MonomialBasis(bL);

        MonomialBasis gotAPrime = MonomialBasis.FormalDerivative(a);
        for (int i = 0; i < gotAPrime.Length(); i++)
        {
            Assert.IsTrue(b._coeffs[i].Equals(gotAPrime._coeffs[i]));
        }
    }
}
