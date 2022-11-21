// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Nethermind.Field;
using Nethermind.Int256;
using Nethermind.Int256.Benchmark;
using Nethermind.Int256.Test;
using Nethermind.MontgomeryField;

namespace ModularArithmetic;
using TestElement = FpE;

public class BenchmarkBase
{
    public IEnumerable<BigInteger> Values => Enumerable.Concat(new[] { Numbers.UInt256Max }, UnaryOps.RandomUnsigned(1));
    public IEnumerable<UInt256> ValuesUint256 => Values.Select(x => (UInt256)x);
    public IEnumerable<TestElement> ValuesElement => Values.Select(x => (TestElement)x);
    public IEnumerable<(BigInteger, UInt256, TestElement)> ValuesTuple => Values.Select(x => (x, (UInt256)x, (TestElement)x));

    public IEnumerable<int> ValuesInt => UnaryOps.RandomInt(3);

    public static UInt256 uMod = FpE._modulus.Value;
    public static BigInteger bMod = (BigInteger)uMod;

}

public class IntTwoParamBenchmarkBase : BenchmarkBase
{
    [ParamsSource(nameof(ValuesTuple))]
    public (BigInteger, UInt256, TestElement) A;

    [ParamsSource(nameof(ValuesInt))]
    public int D;
}

public class TwoParamBenchmarkBase : BenchmarkBase
{
    [ParamsSource(nameof(ValuesTuple))]
    public (BigInteger, UInt256, TestElement) A;

    [ParamsSource(nameof(ValuesTuple))]
    public (BigInteger, UInt256, TestElement) B;
}

public class ThreeParamBenchmarkBase : TwoParamBenchmarkBase
{
    [ParamsSource(nameof(ValuesTuple))]
    public (BigInteger, UInt256, TestElement) C;
}

[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
public class AddMod : ThreeParamBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public BigInteger AddMod_BigInteger()
    {
        return ((A.Item1 + B.Item1) % bMod);
    }

    [Benchmark]
    public BigInteger AddMod_BigInteger_New()
    {
        return BigInteger.Remainder(A.Item1 + B.Item1, bMod);
    }

    [Benchmark]
    public UInt256 AddMod_UInt256()
    {
        UInt256.AddMod(A.Item2, B.Item2, uMod, out UInt256 res);
        return res;
    }

    [Benchmark]
    public TestElement AddMod_Element()
    {
        TestElement.AddMod(A.Item3, B.Item3, out TestElement res);
        return res;
    }
}

[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
public class SubtractMod : ThreeParamBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public BigInteger SubtractMod_BigInteger()
    {
        return ((A.Item1 - B.Item1) % bMod);
    }

    [Benchmark]
    public UInt256 SubtractMod_UInt256()
    {
        UInt256.SubtractMod(A.Item2, B.Item2, uMod, out UInt256 res);
        return res;
    }

    [Benchmark]
    public TestElement SubtractMod_Element()
    {
        TestElement.SubMod(A.Item3, B.Item3, out TestElement res);
        return res;
    }
}


[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
public class MultiplyMod : ThreeParamBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public BigInteger MultiplyMod_BigInteger()
    {
        return ((A.Item1 * B.Item1) % bMod);
    }

    [Benchmark]
    public UInt256 MultiplyMod_UInt256()
    {
        UInt256.MultiplyMod(A.Item2, B.Item2, uMod, out UInt256 res);
        return res;
    }

    [Benchmark]
    public TestElement MultiplyMod_Element()
    {
        TestElement.MulMod(A.Item3, B.Item3, out TestElement res);
        return res;
    }
}

[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
public class DivideMod : TwoParamBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public BigInteger Divide_BigInteger()
    {
        return (A.Item1 / B.Item1);
    }

    [Benchmark]
    public UInt256 Divide_UInt256()
    {
        UInt256.Divide(A.Item2, B.Item2, out UInt256 res);
        return res;
    }

    [Benchmark]
    public TestElement Divide_Element()
    {
        TestElement.Divide(A.Item3, B.Item3, out TestElement res);
        return res;
    }
}

[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
public class ExpMod : ThreeParamBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public BigInteger ExpMod_BigInteger()
    {
        return BigInteger.ModPow(A.Item1, B.Item1, bMod);
    }

    [Benchmark]
    public UInt256 ExpMod_UInt256()
    {
        UInt256.ExpMod(A.Item2, B.Item2, uMod, out UInt256 res);
        return res;
    }

    [Benchmark]
    public TestElement ExpMod_Element()
    {
        TestElement.Exp(A.Item3, B.Item2, out TestElement res);
        return res;
    }
}

[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
public class LeftShift : IntTwoParamBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public BigInteger LeftShift_BigInteger()
    {
        return (A.Item1 << D) % Numbers.TwoTo256;
    }

    [Benchmark]
    public UInt256 LeftShift_UInt256()
    {
        A.Item2.LeftShift(D, out UInt256 res);
        return res;
    }

    [Benchmark]
    public TestElement LeftShift_Element()
    {
        A.Item3.LeftShift(D, out TestElement res);
        return res;
    }
}

[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
public class RightShift : IntTwoParamBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public BigInteger RightShift_BigInteger()
    {
        return (A.Item1 >> D) % Numbers.TwoTo256;
    }

    [Benchmark]
    public UInt256 RightShift_UInt256()
    {
        A.Item2.RightShift(D, out UInt256 res);
        return res;
    }

    [Benchmark]
    public TestElement RightShift_Element()
    {
        A.Item3.RightShift(D, out TestElement res);
        return res;
    }
}

[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
public class Inverse : ThreeParamBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public BigInteger Inverse_BigInteger()
    {
        return BigInteger.ModPow(A.Item1, bMod - 2, bMod);
    }

    [Benchmark]
    public UInt256 Inverse_UInt256()
    {
        UInt256.ExpMod(A.Item2,  uMod - 2, uMod, out UInt256 res);
        return res;
    }

    [Benchmark]
    public TestElement Inverse_Element()
    {
        TestElement.Inverse(A.Item3, out TestElement res);
        return res;
    }
}

[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
public class Sqrt: ThreeParamBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public BigInteger Sqrt_BigInteger()
    {
        return BigInteger.ModPow(A.Item1, B.Item1, bMod);
    }

    [Benchmark]
    public UInt256? Sqrt_UInt256()
    {
        return FieldMethods.ModSqrt(A.Item2, uMod);
    }

    [Benchmark]
    public TestElement Sqrt_Element()
    {
        TestElement.Sqrt(A.Item3, out FpE res);
        return res;
    }
}
