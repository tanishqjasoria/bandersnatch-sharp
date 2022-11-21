using Nethermind.Field;
using Nethermind.MontgomeryField;
using Nethermind.Verkle.Curve;

namespace Nethermind.Verkle.Polynomial;
using Fr = FrE;


public enum ArithmeticOps
{
    Add,
    Sub,
    Mul
}

public class LagrangeBasis : IEqualityComparer<LagrangeBasis>
{
    public readonly Fr[] Evaluations;
    public readonly Fr[] Domain;

    private LagrangeBasis()
    {
        Evaluations = new Fr[] { };
        Domain = new Fr[] { };
    }
    private static LagrangeBasis Empty()
    {
        return new LagrangeBasis();
    }

    public LagrangeBasis(Fr[] evaluations, Fr[] domain)
    {
        Evaluations = evaluations;
        Domain = domain;
    }

    public Fr[] Values()
    {
        return Evaluations;
    }

    private static LagrangeBasis ArithmeticOp(LagrangeBasis lhs, LagrangeBasis rhs, ArithmeticOps op)
    {
        if (!lhs.Domain.SequenceEqual(rhs.Domain))  throw new Exception();

        Fr[] result = new Fr[lhs.Evaluations.Length];

        Parallel.For(0, lhs.Evaluations.Length, i =>
        {
            result[i] = op switch
            {
                ArithmeticOps.Add => lhs.Evaluations[i] + rhs.Evaluations[i],
                ArithmeticOps.Sub => lhs.Evaluations[i] - rhs.Evaluations[i],
                ArithmeticOps.Mul => lhs.Evaluations[i] * rhs.Evaluations[i],
                var _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            };
        });

        return new LagrangeBasis(result, lhs.Domain);
    }

    public static LagrangeBasis Add(LagrangeBasis lhs, LagrangeBasis rhs)
    {
        return ArithmeticOp(lhs, rhs, ArithmeticOps.Add);
    }

    public static LagrangeBasis Sub(LagrangeBasis lhs, LagrangeBasis rhs)
    {
        return ArithmeticOp(lhs, rhs, ArithmeticOps.Sub);
    }

    public static LagrangeBasis Mul(LagrangeBasis lhs, LagrangeBasis rhs)
    {
        return ArithmeticOp(lhs, rhs, ArithmeticOps.Mul);
    }

    public static LagrangeBasis Scale(LagrangeBasis poly, Fr constant)
    {
        Fr[] result = new Fr[poly.Evaluations.Length];

        for (int i = 0; i < poly.Evaluations.Length; i++)
        {
            result[i] = poly.Evaluations[i] * constant;
        }
        return new LagrangeBasis(result, poly.Domain);
    }

    public Fr EvaluateOutsideDomain(LagrangeBasis precomputedWeights, Fr z)
    {
        Fr r = Fr.Zero;
        MonomialBasis A = MonomialBasis.VanishingPoly(Domain);
        Fr az = A.Evaluate(z);

        if (az.IsZero)
            throw new Exception("vanishing polynomial evaluated to zero. z is therefore a point on the domain");

        Fr[] inverses = Fr.MultiInverse(Domain.Select(x => z - x).ToArray());

        for (int i = 0; i < inverses.Length; i++)
        {
            Fr x = inverses[i];
            r += Evaluations[i] * precomputedWeights.Evaluations[i] * x;
        }


        r *= az;

        return r;
    }

    public MonomialBasis Interpolate()
    {
        Fr[] xs = Domain;
        Fr[] ys = Evaluations;

        MonomialBasis root = MonomialBasis.VanishingPoly(xs);
        if (root.Length() != ys.Length + 1)
            throw new Exception();

        List<MonomialBasis> nums = new List<MonomialBasis>();
        foreach (Fr x in xs)
        {
            Fr[] s = { x.Neg(), Fr.One };
            MonomialBasis elem = root / new MonomialBasis(s);
            nums.Add(elem);
        }

        List<Fr> denoms = new List<Fr>();
        for (int i = 0; i < xs.Length; i++)
        {
            denoms.Add(nums[i].Evaluate(xs[i]));
        }
        Fr[] invDenoms = Fr.MultiInverse(denoms.ToArray());

        Fr[] b = new Fr[ys.Length];
        for (int i = 0; i < b.Length; i++)
        {
            b[i] = Fr.Zero;
        }

        for (int i = 0; i < xs.Length; i++)
        {
            Fr ySlice = ys[i] * invDenoms[i];
            for (int j = 0; j < ys.Length; j++)
            {
                b[j] += nums[i]._coeffs[j] * ySlice;
            }
        }

        while (b.Length > 0 && b[^1].IsZero)
        {
            Array.Resize(ref b, b.Length - 1);
        }

        return new MonomialBasis(b);
    }

    public static LagrangeBasis operator +(in LagrangeBasis a, in LagrangeBasis b)
    {
        return Add(a, b);
    }

    public static LagrangeBasis operator -(in LagrangeBasis a, in LagrangeBasis b)
    {
        return Sub(a, b);
    }

    public static LagrangeBasis operator *(in LagrangeBasis a, in LagrangeBasis b)
    {
        return Mul(a, b);
    }

    public static LagrangeBasis operator *(in LagrangeBasis a, in Fr b)
    {
        return Scale(a, b);
    }

    public static LagrangeBasis operator *(in Fr a, in LagrangeBasis b)
    {
        return Scale(b, a);
    }

    public static bool operator ==(in LagrangeBasis a, in LagrangeBasis b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(in LagrangeBasis a, in LagrangeBasis b)
    {
        return !(a == b);
    }

    public bool Equals(LagrangeBasis? x, LagrangeBasis? y)
    {
        return x!.Evaluations.SequenceEqual(y!.Evaluations);
    }

    public int GetHashCode(LagrangeBasis obj)
    {
        return HashCode.Combine(obj.Evaluations, obj.Domain);
    }
}
