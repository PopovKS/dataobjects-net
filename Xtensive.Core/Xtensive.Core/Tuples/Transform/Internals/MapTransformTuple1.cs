// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2008.06.04

using System;
using Xtensive.Core.Internals.DocTemplates;

namespace Xtensive.Core.Tuples.Transform.Internals
{
  /// <summary>
  /// A <see cref="MapTransform"/> result tuple mapping 1 source tuple to a single one (this).
  /// </summary>
  [Serializable]
  public sealed class MapTransformTuple1 : TransformedTuple<MapTransform>
  {
    private Tuple tuple;

    /// <inheritdoc/>
    public override object[] Arguments {
      get {
        Tuple[] copy = new Tuple[1];
        copy[0] = tuple;
        return copy;
      }
    }

    #region GetFieldState, GetValueOrDefault, SetValue methods

    /// <inheritdoc/>
    public override TupleFieldState GetFieldState(int fieldIndex)
    {
      int index = GetMappedFieldIndex(fieldIndex);
      return index == -1 ? TupleFieldState.Default : tuple.GetFieldState(index);
    }

    /// <inheritdoc/>
    public override object GetValueOrDefault(int fieldIndex)
    {
      int index = GetMappedFieldIndex(fieldIndex);
      if (index == -1)
        return Descriptor[index].IsClass ? null : Activator.CreateInstance(Descriptor[index]);
      return tuple.GetValueOrDefault(index);
    }

    /// <inheritdoc/>
    public override T GetValueOrDefault<T>(int fieldIndex)
    {
      int index = GetMappedFieldIndex(fieldIndex);
      if (index == -1) {
        if (!typeof(T).IsAssignableFrom(Descriptor[index]))
          throw new InvalidCastException();
        return default(T);
      }
      return tuple.GetValueOrDefault<T>(index);
    }

    /// <inheritdoc/>
    public override void SetValue<T>(int fieldIndex, T fieldValue)
    {
      if (Transform.IsReadOnly)
        throw Exceptions.ObjectIsReadOnly(null);
      tuple.SetValue(GetMappedFieldIndex(fieldIndex), fieldValue);
    }

    /// <inheritdoc/>
    public override void SetValue(int fieldIndex, object fieldValue)
    {
      if (Transform.IsReadOnly)
        throw Exceptions.ObjectIsReadOnly(null);
      tuple.SetValue(GetMappedFieldIndex(fieldIndex), fieldValue);
    }

    #endregion

    private int GetMappedFieldIndex(int fieldIndex)
    {
      return TypedTransform.singleSourceMap[fieldIndex];
    }


    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="transform">The transform.</param>
    /// <param name="source">Source tuple.</param>
    public MapTransformTuple1(MapTransform transform, Tuple source)
      : base(transform)
    {
      tuple = source;
    }
  }
}