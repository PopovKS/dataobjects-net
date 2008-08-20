// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Aleksey Gamzov
// Created:    2008.08.14

using System;
using System.Collections.Generic;

namespace Xtensive.Sql.Dom.Database.Comparer
{
  [Serializable]
  internal class IndexSqlComparer : WrappingSqlComparer<Index, IndexColumn, DataTableColumn>
  {
    public IndexSqlComparer(ISqlComparerProvider provider)
      : base(provider)
    {
    }
  }
}