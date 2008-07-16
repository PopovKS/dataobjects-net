// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.05.20

using System.Collections.Generic;
using Xtensive.Core.Tuples;
using Xtensive.Storage.Rse;

namespace Xtensive.Storage.Rse.Providers.Executable
{
  public sealed class AliasProvider : TransparentProvider
  {
    

    // Constructor

    public AliasProvider(CompilableProvider origin, Provider source)
      : base(origin, source)
    {
    }
  }
}