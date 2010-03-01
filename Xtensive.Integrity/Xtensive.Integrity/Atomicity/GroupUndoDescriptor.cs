// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2007.12.27

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Integrity.Resources;

namespace Xtensive.Integrity.Atomicity
{
  /// <summary>
  /// Default implementation for <see cref="IGroupUndoDescriptor"/>.
  /// Describes group undo operation - an operation involving
  /// undoing a set of nested undo operations.
  /// </summary>
  [Serializable]
  public class GroupUndoDescriptor: UndoDescriptor, 
    IGroupUndoDescriptor
  {
    private static readonly MethodInfo undoMethodInfo;
    private IList<IUndoDescriptor> operations = new List<IUndoDescriptor>();

    public IList<IUndoDescriptor> Operations {
      [DebuggerStepThrough]
      get { return operations; }
    }

    private static void Undo(UndoDescriptor undoDescriptor)
    {
      IList<IUndoDescriptor> descriptors = ((GroupUndoDescriptor)undoDescriptor).Operations;
      if (descriptors==null)
        return;
      for (int i = descriptors.Count-1; i>=0; i--) {
        try {
          descriptors[i].Invoke();
        }
        catch (Exception e) {
          Log.Error(e, Strings.LogUndoError, descriptors[i]);
        }
      }
    }


    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    public GroupUndoDescriptor() 
    {
      CallDescriptor = new MethodCallDescriptor(null, undoMethodInfo);
    }
    
    static GroupUndoDescriptor()
    {
      undoMethodInfo = typeof (GroupUndoDescriptor).GetMethod("Undo", BindingFlags.Static | BindingFlags.NonPublic);
    }
  }
}