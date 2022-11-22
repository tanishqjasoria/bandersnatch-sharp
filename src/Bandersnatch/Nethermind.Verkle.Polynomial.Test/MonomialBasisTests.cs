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
            FrE.SetElement((ulong)0),
            FrE.SetElement((ulong)1),
            FrE.SetElement((ulong)2),
            FrE.SetElement((ulong)3),
            FrE.SetElement((ulong)4),
            FrE.SetElement((ulong)5)
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
            FrE.SetElement((ulong)2),
            FrE.SetElement((ulong)3),
            FrE.SetElement((ulong)1),
        };
        MonomialBasis a = new MonomialBasis(aL);
        Fr[] bL = new[]
        {
            FrE.SetElement((ulong)1),
            FrE.SetElement((ulong)1),
        };
        MonomialBasis b = new MonomialBasis(bL);

        MonomialBasis result = a / b;
        Assert.IsTrue(result._coeffs[0].Equals(FrE.SetElement((ulong)2)));
        Assert.IsTrue(result._coeffs[1].Equals(FrE.SetElement((ulong)1)));
    }

    [Test]
    public void test_derivative()
    {
        Fr[] aL = new[]
        {
            FrE.SetElement((ulong)9),
            FrE.SetElement((ulong)20),
            FrE.SetElement((ulong)10),
            FrE.SetElement((ulong)5),
            FrE.SetElement((ulong)6),
        };
        MonomialBasis a = new MonomialBasis(aL);
        Fr[] bL = new[]
        {
            FrE.SetElement((ulong)20),
            FrE.SetElement((ulong)20),
            FrE.SetElement((ulong)15),
            FrE.SetElement((ulong)24),
        };
        MonomialBasis b = new MonomialBasis(bL);

        MonomialBasis gotAPrime = MonomialBasis.FormalDerivative(a);
        for (int i = 0; i < gotAPrime.Length(); i++)
        {
            Assert.IsTrue(b._coeffs[i].Equals(gotAPrime._coeffs[i]));
        }
    }
}
