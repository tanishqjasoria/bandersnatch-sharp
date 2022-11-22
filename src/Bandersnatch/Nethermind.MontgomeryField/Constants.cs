// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

using System.Numerics;
using Nethermind.Int256;

namespace Nethermind.MontgomeryField;

public class FpConstants
{
    const int Limbs = 4;
    const int Bits = 255;
    const int Bytes = Limbs * 8;
    const ulong qInvNeg = 17410672245482742751;

    public static readonly FpE Zero = new FpE(0, 0, 0, 0);

    private const ulong one0 = 8589934590;
    private const ulong one1 = 6378425256633387010;
    private const ulong one2 = 11064306276430008309;
    private const ulong one3 = 1739710354780652911;
    public static readonly FpE One = new FpE(one0, one1, one2, one3);

    private const  ulong q0 = 18446744069414584321;
    private const  ulong q1 = 6034159408538082302;
    private const  ulong q2 = 3691218898639771653;
    private const  ulong q3 = 8353516859464449352;
    private static readonly FpE qElement = new FpE(q0, q1, q2, q3);

    private  const ulong r0 = 14526898881837571181;
    private  const ulong r1 = 3129137299524312099;
    private  const ulong r2 = 419701826671360399;
    private  const ulong r3 = 524908885293268753;
    private static readonly FpE rSquare = new FpE(r0, r1, r2, r3);

    private const ulong g0 = 11289237133041595516;
    private const ulong g1 = 2081200955273736677;
    private const ulong g2 = 967625415375836421;
    private const ulong g3 = 4543825880697944938;
    private static readonly FpE gResidue = new FpE(g0, g1, g2, g3);

    private const ulong qM0 = 9223372034707292161;
    private const ulong qM1 = 12240451741123816959;
    private const ulong qM2 = 1845609449319885826;
    private const ulong qM3 = 4176758429732224676;
    private static readonly FpE qMinOne = new FpE(qM0, qM1, qM2, qM3);

    public static Lazy<UInt256> _modulus = new Lazy<UInt256>(() =>
    {
        UInt256.TryParse("52435875175126190479447740508185965837690552500527637822603658699938581184513", out UInt256 output);
        return output;
    });
    public static Lazy<UInt256> _bLegendreExponentElement = new Lazy<UInt256>(() =>
    {
        UInt256.TryParse("39f6d3a994cebea4199cec0404d0ec02a9ded2017fff2dff7fffffff80000000", out UInt256 output);
        return output;
    });
    public static Lazy<UInt256> _bSqrtExponentElement = new Lazy<UInt256>(() =>
    {
        UInt256.TryParse("39f6d3a994cebea4199cec0404d0ec02a9ded2017fff2dff7fffffff", out UInt256 output);
        return output;
    });
}

public class FrConstants
{
    const int Limbs = 4;
    const int Bits = 253;
    const int Bytes = Limbs * 8;
    const ulong qInvNeg = 17410672245482742751;

    public static readonly FrE Zero = new FrE(0, 0, 0, 0);

    private const ulong one0 = 6347764673676886264;
    private const ulong one1 = 253265890806062196;
    private const ulong one2 = 11064306276430008312;
    private const ulong one3 = 1739710354780652911;
    public static readonly FrE One = new FrE(one0, one1, one2, one3);

    private const  ulong q0 = 8429901452645165025;
    private const  ulong q1 = 18415085837358793841;
    private const  ulong q2 = 922804724659942912;
    private const  ulong q3 = 2088379214866112338;
    private static readonly FrE qElement = new FrE(q0, q1, q2, q3);

    private  const ulong r0 = 15831548891076708299;
    private  const ulong r1 = 4682191799977818424;
    private  const ulong r2 = 12294384630081346794;
    private  const ulong r3 = 785759240370973821;
    private static readonly FrE rSquare = new FrE(r0, r1, r2, r3);

    private const ulong g0 = 5415081136944170355;
    private const ulong g1 = 16923187137941795325;
    private const ulong g2 = 11911047149493888393;
    private const ulong g3 = 436996551065533341;
    private static readonly FrE gResidue = new FrE(g0, g1, g2, g3);

    private const ulong qM0 = 13438322763177358321;
    private const ulong qM1 = 9207542918679396920;
    private const ulong qM2 = 461402362329971456;
    private const ulong qM3 = 1044189607433056169;
    private static readonly FrE qMinOne = new FrE(qM0, qM1, qM2, qM3);

    public static Lazy<UInt256> _modulus = new Lazy<UInt256>(() =>
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
}
