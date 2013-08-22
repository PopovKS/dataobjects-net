﻿// Copyright (C) 2013 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2013.08.19

using System;
using Mono.Cecil;

namespace Xtensive.Orm.Weaver.Tasks
{
  internal sealed class ImplementInitializablePatternTask : WeavingTask
  {
    private readonly TypeDefinition type;
    private readonly MethodDefinition constructor;

    public override ActionResult Execute(ProcessorContext context)
    {
      return ActionResult.Success;
    }

    public ImplementInitializablePatternTask(TypeDefinition type, MethodDefinition constructor)
    {
      if (type==null)
        throw new ArgumentNullException("type");
      if (constructor==null)
        throw new ArgumentNullException("constructor");

      this.type = type;
      this.constructor = constructor;
    }
  }
}