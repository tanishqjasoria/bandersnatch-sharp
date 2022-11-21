// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using Nethermind.Int256;

namespace Nethermind.MontgomeryField.obsolete;

public interface IMFieldDefinition
{
    ulong[] One { get; }
    ulong[] QElement { get; }
    ulong[] RSquare { get; }
    ulong QInvNeg { get; }
    BigInteger Modulus { get; }

    int Limbs { get; }
    int Bits { get; }
    int Bytes { get; }
}

[Obsolete("Performance Issues, kept for benchmarking")]
public readonly struct Element<T> where T : struct, IMFieldDefinition
{
    public static readonly Element<T> Zero = 0ul;
    public static readonly Element<T> One = new T().One;

    public static implicit operator Element<T>(ulong value) => new Element<T>(value, 0ul, 0ul, 0ul);
    public static implicit operator Element<T>(ulong[] value) => new Element<T>(value[0], value[1], value[2], value[3]);

    public static readonly Element<T> qElement = new T().QElement;
    public static readonly Element<T> rSquare = new T().RSquare;


    private static BigInteger Modulus = new T().Modulus;

    public static ulong qInvNeg = new T().QInvNeg;

    public readonly int Limbs = new T().Limbs;
    public readonly int Bits = new T().Bits;
    public readonly int Bytes = new T().Bytes;

    /* in little endian order so u3 is the most significant ulong */
    public readonly ulong u0;
    public readonly ulong u1;
    public readonly ulong u2;
    public readonly ulong u3;

    public Element(ulong u0 = 0, ulong u1 = 0, ulong u2 = 0, ulong u3 = 0)
    {
        this.u0 = u0;
        this.u1 = u1;
        this.u2 = u2;
        this.u3 = u3;
    }

    public Element(in ReadOnlySpan<byte> bytes, bool isBigEndian = false)
    {
        if (bytes.Length == 32)
        {
            if (isBigEndian)
            {
                u3 = BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(0, 8));
                u2 = BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(8, 8));
                u1 = BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(16, 8));
                u0 = BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(24, 8));
            }
            else
            {
                u0 = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(0, 8));
                u1 = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(8, 8));
                u2 = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(16, 8));
                u3 = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(24, 8));
            }
        }
        else
        {
            int byteCount = bytes.Length;
            int unalignedBytes = byteCount % 8;
            int dwordCount = byteCount / 8 + (unalignedBytes == 0 ? 0 : 1);

            ulong cs0 = 0;
            ulong cs1 = 0;
            ulong cs2 = 0;
            ulong cs3 = 0;

            if (dwordCount == 0)
            {
                u0 = u1 = u2 = u3 = 0;
                return;
            }

            if (dwordCount >= 1)
            {
                for (int j = 8; j > 0; j--)
                {
                    cs0 <<= 8;
                    if (j <= byteCount)
                    {
                        cs0 |= bytes[byteCount - j];
                    }
                }
            }

            if (dwordCount >= 2)
            {
                for (int j = 16; j > 8; j--)
                {
                    cs1 <<= 8;
                    if (j <= byteCount)
                    {
                        cs1 |= bytes[byteCount - j];
                    }
                }
            }

            if (dwordCount >= 3)
            {
                for (int j = 24; j > 16; j--)
                {
                    cs2 <<= 8;
                    if (j <= byteCount)
                    {
                        cs2 |= bytes[byteCount - j];
                    }
                }
            }

            if (dwordCount >= 4)
            {
                for (int j = 32; j > 24; j--)
                {
                    cs3 <<= 8;
                    if (j <= byteCount)
                    {
                        cs3 |= bytes[byteCount - j];
                    }
                }
            }

            u0 = cs0;
            u1 = cs1;
            u2 = cs2;
            u3 = cs3;
        }
    }

    public bool IsZero => (u0 | u1 | u2 | u3) == 0;

    public bool IsOne => ((u0 ^ 1UL) | u1 | u2 | u3) == 0;

    public static void Divide(Element<T> x, Element<T> y, out Element<T> z)
    {
        Inverse(y, out Element<T> yInv);
        MultiplyModGeneric(x, yInv, out z);
    }

    public static void Inverse(Element<T> x, out Element<T> z)
    {
        if (x.IsZero)
        {
            z = Zero;
            return;
        }

        Element<T> u = qElement.Clone();

        Element<T> s = rSquare.Clone();

        Element<T> q = qElement.Clone();


        Element<T> r = new Element<T>();

        Element<T> v = new Element<T>(x.u0, x.u1, x.u2, x.u3);

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

    public static void MultiplyModGeneric(in Element<T> x, in Element<T> y, out Element<T> res)
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
                    return u0;
                case 1:
                    return u1;
                case 2:
                    return u2;
                case 3:
                    return u3;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }


    public static bool SubtractUnderflow(in Element<T> a, in Element<T> b, out Element<T> res)
    {
        ulong borrow = 0;
        SubtractWithBorrow(a[0], b[0], ref borrow, out ulong z0);
        SubtractWithBorrow(a[1], b[1], ref borrow, out ulong z1);
        SubtractWithBorrow(a[2], b[2], ref borrow, out ulong z2);
        SubtractWithBorrow(a[3], b[3], ref borrow, out ulong z3);
        res = new Element<T>(z0, z1, z2, z3);
        return borrow != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SubtractWithBorrow(ulong a, ulong b, ref ulong borrow, out ulong res)
    {
        res = a - b - borrow;
        borrow = (((~a) & b) | (~(a ^ b)) & res) >> 63;
    }


    public static void Lsh(in Element<T> x, int n, out Element<T> res)
    {
        if ((n % 64) == 0)
        {
            switch (n)
            {
                case 0:
                    res = x;
                    return;
                case 64:
                    x.Lsh64(out res);
                    return;
                case 128:
                    x.Lsh128(out res);
                    return;
                case 192:
                    x.Lsh192(out res);
                    return;
                default:
                    res = Zero;
                    return;
            }
        }

        res = Zero;
        ulong z0 = res.u0, z1 = res.u1, z2 = res.u2, z3 = res.u3;
        ulong a = 0, b = 0;
        // Big swaps first
        if (n > 192)
        {
            if (n > 256)
            {
                res = Zero;
                return;
            }

            x.Lsh192(out res);
            n -= 192;
            goto sh192;
        }
        else if (n > 128)
        {
            x.Lsh128(out res);
            n -= 128;
            goto sh128;
        }
        else if (n > 64)
        {
            x.Lsh64(out res);
            n -= 64;
            goto sh64;
        }
        else
        {
            res = x;
        }

        // remaining shifts
        a = Rsh(res.u0, 64 - n);
        z0 = Lsh(res.u0, n);

sh64:
        b = Rsh(res.u1, 64 - n);
        z1 = Lsh(res.u1, n) | a;

sh128:
        a = Rsh(res.u2, 64 - n);
        z2 = Lsh(res.u2, n) | b;

sh192:
        z3 = Lsh(res.u3, n) | a;

        res = new Element<T>(z0, z1, z2, z3);
    }

    public void LeftShift(int n, out Element<T> res)
    {
        Lsh(this, n, out res);
    }

    public static Element<T> operator <<(in Element<T> a, int n)
    {
        a.LeftShift(n, out Element<T> res);
        return res;
    }

    public bool Bit(int n)
    {
        int bucket = (n / 64) % 4;
        int position = n % 64;
        return (this[bucket] & ((ulong)1 << position)) != 0;
    }

    public static void Rsh(in Element<T> x, int n, out Element<T> res)
    {
        // n % 64 == 0
        if ((n & 0x3f) == 0)
        {
            switch (n)
            {
                case 0:
                    res = x;
                    return;
                case 64:
                    x.Rsh64(out res);
                    return;
                case 128:
                    x.Rsh128(out res);
                    return;
                case 192:
                    x.Rsh192(out res);
                    return;
                default:
                    res = Zero;
                    return;
            }
        }

        res = Zero;
        ulong z0 = res.u0, z1 = res.u1, z2 = res.u2, z3 = res.u3;
        ulong a = 0, b = 0;
        // Big swaps first
        if (n > 192)
        {
            if (n > 256)
            {
                res = Zero;
                return;
            }

            x.Rsh192(out res);
            z0 = res.u0;
            z1 = res.u1;
            z2 = res.u2;
            z3 = res.u3;
            n -= 192;
            goto sh192;
        }
        else if (n > 128)
        {
            x.Rsh128(out res);
            z0 = res.u0;
            z1 = res.u1;
            z2 = res.u2;
            z3 = res.u3;
            n -= 128;
            goto sh128;
        }
        else if (n > 64)
        {
            x.Rsh64(out res);
            z0 = res.u0;
            z1 = res.u1;
            z2 = res.u2;
            z3 = res.u3;
            n -= 64;
            goto sh64;
        }
        else
        {
            res = x;
            z0 = res.u0;
            z1 = res.u1;
            z2 = res.u2;
            z3 = res.u3;
        }

        // remaining shifts
        a = Lsh(res.u3, 64 - n);
        z3 = Rsh(res.u3, n);

sh64:
        b = Lsh(res.u2, 64 - n);
        z2 = Rsh(res.u2, n) | a;

sh128:
        a = Lsh(res.u1, 64 - n);
        z1 = Rsh(res.u1, n) | b;

sh192:
        z0 = Rsh(res.u0, n) | a;

        res = new Element<T>(z0, z1, z2, z3);
    }

    public void RightShift(int n, out Element<T> res) => Rsh(this, n, out res);

    public static Element<T> operator >>(in Element<T> a, int n)
    {
        a.RightShift(n, out Element<T> res);
        return res;
    }

    internal void Lsh64(out Element<T> res)
    {
        res = new Element<T>(0, u0, u1, u2);
    }

    internal void Lsh128(out Element<T> res)
    {
        res = new Element<T>(0, 0, u0, u1);
    }

    internal void Lsh192(out Element<T> res)
    {
        res = new Element<T>(0, 0, 0, u0);
    }

    internal void Rsh64(out Element<T> res)
    {
        res = new Element<T>(u1, u2, u3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Rsh128(out Element<T> res)
    {
        res = new Element<T>(u2, u3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Rsh192(out Element<T> res)
    {
        res = new Element<T>(u3);
    }

    // It avoids c#'s way of shifting a 64-bit number by 64-bit, i.e. in c# a << 64 == a, in our version a << 64 == 0.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Lsh(ulong a, int n)
    {
        var n1 = n >> 1;
        var n2 = n - n1;
        return (a << n1) << n2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Rsh(ulong a, int n)
    {
        var n1 = n >> 1;
        var n2 = n - n1;
        return (a >> n1) >> n2;
    }


    public static Element<T> operator +(in Element<T> a, in Element<T> b)
    {
        Add(in a, in b, out Element<T> res);
        return res;
    }

    public static Element<T> operator -(in Element<T> a, in Element<T> b)
    {
        if (SubtractUnderflow(in a, in b, out Element<T> c))
        {
            throw new ArithmeticException($"Underflow in subtraction {a} - {b}");
        }

        return c;
    }

    public static explicit operator Element<T>(in BigInteger value)
    {
        byte[] bytes32 = value.ToBytes32(true);
        return new Element<T>(bytes32, true);
    }


    // Add sets res to the sum a+b
    public static void Add(in Element<T> a, in Element<T> b, out Element<T> res)
    {
        ulong carry = 0ul;
        AddWithCarry(a.u0, b.u0, ref carry, out ulong res1);
        AddWithCarry(a.u1, b.u1, ref carry, out ulong res2);
        AddWithCarry(a.u2, b.u2, ref carry, out ulong res3);
        AddWithCarry(a.u3, b.u3, ref carry, out ulong res4);
        res = new Element<T>(res1, res2, res3, res4);
        // #if DEBUG
        //             Debug.Assert((BigInteger)res == ((BigInteger)a + (BigInteger)b) % ((BigInteger)1 << 256));
        // #endif
    }

    public static void AddMod(in Element<T> a, in Element<T> b, out Element<T> res)
    {
        Add(a, b, out Element<T> z);
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

    public static void SubMod(in Element<T> a, in Element<T> b, out Element<T> res)
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

    public Element<T> Clone() => (Element<T>)MemberwiseClone();

}
