// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.12.02

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Xtensive.Storage.Model;

namespace Xtensive.Storage.Rse.Compilation.Expressions
{
  [Serializable]
  public sealed class IndexAccessExpression : RseExpression
  {
    public IndexInfo Index { get; private set; }

    public override string ToString()
    {
      return string.Format("Index[{0}]", Index.Name);
    }

    public IndexAccessExpression(Type type, IndexInfo index)
      : base(RseExpressionType.IndexAccess, type)
    {
      Index = index;
    }
  }
}