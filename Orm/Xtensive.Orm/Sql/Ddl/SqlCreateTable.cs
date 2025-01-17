// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using Xtensive.Sql.Model;

namespace Xtensive.Sql.Ddl
{
  [Serializable]
  public class SqlCreateTable : SqlStatement, ISqlCompileUnit
  {
    private Table table;

    public Table Table {
      get {
        return table;
      }
    }

    internal override object Clone(SqlNodeCloneContext context) =>
      context.NodeMapping.TryGetValue(this, out var clone)
        ? clone
        : context.NodeMapping[this] = new SqlCreateTable(table);

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }

    internal SqlCreateTable(Table table) : base(SqlNodeType.Create)
    {
      this.table = table;
    }
  }
}
