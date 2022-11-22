// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0. For full terms, see LICENSE in the project root.

using System.Collections.Generic;
using NUnit.Framework;

namespace Nethermind.Field.Montgomery.Test;

[TestFixture]
public class ElementTests
{
    [Test]
    public void TestAddition()
    {
        using IEnumerator<Element> randomElements = Element.SetRandom().GetEnumerator();

        Element element = randomElements.Current;
        randomElements.MoveNext();

        for (int i = 0; i < 1000; i++)
        {
            element += randomElements.Current;
            Assert.IsTrue(Element.LessThanSubModulus(element));
            randomElements.MoveNext();
        }
    }

    [Test]
    public void TestSub()
    {
        using IEnumerator<Element> randomElements = Element.SetRandom().GetEnumerator();

        Element element = randomElements.Current;
        randomElements.MoveNext();

        for (int i = 0; i < 1000; i++)
        {
            element -= randomElements.Current;
            Assert.IsTrue(Element.LessThanSubModulus(element));
            randomElements.MoveNext();
        }
    }

    [Test]
    public void TestMul()
    {
        using IEnumerator<Element> randomElements = Element.SetRandom().GetEnumerator();

        Element element = randomElements.Current;
        randomElements.MoveNext();

        for (int i = 0; i < 1000; i++)
        {
            element *= randomElements.Current;
            Assert.IsTrue(Element.LessThanSubModulus(element));
            randomElements.MoveNext();
        }
    }


}
