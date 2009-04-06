// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.04.03

using System.Collections.Generic;
using System.Linq.Expressions;
using Xtensive.Storage.Model;

namespace Xtensive.Storage.Rse.Optimization.IndexSelection
{
  internal interface IIndexesSelector
  {
    Dictionary<IndexInfo, RangeSetInfo> Select(Dictionary<Expression,
      List<RsExtractionResult>> extractionResults);
  }
}