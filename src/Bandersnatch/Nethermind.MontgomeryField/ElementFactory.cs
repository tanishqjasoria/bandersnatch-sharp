// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Nethermind.Int256;

namespace Nethermind.MontgomeryField;

[StructLayout(LayoutKind.Explicit)]
public readonly struct Element
{
    public static readonly Element Zero = 0ul;
    public static readonly Element One = new ulong[]
    {
        6347764673676886264,
        253265890806062196,
        11064306276430008312,
        1739710354780652911
    };
    public static readonly Element qElement = new ulong[]
    {
        8429901452645165025,
        18415085837358793841,
        922804724659942912,
        2088379214866112338
    };
    public static readonly Element rSquare =  new ulong[]
    {
        15831548891076708299,
        4682191799977818424,
        12294384630081346794,
        785759240370973821,
    };
    private static Lazy<BigInteger> _modulus = new Lazy<BigInteger>(() =>
    {
        BigInteger.TryParse("13108968793781547619861935127046491459309155893440570251786403306729687672801", out BigInteger output);
        return output;
    });

    public static ulong qInvNeg = 17410672245482742751;
    const int Limbs =  4;
    const int Bits =  253;
    const int Bytes =  Limbs * 8;



    /* in little endian order so u3 is the most significant ulong */
    [FieldOffset(0)]
    public readonly ulong u0;
    [FieldOffset(8)]
    public readonly ulong u1;
    [FieldOffset(16)]
    public readonly ulong u2;
    [FieldOffset(24)]
    public readonly ulong u3;

    public ulong this[int index]
    {
        get
        {
            return index switch
            {
                0 => u0,
                1 => u1,
                2 => u2,
                3 => u3,
                var _ => throw new IndexOutOfRangeException()
            };
        }
    }

    public Element(ulong u0 = 0, ulong u1 = 0, ulong u2 = 0, ulong u3 = 0)
    {
        this.u0 = u0;
        this.u1 = u1;
        this.u2 = u2;
        this.u3 = u3;
    }

    public Element(in ReadOnlySpan<byte> bytes, bool isBigEndian = false)
    {
        FromBytes(bytes, isBigEndian, out u0, out u1, out u2, out u3);
    }

    public bool IsZero => (u0 | u1 | u2 | u3) == 0;

    public bool IsOne => Equals(One);



    public static void Inverse(in Element x, out Element z)
    {
        if (x.IsZero)
        {
            z = Zero;
            return;
        }

        // modulus
        Element q = new Element(
            8429901452645165025UL,
            18415085837358793841UL,
            922804724659942912UL,
            2088379214866112338UL
        );

        // initialize u = q
        Element u = new Element(
            8429901452645165025UL,
            18415085837358793841UL,
            922804724659942912UL,
            2088379214866112338UL
        );

        // initialize s = r^2
        Element s = new Element(
            15831548891076708299,
            4682191799977818424,
            12294384630081346794,
            785759240370973821
        );

        Element r = new Element();
        Element v = x;


        while (true)
        {
            while ((v[0] & 1) == 0)
            {
                v >>= 1;
                if ((s[0] & 1) == 1) s += q;
                s >>= 1;
            }

            while ((u[0] & 1) == 0)
            {
                u >>= 1;
                if ((r[0] & 1) == 1) r += q;
                r >>= 1;
            }

            if (!LessThan(v, u))
            {
                v -= u;
                if (SubtractUnderflow(s, r, out s)) s += q;
            }
            else
            {
                u -= v;
                if (SubtractUnderflow(r, s, out r)) r += q;
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


    public static void MulMod(in Element x, in Element y, out Element res)
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
        if (LessThan(qElement, z))
        {
            ulong b = 0;
            SubtractWithBorrow(z[0], qElement[0], ref b, out z[0]);
            SubtractWithBorrow(z[1], qElement[1], ref b, out z[1]);
            SubtractWithBorrow(z[2], qElement[2], ref b, out z[2]);
            SubtractWithBorrow(z[3], qElement[3], ref b, out z[3]);
        }
        res = z;
    }

    public static void AddMod(in Element a, in Element b, out Element res)
    {
        Add(a, b, out Element z);
        if (LessThan(qElement, z))
            res = z - qElement;
        else
            res = z;

    }

    public static void SubMod(in Element a, in Element b, out Element res)
    {
        if (SubtractUnderflow(a, b, out res)) res += qElement;
    }

    public static void Divide(in Element x, in Element y, out Element z)
    {
        Inverse(y, out Element yInv);
        MulMod(x, yInv, out z);
    }

    public static void Lsh(in Element x, int n, out Element res)
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

        res = new Element(z0, z1, z2, z3);
    }

    public void LeftShift(int n, out Element res)
    {
        Lsh(this, n, out res);
    }



    public bool Bit(int n)
    {
        int bucket = (n / 64) % 4;
        int position = n % 64;
        return (this[bucket] & ((ulong)1 << position)) != 0;
    }

    public static void Rsh(in Element x, int n, out Element res)
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

        res = new Element(z0, z1, z2, z3);
    }

    public void RightShift(int n, out Element res) => Rsh(this, n, out res);



    internal void Lsh64(out Element res)
    {
        res = new Element(0, u0, u1, u2);
    }

    internal void Lsh128(out Element res)
    {
        res = new Element(0, 0, u0, u1);
    }

    internal void Lsh192(out Element res)
    {
        res = new Element(0, 0, 0, u0);
    }

    internal void Rsh64(out Element res)
    {
        res = new Element(u1, u2, u3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Rsh128(out Element res)
    {
        res = new Element(u2, u3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Rsh192(out Element res)
    {
        res = new Element(u3);
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


    // Add sets res to the sum a+b
    public static void Add(in Element a, in Element b, out Element res)
    {
        ulong carry = 0ul;
        AddWithCarry(a.u0, b.u0, ref carry, out ulong res1);
        AddWithCarry(a.u1, b.u1, ref carry, out ulong res2);
        AddWithCarry(a.u2, b.u2, ref carry, out ulong res3);
        AddWithCarry(a.u3, b.u3, ref carry, out ulong res4);
        res = new Element(res1, res2, res3, res4);
    }
    public static bool SubtractUnderflow(in Element a, in Element b, out Element res)
    {
        ulong borrow = 0;
        SubtractWithBorrow(a[0], b[0], ref borrow, out ulong z0);
        SubtractWithBorrow(a[1], b[1], ref borrow, out ulong z1);
        SubtractWithBorrow(a[2], b[2], ref borrow, out ulong z2);
        SubtractWithBorrow(a[3], b[3], ref borrow, out ulong z3);
        res = new Element(z0, z1, z2, z3);
        return borrow != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SubtractWithBorrow(ulong a, ulong b, ref ulong borrow, out ulong res)
    {
        res = a - b - borrow;
        borrow = (((~a) & b) | (~(a ^ b)) & res) >> 63;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddWithCarry(ulong x, ulong y, ref ulong carry, out ulong sum)
    {
        sum = x + y + carry;
        // both msb bits are 1 or one of them is 1 and we had carry from lower bits
        carry = ((x & y) | ((x | y) & (~sum))) >> 63;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (ulong high, ulong low) Multiply64(ulong a, ulong b)
    {
        ulong high = Math.BigMul(a, b, out ulong low);
        return (high, low);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong MAdd0(ulong a, ulong b, ulong c)
    {
        ulong carry = 0;
        (ulong hi, ulong lo) = Multiply64(a, b);
        AddWithCarry(lo, c, ref carry, out lo);
        AddWithCarry(hi, 0, ref carry, out hi);
        return hi;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (ulong, ulong) MAdd1(ulong a, ulong b, ulong c)
    {
        (ulong hi, ulong lo) = Multiply64(a, b);
        ulong carry = 0;
        AddWithCarry(lo, c, ref carry, out lo);
        AddWithCarry(hi, 0, ref carry, out hi);
        return (hi, lo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    private static void FromBytes(in ReadOnlySpan<byte> bytes, bool isBigEndian, out ulong u0, out ulong u1, out ulong u2, out ulong u3)
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

    public static implicit operator Element(ulong value) => new Element(value, 0ul, 0ul, 0ul);
    public static implicit operator Element(ulong[] value) => new Element(value[0], value[1], value[2], value[3]);

    public static explicit operator Element(in BigInteger value)
    {
        byte[] bytes32 = value.ToBytes32(true);
        return new Element(bytes32, true);
    }

    public static Element operator +(in Element a, in Element b)
    {
        Add(in a, in b, out Element res);
        return res;
    }

    public static Element operator -(in Element a, in Element b)
    {
        if (SubtractUnderflow(in a, in b, out Element c))
        {
            throw new ArithmeticException($"Underflow in subtraction {a} - {b}");
        }

        return c;
    }

    public static Element operator >>(in Element a, int n)
    {
        a.RightShift(n, out Element res);
        return res;
    }
    public static Element operator <<(in Element a, int n)
    {
        a.LeftShift(n, out Element res);
        return res;
    }

    public bool Equals(Element other) => u0 == other.u0 && u1 == other.u1 && u2 == other.u2 && u3 == other.u3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Equals(in Element other) =>
        u0 == other.u0 &&
        u1 == other.u1 &&
        u2 == other.u2 &&
        u3 == other.u3;

    public int CompareTo(Element b) => this < b ? -1 : Equals(b) ? 0 : 1;

    public override bool Equals(object? obj) => obj is Element other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(u0, u1, u2, u3);


    public static Element operator /(in Element a, in Element b)
    {
        Divide(in a, in b, out Element c);
        return c;
    }

    public static bool operator <(in Element a, in Element b) => LessThan(in a, in b);
    public static bool operator <(in Element a, int b) => LessThan(in a, b);
    public static bool operator <(int a, in Element b) => LessThan(a, in b);
    public static bool operator <(in Element a, uint b) => LessThan(in a, b);
    public static bool operator <(uint a, in Element b) => LessThan(a, in b);
    public static bool operator <(in Element a, long b) => LessThan(in a, b);
    public static bool operator <(long a, in Element b) => LessThan(a, in b);
    public static bool operator <(in Element a, ulong b) => LessThan(in a, b);
    public static bool operator <(ulong a, in Element b) => LessThan(a, in b);
    public static bool operator <=(in Element a, in Element b) => !LessThan(in b, in a);
    public static bool operator <=(in Element a, int b) => !LessThan(b, in a);
    public static bool operator <=(int a, in Element b) => !LessThan(in b, a);
    public static bool operator <=(in Element a, uint b) => !LessThan(b, in a);
    public static bool operator <=(uint a, in Element b) => !LessThan(in b, a);
    public static bool operator <=(in Element a, long b) => !LessThan(b, in a);
    public static bool operator <=(long a, in Element b) => !LessThan(in b, a);
    public static bool operator <=(in Element a, ulong b) => !LessThan(b, in a);
    public static bool operator <=(ulong a, Element b) => !LessThan(in b, a);
    public static bool operator >(in Element a, in Element b) => LessThan(in b, in a);
    public static bool operator >(in Element a, int b) => LessThan(b, in a);
    public static bool operator >(int a, in Element b) => LessThan(in b, a);
    public static bool operator >(in Element a, uint b) => LessThan(b, in a);
    public static bool operator >(uint a, in Element b) => LessThan(in b, a);
    public static bool operator >(in Element a, long b) => LessThan(b, in a);
    public static bool operator >(long a, in Element b) => LessThan(in b, a);
    public static bool operator >(in Element a, ulong b) => LessThan(b, in a);
    public static bool operator >(ulong a, in Element b) => LessThan(in b, a);
    public static bool operator >=(in Element a, in Element b) => !LessThan(in a, in b);
    public static bool operator >=(in Element a, int b) => !LessThan(in a, b);
    public static bool operator >=(int a, in Element b) => !LessThan(a, in b);
    public static bool operator >=(in Element a, uint b) => !LessThan(in a, b);
    public static bool operator >=(uint a, in Element b) => !LessThan(a, in b);
    public static bool operator >=(in Element a, long b) => !LessThan(in a, b);
    public static bool operator >=(long a, in Element b) => !LessThan(a, in b);
    public static bool operator >=(in Element a, ulong b) => !LessThan(in a, b);
    public static bool operator >=(ulong a, in Element b) => !LessThan(a, in b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LessThan(in Element a, long b) => b >= 0 && a.u3 == 0 && a.u2 == 0 && a.u1 == 0 && a.u0 < (ulong)b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LessThan(long a, in Element b) => a < 0 || b.u1 != 0 || b.u2 != 0 || b.u3 != 0 || (ulong)a < b.u0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LessThan(in Element a, ulong b) => a.u3 == 0 && a.u2 == 0 && a.u1 == 0 && a.u0 < b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LessThan(ulong a, in Element b) => b.u3 != 0 || b.u2 != 0 || b.u1 != 0 || a < b.u0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LessThan(in Element a, in Element b)
    {
        if (a.u3 != b.u3)
            return a.u3 < b.u3;
        if (a.u2 != b.u2)
            return a.u2 < b.u2;
        if (a.u1 != b.u1)
            return a.u1 < b.u1;
        return a.u0 < b.u0;
    }

    public static bool operator ==(in Element a, int b) => a.Equals(b);
    public static bool operator ==(int a, in Element b) => b.Equals(a);
    public static bool operator ==(in Element a, uint b) => a.Equals(b);
    public static bool operator ==(uint a, in Element b) => b.Equals(a);
    public static bool operator ==(in Element a, long b) => a.Equals(b);
    public static bool operator ==(long a, in Element b) => b.Equals(a);
    public static bool operator ==(in Element a, ulong b) => a.Equals(b);
    public static bool operator ==(ulong a, in Element b) => b.Equals(a);
    public static bool operator !=(in Element a, int b) => !a.Equals(b);
    public static bool operator !=(int a, in Element b) => !b.Equals(a);
    public static bool operator !=(in Element a, uint b) => !a.Equals(b);
    public static bool operator !=(uint a, in Element b) => !b.Equals(a);
    public static bool operator !=(in Element a, long b) => !a.Equals(b);
    public static bool operator !=(long a, in Element b) => !b.Equals(a);
    public static bool operator !=(in Element a, ulong b) => !a.Equals(b);
    public static bool operator !=(ulong a, in Element b) => !b.Equals(a);

    public bool Equals(int other) => other >= 0 && u0 == (uint)other && u1 == 0 && u2 == 0 && u3 == 0;

    public bool Equals(uint other) => u0 == other && u1 == 0 && u2 == 0 && u3 == 0;

    public bool Equals(long other) => other >= 0 && u0 == (ulong)other && u1 == 0 && u2 == 0 && u3 == 0;

    public bool Equals(ulong other) => u0 == other && u1 == 0 && u2 == 0 && u3 == 0;
}
