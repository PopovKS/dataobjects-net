// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  /// <summary>
  /// Represents literal value expression.
  /// </summary>
  [Serializable]
  public class SqlLiteral<T> : SqlLiteral
  {
    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <value>The value.</value>
    public T Value { get; private set; }

    public override Type LiteralType { get { return typeof (T); } }

    public override object GetValue()
    {
      return Value;
    }

    public override void ReplaceWith(SqlExpression expression)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      ArgumentValidator.EnsureArgumentIs<SqlLiteral<T>>(expression, "expression");
      var replacingExpression = (SqlLiteral<T>) expression;
      Value = replacingExpression.Value;
    }

    internal override object Clone(SqlNodeCloneContext context) =>
      context.NodeMapping.TryGetValue(this, out var clone)
        ? clone
        : context.NodeMapping[this] = new SqlLiteral<T>(Value);

    // Constructor

    internal SqlLiteral(T value)
    {
      Value = value;
    }
  }
}
