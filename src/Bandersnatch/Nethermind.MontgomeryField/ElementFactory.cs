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
    const int Limbs = 4;
    const int Bits = 253;
    const int Bytes = Limbs * 8;
    const ulong qInvNeg = 17410672245482742751;

    public static readonly Element Zero = new Element(0, 0, 0, 0);

    private const ulong one0 = 6347764673676886264;
    private const ulong one1 = 253265890806062196;
    private const ulong one2 = 11064306276430008312;
    private const ulong one3 = 1739710354780652911;
    private static readonly Element One = new Element(one0, one1, one2, one3);

    private const  ulong q0 = 8429901452645165025;
    private const  ulong q1 = 18415085837358793841;
    private const  ulong q2 = 922804724659942912;
    private const  ulong q3 = 2088379214866112338;
    private static readonly Element qElement = new Element(q0, q1, q2, q3);

    private  const ulong r0 = 15831548891076708299;
    private  const ulong r1 = 4682191799977818424;
    private  const ulong r2 = 12294384630081346794;
    private  const ulong r3 = 785759240370973821;
    private static readonly Element rSquare = new Element(r0, r1, r2, r3);

    private const ulong g0 = 5415081136944170355;
    private const ulong g1 = 16923187137941795325;
    private const ulong g2 = 11911047149493888393;
    private const ulong g3 = 436996551065533341;
    private static readonly Element gResidue = new Element(g0, g1, g2, g3);

    private static Lazy<UInt256> _modulus = new Lazy<UInt256>(() =>
    {
        UInt256.TryParse("13108968793781547619861935127046491459309155893440570251786403306729687672801", out UInt256 output);
        return output;
    });
    public static Lazy<UInt256> _bLegendreExponentElement = new Lazy<UInt256>(() =>
    {
        UInt256.TryParse("e7db4ea6533afa906673b0101343b007fc7c3803a0c8238ba7e835a943b73f0", out UInt256 output);
        return output;
    });
    public static Lazy<UInt256> _bSqrtExponentElement = new Lazy<UInt256>(() =>
    {
        UInt256.TryParse("73eda753299d7d483339d80809a1d803fe3e1c01d06411c5d3f41ad4a1db9f", out UInt256 output);
        return output;
    });

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
        ElementUtils.FromBytes(bytes, isBigEndian, out u0, out u1, out u2, out u3);
    }

    public Span<byte> ToBytes() => ElementUtils.ToLittleEndian(u0, u1, u2, u3);
    public Span<byte> ToBytesBigEndian() => ElementUtils.ToBigEndian(u0, u1, u2, u3);

    public static Element? FromBytes(byte[] byteEncoded, bool isBigEndian=false)
    {
        ElementUtils.FromBytes(byteEncoded, isBigEndian, out ulong u0, out ulong u1, out ulong u2, out ulong u3);
        Element item = new Element(u0, u1, u2, u3);
        return item > qElement ? null : item;
    }

    public static Element FromBytesReduced(byte[] byteEncoded, bool isBigEndian=false)
    {
        ElementUtils.FromBytes(byteEncoded, isBigEndian, out ulong u0, out ulong u1, out ulong u2, out ulong u3);
        return new Element(u0, u1, u2, u3);
    }

    public bool IsZero => (u0 | u1 | u2 | u3) == 0;

    public bool IsOne => Equals(One);

    public static bool Sqrt(in Element x, out Element z)
    {
        Exp(in x, _bSqrtExponentElement.Value, out var w);
        MulMod(x, w, out var y);
        MulMod(w, y, out var b);

        ulong r = 5;
        Element t = b;

        for (ulong i = 0; i < r - 1; i++)
        {
            MulMod(in t, in t, out t);
        }

        if (t.IsZero)
        {
            z = Zero;
            return true;
        }

        if (!t.IsOne)
        {
            z = Zero;
            return false;
        }

        while (true)
        {
            ulong m = 0;
            t = b;

            if (!t.IsOne)
            {
                Sqrt(in t, out t);
                m++;
            }

            if (m == 0)
            {
                z = y;
                return true;
            }
            int ge = (int)(r - m - 1);
            t = gResidue;

            while (ge > 0)
            {
                MulMod(in t, in t, out t);
                ge--;
            }

            MulMod(in t, in t, out Element g);
            MulMod(in y, in t, out y);
            MulMod(in b, in g, out b);
            r = m;
        }
    }

    public static int Legendre(in Element z)
    {
        Exp(z, _bLegendreExponentElement.Value, out Element res);
        if (res.IsZero) return 0;

        if (res.IsOne) return 1;
        return -1;
    }

    public static void Exp(in Element b, in UInt256 e, out Element result)
    {
        result = One;
        Element bs = b;
        int len = e.BitLen;
        for (int i = 0; i < len; i++)
        {
            if (e.Bit(i))
            {
                MulMod(result, bs, out result);
            }
            MulMod(bs, bs, out bs);
        }
    }

    public static Element[] MultiInverse(Element[] values)
    {
        if (values.Length == 0) return Array.Empty<Element>();

        Element[] results = new Element[values.Length];
        bool[] zeros = new bool[values.Length];

        Element accumulator = One;

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i].IsZero)
            {
                zeros[i] = true;
                continue;
            }
            results[i] = accumulator;
            MulMod(in accumulator, in values[i], out accumulator);
        }

        Inverse(in accumulator, out accumulator);

        for (int i = values.Length - 1; i >= 0; i--)
        {
            if(zeros[i]) continue;
            MulMod(in results[i], in accumulator, out results[i]);
            MulMod(in accumulator, in values[i], out accumulator);
        }

        return values;
    }

    public static void ToMont(in Element x, out Element z)
    {
        MulMod(x, rSquare, out z);
    }

    public static void ToRegular(in Element x, out Element z)
    {
        FromMont(in x, out z);
    }

    public static void FromMont(in Element x, out Element res)
    {
        ulong[] z = new ulong[4];
        z[0] = x[0];
        z[1] = x[1];
        z[2] = x[2];
        z[3] = x[3];
        ulong m, C;

        m = z[0] * qInvNeg;
        C = MAdd0(m, q0, z[0]);
        (C, z[0]) = MAdd2(m, q1, z[1], C);
        (C, z[1]) = MAdd2(m, q2, z[2], C);
        (C, z[2]) = MAdd2(m, q3, z[3], C);
        z[3] = C;

        m = z[0] * qInvNeg;
        C = MAdd0(m, q0, z[0]);
        (C, z[0]) = MAdd2(m, q1, z[1], C);
        (C, z[1]) = MAdd2(m, q2, z[2], C);
        (C, z[2]) = MAdd2(m, q3, z[3], C);
        z[3] = C;

        m = z[0] * qInvNeg;
        C = MAdd0(m, q0, z[0]);
        (C, z[0]) = MAdd2(m, q1, z[1], C);
        (C, z[1]) = MAdd2(m, q2, z[2], C);
        (C, z[2]) = MAdd2(m, q3, z[3], C);
        z[3] = C;

        m = z[0] * qInvNeg;
        C = MAdd0(m, q0, z[0]);
        (C, z[0]) = MAdd2(m, q1, z[1], C);
        (C, z[1]) = MAdd2(m, q2, z[2], C);
        (C, z[2]) = MAdd2(m, q3, z[3], C);
        z[3] = C;

        if (LessThan(qElement, z))
        {
            ulong b = 0;
            SubtractWithBorrow(z[0], q0, ref b, out z[0]);
            SubtractWithBorrow(z[1], q1, ref b, out z[1]);
            SubtractWithBorrow(z[2], q2, ref b, out z[2]);
            SubtractWithBorrow(z[3], q3, ref b, out z[3]);
        }
        res = z;
    }


    public static void Inverse(in Element x, out Element z)
    {
        if (x.IsZero)
        {
            z = Zero;
            return;
        }

        // initialize u = q
        Element u = qElement;
        // initialize s = r^2
        Element s = rSquare;
        Element r = new Element();
        Element v = x;


        while (true)
        {
            while ((v[0] & 1) == 0)
            {
                v >>= 1;
                if ((s[0] & 1) == 1) s += qElement;
                s >>= 1;
            }

            while ((u[0] & 1) == 0)
            {
                u >>= 1;
                if ((r[0] & 1) == 1) r += qElement;
                r >>= 1;
            }

            if (!LessThan(v, u))
            {
                v -= u;
                if (SubtractUnderflow(s, r, out s)) s += qElement;
            }
            else
            {
                u -= v;
                if (SubtractUnderflow(r, s, out r)) r += qElement;
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
            c[2] = MAdd0(m, q0, c[0]);
            (c[1], c[0]) = MAdd1(v, y[1], c[1]);
            (c[2], t[0]) = MAdd2(m, q1, c[2], c[0]);
            (c[1], c[0]) = MAdd1(v, y[2], c[1]);
            (c[2], t[1]) = MAdd2(m, q2, c[2], c[0]);
            (c[1], c[0]) = MAdd1(v, y[3], c[1]);
            (t[3], t[2]) = MAdd3(m, q3, c[0], c[2], c[1]);
        }
        {
            // round 1
            ulong v = x[1];
            (c[1], c[0]) = MAdd1(v, y[0], t[0]);
            ulong m = (c[0] * qInvNeg);
            c[2] = MAdd0(m, q0, c[0]);
            (c[1], c[0]) = MAdd2(v, y[1], c[1], t[1]);
            (c[2], t[0]) = MAdd2(m, q1, c[2], c[0]);
            (c[1], c[0]) = MAdd2(v, y[2], c[1], t[2]);
            (c[2], t[1]) = MAdd2(m, q2, c[2], c[0]);
            (c[1], c[0]) = MAdd2(v, y[3], c[1], t[3]);
            (t[3], t[2]) = MAdd3(m, q3, c[0], c[2], c[1]);
        }
        {
            // round 2

            ulong v = x[2];
            (c[1], c[0]) = MAdd1(v, y[0], t[0]);
            ulong m = (c[0] * qInvNeg);
            c[2] = MAdd0(m, q0, c[0]);
            (c[1], c[0]) = MAdd2(v, y[1], c[1], t[1]);
            (c[2], t[0]) = MAdd2(m, q1, c[2], c[0]);
            (c[1], c[0]) = MAdd2(v, y[2], c[1], t[2]);
            (c[2], t[1]) = MAdd2(m, q2, c[2], c[0]);
            (c[1], c[0]) = MAdd2(v, y[3], c[1], t[3]);
            (t[3], t[2]) = MAdd3(m, q3, c[0], c[2], c[1]);
        }
        {
            // round 3

            ulong v = x[3];
            (c[1], c[0]) = MAdd1(v, y[0], t[0]);
            ulong m = (c[0] * qInvNeg);
            c[2] = MAdd0(m, q0, c[0]);
            (c[1], c[0]) = MAdd2(v, y[1], c[1], t[1]);
            (c[2], z[0]) = MAdd2(m, q1, c[2], c[0]);
            (c[1], c[0]) = MAdd2(v, y[2], c[1], t[2]);
            (c[2], z[1]) = MAdd2(m, q2, c[2], c[0]);
            (c[1], c[0]) = MAdd2(v, y[3], c[1], t[3]);
            (z[3], z[2]) = MAdd3(m, q3, c[0], c[2], c[1]);
        }
        if (LessThan(qElement, z))
        {
            ulong b = 0;
            SubtractWithBorrow(z[0], q0, ref b, out z[0]);
            SubtractWithBorrow(z[1], q1, ref b, out z[1]);
            SubtractWithBorrow(z[2], q2, ref b, out z[2]);
            SubtractWithBorrow(z[3], q3, ref b, out z[3]);
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
    public int BitLen =>
        u3 != 0
            ? 192 + Len64(u3)
            : u2 != 0
                ? 128 + Len64(u2)
                : u1 != 0
                    ? 64 + Len64(u1)
                    : Len64(u0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Len64(ulong x)
    {
        int n = 0;
        if (x >= (1ul << 32))
        {
            x >>= 32;
            n = 32;
        }
        if (x >= (1ul << 16))
        {
            x >>= 16;
            n += 16;
        }
        if (x >= (1ul << 8))
        {
            x >>= 8;
            n += 8;
        }

        return n + len8tab[x];
    }
    private static readonly byte[] len8tab = new byte[] {
        0x00, 0x01, 0x02, 0x02, 0x03, 0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
        0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
        0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
        0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
        0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
        0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
        0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
        0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
        0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
        0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
        0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
        0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
        0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
        0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
        0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
        0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
    };


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
