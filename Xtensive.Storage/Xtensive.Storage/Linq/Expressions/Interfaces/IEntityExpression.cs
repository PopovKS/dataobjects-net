// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.05.14

using System;
namespace Xtensive.Storage.Linq.Expressions
{
  internal interface IEntityExpression : IPersistentExpression
  {
    KeyExpression Key { get; }
    Type Type { get;}
  }
}