// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Ivan Galkin
// Created:    2009.03.24

using System;
using System.Globalization;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core;
using System.Text;
using Xtensive.Core.Reflection;
using Xtensive.Modelling.Validation;
using Xtensive.Storage.Indexing.Model.Resources;

namespace Xtensive.Storage.Indexing.Model
{
  /// <summary>
  /// Type of table column.
  /// </summary>
  [Serializable]
  public sealed class TypeInfo : IEquatable<TypeInfo>,
    IValidatable,
    ICloneable
  {
    /// <summary>
    /// Gets the <see cref="TypeInfo"/> with undefined type.
    /// </summary>
    public static TypeInfo Undefined { get { return new TypeInfo(); } }

    /// <summary>
    /// Gets a value indicating whether type is undefined.
    /// </summary>
    public bool IsTypeUndefined { get { return Type==null; } }

    /// <summary>
    /// Gets the type of the data.
    /// </summary>
    public Type Type { get;  private set; }

    /// <summary>
    /// Indicates whether <see cref="Type"/> is nullable.
    /// </summary>
    public bool IsNullable { get; private set; }

    /// <summary>
    /// Gets the length.
    /// </summary>
    public int? Length { get;  private set; }

    /// <summary>
    /// Gets the culture.
    /// </summary>
    public CultureInfo Culture { get;  private set; }

    /// <summary>
    /// Gets the scale.
    /// </summary>
    public int? Scale { get;  private set; }

    /// <summary>
    /// Gets the precision.
    /// </summary>
    public int? Precision { get;  private set; }

    /// <inheritdoc/>
    public void Validate()
    {
      // TODO: Implement
    }

    #region Implementation of ICloneable

    /// <inheritdoc/>
    public object Clone()
    {
      if (IsTypeUndefined)
        return Undefined;
      var clone = new TypeInfo(Type, IsNullable);
      clone.Length = Length;
      clone.Culture = Culture;
      clone.Scale = Scale;
      clone.Precision = Precision;
      return clone;
    }

    #endregion

    #region Equality members

    /// <inheritdoc/>
    public bool Equals(TypeInfo other)
    {
      if (IsTypeUndefined)
        return false;
      if (ReferenceEquals(null, other))
        return false;
      if (ReferenceEquals(this, other))
        return true;
      if (other.IsTypeUndefined)
        return false;
      var isEqual =
        other.Type==Type &&
        other.IsNullable==IsNullable &&
        other.Scale==Scale &&
        other.Precision==Precision;
      if (Length.HasValue && other.Length.HasValue)
        isEqual &= other.Length==Length;

      return isEqual;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
      if (IsTypeUndefined)
        return false;
      if (ReferenceEquals(null, obj))
        return false;
      if (ReferenceEquals(this, obj))
        return true;
      if (obj.GetType()!=typeof (TypeInfo))
        return false;
      return Equals((TypeInfo) obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
      unchecked {
        int result = (Type!=null ? Type.GetHashCode() : 0);
        result = (result * 397) ^ (IsNullable ? 1 : 0);
        if (Length.HasValue)
          result = (result * 397) ^ Length.Value;
        if (Scale.HasValue)
          result = (result * 397) ^ Scale.Value;
        if (Precision.HasValue)
          result = (result * 397) ^ Precision.Value;
        if (Culture!=null)
          result = (result * 397) ^ Culture.GetHashCode();
        return result;
      }
    }

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(TypeInfo left, TypeInfo right)
    {
      return Equals(left, right);
    }

    /// <summary>
    /// Implements the operator !=.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(TypeInfo left, TypeInfo right)
    {
      return !Equals(left, right);
    }

    #endregion

    /// <inheritdoc/>
    public override string ToString()
    {
      var sb = new StringBuilder();
      var type = Type;
      if (type.IsNullable())
        type = type.GetGenericArguments()[0];
      sb.Append(string.Format(Strings.PropertyPairFormat, Strings.Type, type.GetShortName()));
      if (IsNullable)
        sb.Append(Strings.NullableMark);
      sb.Append(Strings.Comma).Append(string.Format(
        Strings.PropertyPairFormat, Strings.Length, Length.HasValue ? Length.Value.ToString() : "null"));
      if (Culture!=null)
        sb.Append(Strings.Comma).Append(string.Format(
          Strings.PropertyPairFormat, Strings.Culture, Culture));
      if (Scale > 0)
        sb.Append(Strings.Comma).Append(string.Format(
          Strings.PropertyPairFormat, Strings.Scale, Scale));
      if (Precision > 0)
        sb.Append(Strings.Comma).Append(string.Format(
          Strings.PropertyPairFormat, Strings.Precision, Precision));
      return sb.ToString();
    }


    #region Constructors

    protected TypeInfo()
    {
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="type">Underlying data type.</param>
    public TypeInfo(Type type)
      : this(type, type.IsClass || type.IsNullable())
    {
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="type">Underlying data type.</param>
    /// <param name="length">The length.</param>
    public TypeInfo(Type type, int? length)
      : this(type, type.IsClass || type.IsNullable(), length)
    {
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="type">Underlying data type.</param>
    /// <param name="length">The length.</param>
    /// <param name="culture">The culture.</param>
    public TypeInfo(Type type, int? length, CultureInfo culture)
      : this(type, type.IsClass || type.IsNullable(), length, culture)
    {
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="type">Underlying data type.</param>
    /// <param name="length">The length.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="precision">The precision.</param>
    public TypeInfo(Type type, int? length, int? scale, int? precision)
      : this(type, type.IsClass || type.IsNullable(), length, scale, precision)
    {
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="type">Underlying data type.</param>
    /// <param name="isNullable">Indicates whether type is nullable.</param>
    public TypeInfo(Type type, bool isNullable)
    {
      ArgumentValidator.EnsureArgumentNotNull(type, "type");
      if (isNullable && type.IsValueType && !type.IsNullable())
        ArgumentValidator.EnsureArgumentIsInRange(true, false, false, "isNullable");
      Type = type;
      IsNullable = isNullable;
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="type">Underlying data type.</param>
    /// <param name="isNullable">Indicates whether type is nullable.</param>
    /// <param name="length">The length.</param>
    public TypeInfo(Type type, bool isNullable, int? length)
      : this(type, isNullable)
    {
      // ArgumentValidator.EnsureArgumentIsInRange(length, 0, int.MaxValue, "length");
      Length = length;
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="type">Underlying data type.</param>
    /// <param name="isNullable">Indicates whether type is nullable.</param>
    /// <param name="length">The length.</param>
    /// <param name="culture">The culture.</param>
    public TypeInfo(Type type, bool isNullable, int? length, CultureInfo culture)
      : this(type, isNullable, length)
    {
      ArgumentValidator.EnsureArgumentNotNull(culture, "culture");
      Culture = culture;
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="type">Underlying data type.</param>
    /// <param name="isNullable">Indicates whether type is nullable.</param>
    /// <param name="length">The length.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="precision">The precision.</param>
    public TypeInfo(Type type, bool isNullable, int? length, int? scale, int? precision)
      : this(type, isNullable, length)
    {
      Scale = scale;
      Precision = precision;
    }

    #endregion
  }
}
