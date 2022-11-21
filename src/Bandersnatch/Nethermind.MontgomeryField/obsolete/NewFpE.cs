// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nethermind.MontgomeryField.obsolete;


[Obsolete("Performance Issues, kept for benchmarking")]
[StructLayout(LayoutKind.Explicit)]
public readonly struct NewFpE
{
    // CONSTANTS S----------------------------------------------------------------------------------------------------------------------
    private static readonly NewFpE _one = new ulong[]
    {
        6347764673676886264,
        253265890806062196,
        11064306276430008312,
        1739710354780652911
    };
    public static readonly NewFpE qElement = new ulong[]
    {
        8429901452645165025,
        18415085837358793841,
        922804724659942912,
        2088379214866112338
    };
    public static readonly NewFpE rSquare = new ulong[]
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

    const int Limbs = 4;
    const int Bits = 253;
    const int Bytes = Limbs * 8;
    // CONSTANTS E----------------------------------------------------------------------------------------------------------------------

    public static readonly NewFpE Zero = 0ul;
    public static NewFpE One => _one;

    public static implicit operator NewFpE(ulong value) => new NewFpE(value, 0ul, 0ul, 0ul);
    public static implicit operator NewFpE(ulong[] value) => new NewFpE(value[0], value[1], value[2], value[3]);

    /* in little endian order so u3 is the most significant ulong */
    [FieldOffset(0)]
    private readonly ulong u0;
    [FieldOffset(8)]
    private readonly ulong u1;
    [FieldOffset(16)]
    private readonly ulong u2;
    [FieldOffset(24)]
    private readonly ulong u3;

    public NewFpE(ulong u0 = 0, ulong u1 = 0, ulong u2 = 0, ulong u3 = 0)
    {
        this.u0 = u0;
        this.u1 = u1;
        this.u2 = u2;
        this.u3 = u3;
    }

    public bool IsZero => (u0 | u1 | u2 | u3) == 0;
    public bool IsOne => (u0 == One[0]) && (u1 == One[1]) && (u2 == One[2]) && (u3 == One[3]);


    public static void Divide(NewFpE x, NewFpE y, out NewFpE z)
    {
        Inverse(y, out NewFpE yInv);
        MultiplyModGeneric(x, yInv, out z);
    }

    public static void Inverse(in NewFpE x, out NewFpE z)
    {
        if (x.IsZero)
        {
            z = Zero;
            return;
        }

        NewFpE u = qElement;
        NewFpE s = rSquare;
        NewFpE q = qElement;
        NewFpE r = new NewFpE();

        NewFpE v = x;

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


    public static void MultiplyModGeneric(in NewFpE x, in NewFpE y, out NewFpE res)
    {
        ulong[] t = new ulong[4];
        ulong[] c = new ulong[3];
        ulong[] z = new ulong[4];

        {
            // round 0

            ulong v = x[0];

            (c[1], c[0]) = Arithmetic.Multiply64(v, y[0]);

            ulong m = c[0] * qInvNeg;

            c[2] = Arithmetic.MAdd0(m, qElement[0], c[0]);

            (c[1], c[0]) = Arithmetic.MAdd1(v, y[1], c[1]);

            (c[2], t[0]) = Arithmetic.MAdd2(m, qElement[1], c[2], c[0]);

            (c[1], c[0]) = Arithmetic.MAdd1(v, y[2], c[1]);

            (c[2], t[1]) = Arithmetic.MAdd2(m, qElement[2], c[2], c[0]);

            (c[1], c[0]) = Arithmetic.MAdd1(v, y[3], c[1]);

            (t[3], t[2]) = Arithmetic.MAdd3(m, qElement[3], c[0], c[2], c[1]);


        }
        {
            // round 1

            ulong v = x[1];

            (c[1], c[0]) = Arithmetic.MAdd1(v, y[0], t[0]);

            ulong m = (c[0] * qInvNeg);

            c[2] = Arithmetic.MAdd0(m, qElement[0], c[0]);

            (c[1], c[0]) = Arithmetic.MAdd2(v, y[1], c[1], t[1]);

            (c[2], t[0]) = Arithmetic.MAdd2(m, qElement[1], c[2], c[0]);

            (c[1], c[0]) = Arithmetic.MAdd2(v, y[2], c[1], t[2]);

            (c[2], t[1]) = Arithmetic.MAdd2(m, qElement[2], c[2], c[0]);

            (c[1], c[0]) = Arithmetic.MAdd2(v, y[3], c[1], t[3]);

            (t[3], t[2]) = Arithmetic.MAdd3(m, qElement[3], c[0], c[2], c[1]);
        }
        {
            // round 2

            ulong v = x[2];

            (c[1], c[0]) = Arithmetic.MAdd1(v, y[0], t[0]);

            ulong m = (c[0] * qInvNeg);

            c[2] = Arithmetic.MAdd0(m, qElement[0], c[0]);

            (c[1], c[0]) = Arithmetic.MAdd2(v, y[1], c[1], t[1]);

            (c[2], t[0]) = Arithmetic.MAdd2(m, qElement[1], c[2], c[0]);

            (c[1], c[0]) = Arithmetic.MAdd2(v, y[2], c[1], t[2]);

            (c[2], t[1]) = Arithmetic.MAdd2(m, qElement[2], c[2], c[0]);

            (c[1], c[0]) = Arithmetic.MAdd2(v, y[3], c[1], t[3]);

            (t[3], t[2]) = Arithmetic.MAdd3(m, qElement[3], c[0], c[2], c[1]);


        }
        {
            // round 3

            ulong v = x[3];

            (c[1], c[0]) = Arithmetic.MAdd1(v, y[0], t[0]);

            ulong m = (c[0] * qInvNeg);

            c[2] = Arithmetic.MAdd0(m, qElement[0], c[0]);

            (c[1], c[0]) = Arithmetic.MAdd2(v, y[1], c[1], t[1]);

            (c[2], z[0]) = Arithmetic.MAdd2(m, qElement[1], c[2], c[0]);

            (c[1], c[0]) = Arithmetic.MAdd2(v, y[2], c[1], t[2]);

            (c[2], z[1]) = Arithmetic.MAdd2(m, qElement[2], c[2], c[0]);

            (c[1], c[0]) = Arithmetic.MAdd2(v, y[3], c[1], t[3]);

            (z[3], z[2]) = Arithmetic.MAdd3(m, qElement[3], c[0], c[2], c[1]);


        }
        if (!(z[3] < 2088379214866112338 || (z[3] == 2088379214866112338 && (z[2] < 922804724659942912 || (z[2] == 922804724659942912 && (z[1] < 18415085837358793841 || (z[1] == 18415085837358793841 && (z[0] < 8429901452645165025))))))))
        {

            ulong b = 0;
            Arithmetic.SubtractWithBorrow(z[0], qElement[0], ref b, out z[0]);
            Arithmetic.SubtractWithBorrow(z[1], qElement[1], ref b, out z[1]);
            Arithmetic.SubtractWithBorrow(z[2], qElement[2], ref b, out z[2]);
            Arithmetic.SubtractWithBorrow(z[3], qElement[3], ref b, out z[3]);
        }
        res = z;
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


    public static bool SubtractUnderflow(in NewFpE a, in NewFpE b, out NewFpE res)
    {
        ulong borrow = 0;
        Arithmetic.SubtractWithBorrow(a[0], b[0], ref borrow, out ulong z0);
        Arithmetic.SubtractWithBorrow(a[1], b[1], ref borrow, out ulong z1);
        Arithmetic.SubtractWithBorrow(a[2], b[2], ref borrow, out ulong z2);
        Arithmetic.SubtractWithBorrow(a[3], b[3], ref borrow, out ulong z3);
        res = new NewFpE(z0, z1, z2, z3);
        return borrow != 0;
    }




    public static void Lsh(in NewFpE x, int n, out NewFpE res)
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

        res = new NewFpE(z0, z1, z2, z3);
    }

    public void LeftShift(int n, out NewFpE res)
    {
        Lsh(this, n, out res);
    }

    public static NewFpE operator <<(in NewFpE a, int n)
    {
        a.LeftShift(n, out NewFpE res);
        return res;
    }

    public bool Bit(int n)
    {
        int bucket = (n / 64) % 4;
        int position = n % 64;
        return (this[bucket] & ((ulong)1 << position)) != 0;
    }

    public static void Rsh(in NewFpE x, int n, out NewFpE res)
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

        res = new NewFpE(z0, z1, z2, z3);
    }

    public void RightShift(int n, out NewFpE res) => Rsh(this, n, out res);

    public static NewFpE operator >>(in NewFpE a, int n)
    {
        a.RightShift(n, out NewFpE res);
        return res;
    }

    internal void Lsh64(out NewFpE res)
    {
        res = new NewFpE(0, u0, u1, u2);
    }

    internal void Lsh128(out NewFpE res)
    {
        res = new NewFpE(0, 0, u0, u1);
    }

    internal void Lsh192(out NewFpE res)
    {
        res = new NewFpE(0, 0, 0, u0);
    }

    internal void Rsh64(out NewFpE res)
    {
        res = new NewFpE(u1, u2, u3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Rsh128(out NewFpE res)
    {
        res = new NewFpE(u2, u3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Rsh192(out NewFpE res)
    {
        res = new NewFpE(u3);
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


    public static NewFpE operator +(in NewFpE a, in NewFpE b)
    {
        Add(in a, in b, out NewFpE res);
        return res;
    }

    public static NewFpE operator -(in NewFpE a, in NewFpE b)
    {
        if (SubtractUnderflow(in a, in b, out NewFpE c))
        {
            throw new ArithmeticException($"Underflow in subtraction {a} - {b}");
        }

        return c;
    }

    // Add sets res to the sum a+b
    public static void Add(in NewFpE a, in NewFpE b, out NewFpE res)
    {
        ulong carry = 0ul;
        Arithmetic.AddWithCarry(a.u0, b.u0, ref carry, out ulong res1);
        Arithmetic.AddWithCarry(a.u1, b.u1, ref carry, out ulong res2);
        Arithmetic.AddWithCarry(a.u2, b.u2, ref carry, out ulong res3);
        Arithmetic.AddWithCarry(a.u3, b.u3, ref carry, out ulong res4);
        res = new NewFpE(res1, res2, res3, res4);
    }
}
