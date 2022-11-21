// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

using System.Numerics;
using Nethermind.Int256;

namespace Nethermind.MontgomeryField;

public class BaseFieldConstants
{
    public static readonly ulong[] One = new ulong[]
    {
        6347764673676886264,
        253265890806062196,
        11064306276430008312,
        1739710354780652911
    };
    public static readonly ulong[] qElement = new ulong[]
    {
        8429901452645165025,
        18415085837358793841,
        922804724659942912,
        2088379214866112338
    };
    public static readonly ulong[] rSquare = new ulong[]
    {
        15831548891076708299,
        4682191799977818424,
        12294384630081346794,
        785759240370973821,
    };
    public static readonly ulong[] qMinOneDiv2 = new ulong[]
    {
        13438322763177358321,
        9207542918679396920,
        461402362329971456,
        1044189607433056169,
    };
    public static readonly ulong[] gNonResidue = new ulong[]
    {
        11289237133041595516,
        2081200955273736677,
        967625415375836421,
        4543825880697944938,
    };
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

    public static ulong qInvNeg = 17410672245482742751;
    const int Limbs = 4;
    const int Bits = 253;
    const int Bytes = Limbs * 8;
}

public class ScalarFieldConstants
{
    public static readonly ulong[] One = new ulong[]
    {
        8589934590,
        6378425256633387010,
        11064306276430008309,
        1739710354780652911
    };
    public static readonly ulong[] qElement = new ulong[]
    {
        18446744069414584321,
        6034159408538082302,
        3691218898639771653,
        8353516859464449352
    };
    public static readonly ulong[] rSquare = new ulong[]
    {
        14526898881837571181,
        3129137299524312099,
        419701826671360399,
        524908885293268753,
    };
    public static readonly ulong[] qMinOneDiv2 = new ulong[]
    {
        9223372034707292161,
        12240451741123816959,
        1845609449319885826,
        4176758429732224676,
    };
    public static readonly ulong[] gNonResidue = new ulong[]
    {
        11289237133041595516,
        2081200955273736677,
        967625415375836421,
        4543825880697944938,
    };
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

    public static ulong qInvNeg = 18446744069414584319;
    const int Limbs = 4;
    const int Bits = 255;
    const int Bytes = Limbs * 8;
}
