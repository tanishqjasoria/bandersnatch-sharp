using Nethermind.Field;
using Nethermind.MontgomeryField;
using Nethermind.Verkle.Curve;

namespace Nethermind.Verkle.Polynomial;
using Fr = FrE;

public class MonomialBasis : IEqualityComparer<MonomialBasis>
{
    public readonly Fr[] _coeffs;

    public MonomialBasis(Fr[] coeffs)
    {
        _coeffs = coeffs;
    }

    public static MonomialBasis Empty() =>
        new MonomialBasis(new Fr[]
        {
        });

    private static MonomialBasis Mul(MonomialBasis a, MonomialBasis b)
    {
        Fr[] output = new Fr[a.Length() + b.Length() - 1];
        for (int i = 0; i < a.Length(); i++)
        {
            for (int j = 0; j < b.Length(); j++)
            {
                output[i + j] += a._coeffs[i]! * b._coeffs[j]!;
            }
        }
        return new MonomialBasis(output);
    }

    public static MonomialBasis Div(MonomialBasis a, MonomialBasis b)
    {
        if (a.Length() < b.Length())
        {
            throw new Exception();
        }

        Fr[] x = a._coeffs.ToArray();
        List<Fr> output = new List<FrE>();

        int aPos = a.Length() - 1;
        int bPos = b.Length() - 1;

        int diff = aPos - bPos;
        while (diff >= 0)
        {
            Fr quot = x[aPos]! / b._coeffs[bPos]!;
            output.Insert(0, quot);
            for (int i = bPos; i > -1; i--)
            {
                x[diff + i] -= b._coeffs[i]! * quot;
            }

            aPos -= 1;
            diff -= 1;
        }

        return new MonomialBasis(output.ToArray());
    }

    public Fr Evaluate(Fr x)
    {
        Fr y = Fr.Zero;
        Fr powerOfX = Fr.One;
        foreach (Fr pCoeff in _coeffs)
        {
            y += powerOfX * pCoeff;
            powerOfX *= x;
        }

        return y;
    }

    public static MonomialBasis FormalDerivative(MonomialBasis f)
    {
        Fr[] derivative = new Fr[f.Length() - 1];
        for (int i = 1; i < f.Length(); i++)
        {
            Fr x = new Fr((ulong)i) * f._coeffs[i]!;
            derivative[i - 1] = x;
        }
        return new MonomialBasis(derivative.ToArray());
    }

    public static MonomialBasis VanishingPoly(IEnumerable<Fr> xs)
    {
        List<Fr> root = new List<Fr>
        {
            Fr.One
        };
        foreach (Fr x in xs)
        {
            root.Insert(0, Fr.Zero);
            for (int i = 0; i < root.Count - 1; i++)
            {
                root[i] -= root[i + 1] * x;
            }
        }

        return new MonomialBasis(root.ToArray());
    }

    public int Length()
    {
        return _coeffs.Length;
    }

    public static MonomialBasis operator /(in MonomialBasis a, in MonomialBasis b)
    {
        return Div(a, b);
    }

    public static MonomialBasis operator *(in MonomialBasis a, in MonomialBasis b)
    {
        return Mul(a, b);
    }

    public static bool operator ==(in MonomialBasis a, in MonomialBasis b)
    {
        return a._coeffs == b._coeffs;
    }

    public static bool operator !=(in MonomialBasis a, in MonomialBasis b)
    {
        return !(a == b);
    }

    public bool Equals(MonomialBasis? x, MonomialBasis? y)
    {
        return x!._coeffs.SequenceEqual(y!._coeffs);
    }

    public int GetHashCode(MonomialBasis obj)
    {
        return obj._coeffs.GetHashCode();
    }

    private bool Equals(MonomialBasis other)
    {
        return _coeffs.Equals(other._coeffs);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MonomialBasis)obj);
    }

    public override int GetHashCode()
    {
        return _coeffs.GetHashCode();
    }
}
