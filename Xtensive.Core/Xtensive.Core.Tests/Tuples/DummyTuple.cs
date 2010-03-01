// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.01.24

using System;
using System.Collections;
using Xtensive.Core.Tuples;

namespace Xtensive.Core.Tests.Tuples
{
  public class DummyTuple: Tuple
  {
    private TupleDescriptor descriptor;
    private BitArray available;
    private object[] values;

    public override TupleDescriptor Descriptor
    {
      get { return descriptor; }
    }

    public object this[int fieldIndex]
    {
      get { return GetValue(fieldIndex); }
      set { SetValue(fieldIndex, value); }
    }

    public override Tuple CreateNew()
    {
      return new DummyTuple(descriptor);
    }

    public override Tuple Clone()
    {
      DummyTuple tuple = (DummyTuple)CreateNew();
      tuple.values = (object[])values.Clone();
      tuple.available = available;
      return tuple;
    }

    public override TupleFieldState GetFieldState(int fieldIndex)
    {
      if (!available[fieldIndex])
        return 0;
      return values[fieldIndex] == null ? 
        TupleFieldState.Null | TupleFieldState.Available :
        TupleFieldState.Available;
    }

    public override object GetValue(int fieldIndex, out TupleFieldState fieldState)
    {
      fieldState = GetFieldState(fieldIndex);
      return values[fieldIndex];
    }

    public override void SetValue(int fieldIndex, object fieldValue)
    {
      if (fieldValue != null && !descriptor[fieldIndex].IsAssignableFrom(fieldValue.GetType()))
        throw new InvalidCastException();
      values[fieldIndex] = fieldValue;
      available[fieldIndex] = true;
    }


    // Constructors

    public DummyTuple(TupleDescriptor descriptor)
    {
      ArgumentValidator.EnsureArgumentNotNull(descriptor, "descriptor");
      this.descriptor = descriptor;
      values = new object[descriptor.Count];
      available = new BitArray(new bool[descriptor.Count]);
    }
  }
}