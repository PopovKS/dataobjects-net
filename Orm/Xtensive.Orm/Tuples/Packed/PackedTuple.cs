// Copyright (C) 2003-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2012.12.29

using System;

namespace Xtensive.Tuples.Packed
{
  [Serializable]
  internal sealed class PackedTuple : RegularTuple
  {
    private static readonly object[] EmptyObjectArray = new object[0];

    public readonly TupleDescriptor PackedDescriptor;
    public readonly long[] Values;
    public readonly object[] Objects;

    public override TupleDescriptor Descriptor
    {
      get { return PackedDescriptor; }
    }

    public override Tuple Clone()
    {
      return new PackedTuple(this);
    }

    public override Tuple CreateNew()
    {
      return new PackedTuple(PackedDescriptor);
    }

    public override bool Equals(Tuple other)
    {
      var packedOther = other as PackedTuple;
      if (packedOther==null)
        return base.Equals(other);

      if (ReferenceEquals(packedOther, this))
        return true;
      if (Descriptor!=packedOther.Descriptor)
        return false;

      var fieldDescriptors = PackedDescriptor.FieldDescriptors;
      var count = Count;
      for (int i = 0; i < count; i++) {
        ref var descriptor = ref fieldDescriptors[i];
        var thisState = GetFieldState(descriptor);
        var otherState = packedOther.GetFieldState(descriptor);
        if (thisState!=otherState)
          return false;
        if (thisState!=TupleFieldState.Available)
          continue;
        var accessor = descriptor.Accessor;
        if (!accessor.ValueEquals(this, descriptor, packedOther, descriptor))
          return false;
      }

      return true;
    }

    public override int GetHashCode()
    {
      var count = Count;
      var fieldDescriptors = PackedDescriptor.FieldDescriptors;
      int result = 0;
      for (int i = 0; i < count; i++) {
        ref var descriptor = ref fieldDescriptors[i];
        var state = GetFieldState(descriptor);
        var fieldHash = state == TupleFieldState.Available
          ? descriptor.Accessor.GetValueHashCode(this, descriptor)
          : 0;
        result = HashCodeMultiplier * result ^ fieldHash;
      }
      return result;
    }

    public override TupleFieldState GetFieldState(int fieldIndex)
    {
      return GetFieldState(PackedDescriptor.FieldDescriptors[fieldIndex]);
    }

    protected internal override void SetFieldState(int fieldIndex, TupleFieldState fieldState)
    {
      if (fieldState==TupleFieldState.Null) {
        throw new ArgumentOutOfRangeException(nameof(fieldState));
      }

      SetFieldState(PackedDescriptor.FieldDescriptors[fieldIndex], fieldState);
    }

    public override object GetValue(int fieldIndex, out TupleFieldState fieldState)
    {
      ref var descriptor = ref PackedDescriptor.FieldDescriptors[fieldIndex];
      return descriptor.Accessor.GetUntypedValue(this, descriptor, out fieldState);
    }

    public override void SetValue(int fieldIndex, object fieldValue)
    {
      ref var descriptor = ref PackedDescriptor.FieldDescriptors[fieldIndex];
      descriptor.Accessor.SetUntypedValue(this, descriptor, fieldValue);
    }

    public void SetFieldState(in PackedFieldDescriptor d, TupleFieldState fieldState)
    {
      var bits = (long) fieldState;
      ref var block = ref Values[d.StateIndex];
      block = (block & ~(3L << d.StateBitOffset)) | (bits << d.StateBitOffset);

      if (fieldState!=TupleFieldState.Available && d.IsObjectField) {
        Objects[d.ObjectIndex] = null;
      }
    }

    public TupleFieldState GetFieldState(in PackedFieldDescriptor d)
    {
      var block = Values[d.StateIndex];
      return (TupleFieldState) ((block >> d.StateBitOffset) & 3);
    }

    public PackedTuple(TupleDescriptor descriptor)
    {
      PackedDescriptor = descriptor;

      Values = new long[PackedDescriptor.ValuesLength];
      Objects = PackedDescriptor.ObjectsLength > 0
        ? new object[PackedDescriptor.ObjectsLength]
        : EmptyObjectArray;
    }

    private PackedTuple(PackedTuple origin)
    {
      PackedDescriptor = origin.PackedDescriptor;

      Values = (long[]) origin.Values.Clone();
      Objects = PackedDescriptor.ObjectsLength > 0
        ? (object[]) origin.Objects.Clone()
        : EmptyObjectArray;
    }
  }
}