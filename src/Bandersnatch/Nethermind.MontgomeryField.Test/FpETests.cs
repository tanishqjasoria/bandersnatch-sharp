// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

using NUnit.Framework;
using Nethermind.Int256;

namespace Nethermind.MontgomeryField.Test;

[TestFixture]
public class FpETests
{
    [Test]
    public void TestInverse()
    {
        for (int i = 0; i < 100; i++)
        {
            UInt256.TryParse("8821992414909496695555237858960650346645906663732611838901686824314168704", out UInt256 test);

            FrE x = FrE.SetElement(
                test.u0,
                test.u1,
                test.u2,
                test.u3
            );

            FrE z = FrE.SetElement(
                test.u0,
                test.u1,
                test.u2,
                test.u3
            );

            FrE.Inverse(x, out FrE y);
            FrE.Inverse(y, out FrE res);
            Assert.IsTrue(z.Equals(res));
            // Assert.IsTrue(
            //     z.u0 == zz.Value.u0 &&
            //     z.Value.u1 == zz.Value.u1 &&
            //     z.Value.u2 == zz.Value.u2 &&
            //     z.Value.u3 == zz.Value.u3
            // );
        }
    }

    [Test]
    public void TestMul()
    {
        for (int i = 0; i < 100; i++)
        {
            UInt256.TryParse("8821992414909496695555237858960650346645906663732611838901686824314168704", out UInt256 test);

            FrE x = FrE.SetElement(
                test.u0,
                test.u1,
                test.u2,
                test.u3
            );
            FrE.MulMod(x , x, out var res);

            // FrE.Inverse(x, out FrE y);
            // FrE.Inverse(y, out FrE res);
            // Assert.IsTrue(z.Equals(res));
            // Assert.IsTrue(
            //     z.u0 == zz.Value.u0 &&
            //     z.Value.u1 == zz.Value.u1 &&
            //     z.Value.u2 == zz.Value.u2 &&
            //     z.Value.u3 == zz.Value.u3
            // );
        }
    }
}
