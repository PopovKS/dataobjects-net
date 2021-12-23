// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2008.06.04

using System;
using Xtensive.Collections;
using Xtensive.Core;


namespace Xtensive.Tuples.Transform.Internals
{
  /// <summary>
  /// A <see cref="MapTransform"/> result tuple mapping up to 3 source tuples to a single one (this).
  /// </summary>
  [Serializable]
  internal sealed class MapTransformTuple3 : TransformedTuple<MapTransform>
  {
    private readonly FixedReadOnlyList3<Tuple> tuples;

    #region GetFieldState, GetValue, SetValue methods

    /// <inheritdoc/>
    public override TupleFieldState GetFieldState(int fieldIndex)
    {
      var indexes = TupleTransform.map[fieldIndex];
      return tuples[indexes.First].GetFieldState(indexes.Second);
    }

    protected internal override void SetFieldState(int fieldIndex, TupleFieldState fieldState)
    {
      var indexes = TupleTransform.map[fieldIndex];
      tuples[indexes.First].SetFieldState(indexes.Second, fieldState);
    }

    /// <inheritdoc/>
    public override object GetValue(int fieldIndex, out TupleFieldState fieldState)
    {
      var indexes = TupleTransform.map[fieldIndex];
      return tuples[indexes.First].GetValue(indexes.Second, out fieldState);
    }

    /// <inheritdoc/>
    public override void SetValue(int fieldIndex, object fieldValue)
    {
      if (TupleTransform.IsReadOnly) {
        throw Exceptions.ObjectIsReadOnly(null);
      }
      var indexes = TupleTransform.map[fieldIndex];
      tuples[indexes.First].SetValue(indexes.Second, fieldValue);
    }

    #endregion

    protected internal override Pair<Tuple, int> GetMappedContainer(int fieldIndex, bool isWriting)
    {
      if (isWriting && TupleTransform.IsReadOnly) {
        throw Exceptions.ObjectIsReadOnly(null);
      }
      var map = TupleTransform.map[fieldIndex];
      return tuples[map.First].GetMappedContainer(map.Second, isWriting);
    }


    // Constructors

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="transform">The transform.</param>
    /// <param name="source1">First source tuple.</param>
    /// <param name="source2">2nd source tuple.</param>
    public MapTransformTuple3(MapTransform transform, Tuple source1, Tuple source2)
      : base(transform)
    {
      tuples = new FixedReadOnlyList3<Tuple>(source1, source2);
    }

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="transform">The transform.</param>
    /// <param name="source1">First source tuple.</param>
    /// <param name="source2">2nd source tuple.</param>
    /// <param name="source3">3rd source tuple.</param>
    public MapTransformTuple3(MapTransform transform, Tuple source1, Tuple source2, Tuple source3)
      : base(transform)
    {
      tuples = new FixedReadOnlyList3<Tuple>(source1, source2, source3);
    }
  }
}