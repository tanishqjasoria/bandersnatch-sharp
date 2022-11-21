using System.Numerics;
using Nethermind.Field;
using Nethermind.Int256;

namespace Nethermind.Verkle.Curve;

public struct BandersnatchBaseFieldStruct : IFieldDefinition
{
    private static readonly byte[] ModBytes =
    {
        1, 0, 0, 0, 255, 255, 255, 255, 254, 91, 254, 255, 2, 164, 189, 83, 5, 216, 161, 9, 8, 216, 57, 51, 72, 125,
        157, 41, 83, 167, 237, 115
    };
    public readonly UInt256 FieldMod => new(ModBytes);
}

public class FpN : FiniteField, IComparable<FpN>, IEqualityComparer<FpN>
{
    private static readonly byte[] _modBytes =
    {
        1, 0, 0, 0, 255, 255, 255, 255, 254, 91, 254, 255, 2, 164, 189, 83, 5, 216, 161, 9, 8, 216, 57, 51, 72, 125,
        157, 41, 83, 167, 237, 115
    };
    private static UInt256 FieldMod => new UInt256(_modBytes);
    public FpN(UInt256 value)
    {
        Modulus = FieldMod;
        UInt256.Mod(value, Modulus, out Value);
    }

    public FpN(BigInteger value)
    {
        Modulus = FieldMod;
        if (value.Sign < 0)
        {
            UInt256Extension.SubtractMod(UInt256.Zero, (UInt256)(-value), Modulus, out Value);
        }
        else
        {
            UInt256.Mod((UInt256)value, Modulus, out Value);
        }
    }

    private FpN()
    {
        Modulus = FieldMod;
    }

    public static FpN Zero => new((UInt256)0);
    public static FpN One => new((UInt256)1);

    public static FpN? FromBytes(byte[] byteEncoded)
    {
        UInt256 value = new UInt256(byteEncoded);
        return value > FieldMod ? null : new FpN(value);
    }

    public static FpN FromBytesReduced(byte[] byteEncoded)
    {
        return new FpN(new UInt256(byteEncoded));
    }

    public static new FpN FromBytesReduced(byte[] byteEncoded, UInt256 modulus)
    {
        throw new Exception("cannot get field with different modulus");
    }

    public static bool LexicographicallyLargest(FpN x, UInt256 qMinOneDiv2)
    {
        return x.Value > qMinOneDiv2;
    }

    public bool LexicographicallyLargest()
    {
        return Value > QMinOneDiv2;
    }

    public new FpN Neg()
    {
        FpN? result = new FpN();
        UInt256Extension.SubtractMod(UInt256.Zero, Value, Modulus, out result.Value);
        return result;
    }

    public static FpN Neg(FpN a)
    {
        FpN res = new();
        UInt256Extension.SubtractMod(UInt256.Zero, a.Value, a.Modulus, out res.Value);
        return res;
    }

    public FpN Add(FpN a)
    {
        FpN res = new();
        UInt256.AddMod(Value, a.Value, Modulus, out res.Value);
        return res;
    }

    public static FpN Add(FpN a, FpN b)
    {
        FpN res = new();
        UInt256.AddMod(a.Value, b.Value, a.Modulus, out res.Value);
        return res;
    }

    public FpN Sub(FpN a)
    {
        FpN res = new();
        UInt256Extension.SubtractMod(Value, a.Value, Modulus, out res.Value);
        return res;
    }

    public static FpN Sub(FpN a, FpN b)
    {
        FpN res = new();
        UInt256Extension.SubtractMod(a.Value, b.Value, a.Modulus, out res.Value);
        return res;
    }

    public static FpN Mul(FpN a, FpN b)
    {
        FpN result = new();
        UInt256.MultiplyMod(a.Value, b.Value, a.Modulus, out result.Value);
        return result;
    }

    public FpN Mul(FpN a)
    {
        FpN result = new();
        UInt256.MultiplyMod(Value, a.Value, Modulus, out result.Value);
        return result;
    }

    public static FpN? Div(FpN a, FpN b)
    {
        FpN? bInv = Inverse(b);
        return bInv is null ? null : Mul(a, bInv);
    }

    public static FpN? ExpMod(FpN a, UInt256 b)
    {
        FpN result = new();
        UInt256.ExpMod(a.Value, b, a.Modulus, out result.Value);
        return result;
    }

    public bool Equals(FpN a)
    {
        return Value.Equals(a.Value);
    }

    public new FpN Dup()
    {
        FpN ret = new FpN
        {
            Value = Value,
            Modulus = Modulus,
        };
        return ret;
    }

    public new FpN? Inverse()
    {
        if (Value.IsZero) return null;
        FpN result = new();
        UInt256.ExpMod(Value, Modulus - 2, Modulus, out result.Value);
        return result;
    }

    public static FpN? Inverse(FpN a)
    {
        if (a.Value.IsZero) return null;
        FpN inv = new FpN();
        UInt256.ExpMod(a.Value, a.Modulus - 2, a.Modulus, out inv.Value);
        return inv;
    }

    public static FpN[] MultiInverse(FpN[] values)
    {
        FpN[] partials = new FpN[values.Length + 1];
        partials[0] = One;
        for (int i = 0; i < values.Length; i++)
        {
            FpN x = Mul(partials[i], values[i]);
            partials[i + 1] = x.IsZero ? One : x;
        }

        FpN? inverse = Inverse(partials[^1]);

        FpN[] outputs = new FpN[values.Length];
        for (int i = values.Length - 1; i >= 0; i--)
        {
            outputs[i] = values[i].IsZero ? Zero : Mul(partials[i], inverse);
            inverse = inverse.Mul(values[i]);
            inverse = inverse.IsZero ? One : inverse;
        }
        return outputs;
    }

    public static FpN? Sqrt(FpN a)
    {
        FpN res = new();

        UInt256? val = FieldMethods.ModSqrt(a.Value, a.Modulus);
        if (val is null)
            return null;
        res.Value = (UInt256)val;
        return res;
    }

    public static FpN operator +(in FpN a, in FpN b)
    {
        return Add(a, b);
    }

    public static FpN operator -(in FpN a, in FpN b)
    {
        return Sub(a, b);
    }

    public static FpN operator *(in FpN a, in FpN b)
    {
        return Mul(a, b);
    }

    public static FpN? operator /(in FpN a, in FpN b)
    {
        return Div(a, b);
    }

    public static bool operator ==(in FpN a, in FpN b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(in FpN a, in FpN b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((FpN)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, Modulus);
    }

    public new int CompareTo(object? obj) => obj is not FpN fixedFiniteField
        ? throw new InvalidOperationException()
        : CompareTo(fixedFiniteField);

    public int CompareTo(FpN? other)
    {
        return Value.CompareTo(other!.Value);
    }

    public bool Equals(FpN? x, FpN? y)
    {
        return x.Value == y.Value;
    }
    public int GetHashCode(FpN obj)
    {
        return HashCode.Combine(obj.Value, obj.Modulus);
    }
}
