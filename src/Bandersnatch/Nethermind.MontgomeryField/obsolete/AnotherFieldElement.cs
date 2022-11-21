// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

using System.Numerics;
using System.Runtime.CompilerServices;
using Nethermind.Int256;

namespace Nethermind.MontgomeryField.obsolete;

[Obsolete("Performance Issues, kept for benchmarking")]
public readonly struct AnotherElement<T> where T : struct, IMFieldDefinition
{
    public readonly UInt256 Value;
    public static readonly AnotherElement<T> Zero = 0ul;
    public static readonly AnotherElement<T> One = new T().One;

    public static implicit operator AnotherElement<T>(ulong value) => new AnotherElement<T>(value, 0ul, 0ul, 0ul);
    public static implicit operator AnotherElement<T>(ulong[] value) => new AnotherElement<T>(value[0], value[1], value[2], value[3]);

    public static readonly AnotherElement<T> qElement = new T().QElement;
    public static readonly AnotherElement<T> rSquare = new T().RSquare;


    private static BigInteger Modulus = new T().Modulus;

    public static ulong qInvNeg = new T().QInvNeg;

    public readonly int Limbs = new T().Limbs;
    public readonly int Bits = new T().Bits;
    public readonly int Bytes = new T().Bytes;

    public AnotherElement(ulong u0 = 0, ulong u1 = 0, ulong u2 = 0, ulong u3 = 0)
    {
        Value = new UInt256(u0, u1, u2, u3);
    }

    public AnotherElement(UInt256 x)
    {
        Value = x;
    }

    public AnotherElement(in ReadOnlySpan<byte> bytes, bool isBigEndian = false)
    {
        Value = new UInt256(bytes, isBigEndian);
    }


    public bool IsZero => Value.IsZero;

    public static void Divide(AnotherElement<T> x, AnotherElement<T> y, out AnotherElement<T> z)
    {
        Inverse(y, out AnotherElement<T> yInv);
        MultiplyModGeneric(x, yInv, out z);
    }

    public static void Inverse(AnotherElement<T> x, out AnotherElement<T> z)
    {
        if (x.IsZero)
        {
            z = Zero;
            return;
        }

        AnotherElement<T> u = qElement.Clone();

        AnotherElement<T> s = rSquare.Clone();

        AnotherElement<T> q = qElement.Clone();


        AnotherElement<T> r = new AnotherElement<T>();

        AnotherElement<T> v = new AnotherElement<T>(x.Value.u0, x.Value.u1, x.Value.u2, x.Value.u3);

        while (true)
        {
            while ((v[0] & 1) == 0)
            {
                v >>= 1;

                if ((s[0] & 1) == 1)
                {
                    s += q;
                }
                s >>= 1;
            }

            while ((u[0] & 1) == 0)
            {
                u >>= 1;
                if ((r[0] & 1) == 1)
                {
                    r += q;
                }
                r >>= 1;
            }

            bool bigger = !(v[3] < u[3] || (v[3] == u[3] && (v[2] < u[2] || (v[2] == u[2] && (v[1] < u[1] || (v[1] == u[1] && (v[0] < u[0])))))));

            if (bigger)
            {
                v -= u;

                if (SubtractUnderflow(s, r, out s))
                {
                    s += q;
                }

            }
            else
            {
                u -= v;
                if (SubtractUnderflow(r, s, out r))
                {
                    r += q;
                }
            }

            if ((u[0] == 1) && ((u[3] | u[2] | u[1]) == 0))
            {
                z = r;
                return;
            }

            if ((v[0] == 1) && ((v[3] | v[2] | v[1]) == 0))
            {
                z = s;
                return;
            }
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (ulong high, ulong low) Multiply64(ulong a, ulong b)
    {
        ulong high = Math.BigMul(a, b, out ulong low);
        return (high, low);
    }

    public static void MultiplyModGeneric(in AnotherElement<T> x, in AnotherElement<T> y, out AnotherElement<T> res)
    {
        ulong[] t = new ulong[4];
        ulong[] c = new ulong[3];
        ulong[] z = new ulong[4];

        {
            // round 0

            ulong v = x[0];

            (c[1], c[0]) = Multiply64(v, y[0]);

            ulong m = c[0] * qInvNeg;

            c[2] = MAdd0(m, qElement[0], c[0]);

            (c[1], c[0]) = MAdd1(v, y[1], c[1]);

            (c[2], t[0]) = MAdd2(m, qElement[1], c[2], c[0]);

            (c[1], c[0]) = MAdd1(v, y[2], c[1]);

            (c[2], t[1]) = MAdd2(m, qElement[2], c[2], c[0]);

            (c[1], c[0]) = MAdd1(v, y[3], c[1]);

            (t[3], t[2]) = MAdd3(m, qElement[3], c[0], c[2], c[1]);


        }
        {
            // round 1

            ulong v = x[1];

            (c[1], c[0]) = MAdd1(v, y[0], t[0]);

            ulong m = (c[0] * qInvNeg);

            c[2] = MAdd0(m, qElement[0], c[0]);

            (c[1], c[0]) = MAdd2(v, y[1], c[1], t[1]);

            (c[2], t[0]) = MAdd2(m, qElement[1], c[2], c[0]);

            (c[1], c[0]) = MAdd2(v, y[2], c[1], t[2]);

            (c[2], t[1]) = MAdd2(m, qElement[2], c[2], c[0]);

            (c[1], c[0]) = MAdd2(v, y[3], c[1], t[3]);

            (t[3], t[2]) = MAdd3(m, qElement[3], c[0], c[2], c[1]);
        }
        {
            // round 2

            ulong v = x[2];

            (c[1], c[0]) = MAdd1(v, y[0], t[0]);

            ulong m = (c[0] * qInvNeg);

            c[2] = MAdd0(m, qElement[0], c[0]);

            (c[1], c[0]) = MAdd2(v, y[1], c[1], t[1]);

            (c[2], t[0]) = MAdd2(m, qElement[1], c[2], c[0]);

            (c[1], c[0]) = MAdd2(v, y[2], c[1], t[2]);

            (c[2], t[1]) = MAdd2(m, qElement[2], c[2], c[0]);

            (c[1], c[0]) = MAdd2(v, y[3], c[1], t[3]);

            (t[3], t[2]) = MAdd3(m, qElement[3], c[0], c[2], c[1]);


        }
        {
            // round 3

            ulong v = x[3];

            (c[1], c[0]) = MAdd1(v, y[0], t[0]);

            ulong m = (c[0] * qInvNeg);

            c[2] = MAdd0(m, qElement[0], c[0]);

            (c[1], c[0]) = MAdd2(v, y[1], c[1], t[1]);

            (c[2], z[0]) = MAdd2(m, qElement[1], c[2], c[0]);

            (c[1], c[0]) = MAdd2(v, y[2], c[1], t[2]);

            (c[2], z[1]) = MAdd2(m, qElement[2], c[2], c[0]);

            (c[1], c[0]) = MAdd2(v, y[3], c[1], t[3]);

            (z[3], z[2]) = MAdd3(m, qElement[3], c[0], c[2], c[1]);


        }
        if (!(z[3] < 2088379214866112338 || (z[3] == 2088379214866112338 && (z[2] < 922804724659942912 || (z[2] == 922804724659942912 && (z[1] < 18415085837358793841 || (z[1] == 18415085837358793841 && (z[0] < 8429901452645165025))))))))
        {

            ulong b = 0;
            SubtractWithBorrow(z[0], qElement[0], ref b, out z[0]);
            SubtractWithBorrow(z[1], qElement[1], ref b, out z[1]);
            SubtractWithBorrow(z[2], qElement[2], ref b, out z[2]);
            SubtractWithBorrow(z[3], qElement[3], ref b, out z[3]);
        }
        res = z;
    }

    public static ulong MAdd0(ulong a, ulong b, ulong c)
    {
        ulong carry = 0;
        (ulong hi, ulong lo) = Multiply64(a, b);
        AddWithCarry(lo, c, ref carry, out lo);
        AddWithCarry(hi, 0, ref carry, out hi);
        return hi;
    }

    public static (ulong, ulong) MAdd1(ulong a, ulong b, ulong c)
    {
        (ulong hi, ulong lo) = Multiply64(a, b);
        ulong carry = 0;
        AddWithCarry(lo, c, ref carry, out lo);
        AddWithCarry(hi, 0, ref carry, out hi);
        return (hi, lo);
    }

    public static (ulong, ulong) MAdd2(ulong a, ulong b, ulong c, ulong d)
    {
        (ulong hi, ulong lo) = Multiply64(a, b);
        ulong carry = 0;
        AddWithCarry(c, d, ref carry, out c);
        AddWithCarry(hi, 0, ref carry, out hi);
        carry = 0;
        AddWithCarry(lo, c, ref carry, out lo);
        AddWithCarry(hi, 0, ref carry, out hi);
        return (hi, lo);
    }

    public static (ulong, ulong) MAdd3(ulong a, ulong b, ulong c, ulong d, ulong e)
    {
        (ulong hi, ulong lo) = Multiply64(a, b);
        ulong carry = 0;
        AddWithCarry(c, d, ref carry, out c);
        AddWithCarry(hi, 0, ref carry, out hi);
        carry = 0;
        AddWithCarry(lo, c, ref carry, out lo);
        AddWithCarry(hi, e, ref carry, out hi);
        return (hi, lo);
    }

    public ulong this[int index]
    {
        get
        {
            switch (index)
            {
                case 0:
                    return Value.u0;
                case 1:
                    return Value.u1;
                case 2:
                    return Value.u2;
                case 3:
                    return Value.u3;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }


    public static bool SubtractUnderflow(in AnotherElement<T> a, in AnotherElement<T> b, out AnotherElement<T> res)
    {
        ulong borrow = 0;
        SubtractWithBorrow(a[0], b[0], ref borrow, out ulong z0);
        SubtractWithBorrow(a[1], b[1], ref borrow, out ulong z1);
        SubtractWithBorrow(a[2], b[2], ref borrow, out ulong z2);
        SubtractWithBorrow(a[3], b[3], ref borrow, out ulong z3);
        res = new AnotherElement<T>(z0, z1, z2, z3);
        return borrow != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SubtractWithBorrow(ulong a, ulong b, ref ulong borrow, out ulong res)
    {
        res = a - b - borrow;
        borrow = (((~a) & b) | (~(a ^ b)) & res) >> 63;
    }


    public static void Lsh(in AnotherElement<T> x, int n, out AnotherElement<T> res)
    {
        UInt256.Lsh(x.Value, n, out var z);
        res = new AnotherElement<T>(z);
    }

    public void LeftShift(int n, out AnotherElement<T> res)
    {
        Lsh(this, n, out res);
    }

    public static AnotherElement<T> operator <<(in AnotherElement<T> a, int n)
    {
        a.LeftShift(n, out AnotherElement<T> res);
        return res;
    }

    public bool Bit(int n)
    {
        int bucket = (n / 64) % 4;
        int position = n % 64;
        return (this[bucket] & ((ulong)1 << position)) != 0;
    }

    public static void Rsh(in AnotherElement<T> x, int n, out AnotherElement<T> res)
    {
        UInt256.Rsh(x.Value, n, out var z);
        res = new AnotherElement<T>(z);
    }

    public void RightShift(int n, out AnotherElement<T> res) => Rsh(this, n, out res);

    public static AnotherElement<T> operator >>(in AnotherElement<T> a, int n)
    {
        a.RightShift(n, out AnotherElement<T> res);
        return res;
    }



    public static AnotherElement<T> operator +(in AnotherElement<T> a, in AnotherElement<T> b)
    {
        Add(in a, in b, out AnotherElement<T> res);
        return res;
    }

    public static AnotherElement<T> operator -(in AnotherElement<T> a, in AnotherElement<T> b)
    {
        if (SubtractUnderflow(in a, in b, out AnotherElement<T> c))
        {
            throw new ArithmeticException($"Underflow in subtraction {a} - {b}");
        }

        return c;
    }

    public static explicit operator AnotherElement<T>(in BigInteger value)
    {
        byte[] bytes32 = value.ToBytes32(true);
        return new AnotherElement<T>(bytes32, true);
    }


    // Add sets res to the sum a+b
    public static void Add(in AnotherElement<T> a, in AnotherElement<T> b, out AnotherElement<T> res)
    {
        UInt256.Add(a.Value, b.Value, out var z);
        res = new AnotherElement<T>(z);
    }

    public static void AddMod(in AnotherElement<T> a, in AnotherElement<T> b, out AnotherElement<T> res)
    {
        Add(a, b, out AnotherElement<T> z);
        if (!(z[3] < 2088379214866112338 || (z[3] == 2088379214866112338 &&
                                             (z[2] < 922804724659942912 ||
                                              (z[2] == 922804724659942912 && (z[1] < 18415085837358793841 || (z[1] == 18415085837358793841 && (z[0] < 8429901452645165025))))))))
        {
            res = z - qElement;
        }
        else
        {
            res = z;
        }
    }

    public static void SubMod(in AnotherElement<T> a, in AnotherElement<T> b, out AnotherElement<T> res)
    {
        if (SubtractUnderflow(a, b, out res))
        {
            res += qElement;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddWithCarry(ulong x, ulong y, ref ulong carry, out ulong sum)
    {
        sum = x + y + carry;
        // both msb bits are 1 or one of them is 1 and we had carry from lower bits
        carry = ((x & y) | ((x | y) & (~sum))) >> 63;
    }

    public AnotherElement<T> Clone() => (AnotherElement<T>)MemberwiseClone();

}
