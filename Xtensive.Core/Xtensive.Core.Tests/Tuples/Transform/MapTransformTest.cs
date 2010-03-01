// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.08.08

using NUnit.Framework;
using Xtensive.Core.Tuples;
using Xtensive.Core.Tuples.Transform;

namespace Xtensive.Core.Tests.Tuples.Transform
{
  [TestFixture]
  public class MapTransformTest
  {
    public void MainTest()
    {
      Tuple source = Tuple.Create(1);
      MapTransform transform = new MapTransform(true, TupleDescriptor.Create<byte, int, string>(), new[] {-1, 0});
      Tuple result = transform.Apply(TupleTransformType.TransformedTuple, source);
      Assert.AreEqual(TupleFieldState.Default, result.GetFieldState(0));
      Assert.AreEqual(TupleFieldState.Available, result.GetFieldState(1));
      Assert.AreEqual(TupleFieldState.Default, result.GetFieldState(2));
    }
  }
}