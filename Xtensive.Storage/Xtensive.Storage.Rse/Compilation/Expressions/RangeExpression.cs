// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.12.02

using System;

namespace Xtensive.Storage.Rse.Compilation.Expressions
{
  [Serializable]
  public sealed class RangeExpression : RseExpression
  {
    public RangeExpression(Type type)
      : base(RseExpressionType.Range, type)
    {
    }
  }
}