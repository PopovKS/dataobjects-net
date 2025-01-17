// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.08.10

using System;
using Xtensive.Sql.Model;

namespace Xtensive.Sql.Ddl
{
  [Serializable]
  public class SqlRenameColumn : SqlAction
  {
    public TableColumn Column { get; private set; }
    public string NewName { get; private set; }

    internal override object Clone(SqlNodeCloneContext context) =>
      context.NodeMapping.TryGetValue(this, out var clone)
        ? clone
        : context.NodeMapping[this] = new SqlRenameColumn(Column, NewName);
      

    // Constructors

    internal SqlRenameColumn(TableColumn column, string newName)
    {
      Column = column;
      NewName = newName;
    }
  }
}