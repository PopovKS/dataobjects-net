﻿// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.03.17

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Core.Linq.Normalization;
using Xtensive.Storage.Model;

namespace Xtensive.Storage.Rse.Optimization.IndexSelection
{
  internal sealed class RangeSetExtractor
  {
    private readonly CnfParser cnfParser;
    private readonly GeneralPredicateParser generalParser;

    public Dictionary<Expression, List<RsExtractionResult>> Extract(DisjunctiveNormalized predicate,
      IEnumerable<IndexInfo> indexInfo, RecordSetHeader primaryIdxRecordSetHeader)
    {
      ArgumentValidator.EnsureArgumentNotNull(predicate, "predicate");
      ArgumentValidator.EnsureArgumentNotNull(indexInfo, "indexInfo");
      ArgumentValidator.EnsureArgumentNotNull(primaryIdxRecordSetHeader, "primaryIdxRecordSetHeader");
      predicate.Validate();
      var result = new Dictionary<Expression, List<RsExtractionResult>>();
      var indexCount = indexInfo.Count();
      foreach (var operand in predicate.Operands) {
        var expressionPart = operand.ToExpression();
        var rangeSets = ProcessExpressionPart(operand, indexInfo, indexCount, primaryIdxRecordSetHeader);
        result.Add(expressionPart, rangeSets);
      }
      return result;
    }

    public Dictionary<Expression, List<RsExtractionResult>> Extract(Expression predicate,
      IEnumerable<IndexInfo> indexInfo, RecordSetHeader primaryIdxRecordSetHeader)
    {
      ArgumentValidator.EnsureArgumentNotNull(predicate, "predicate");
      ArgumentValidator.EnsureArgumentNotNull(indexInfo, "indexInfo");
      ArgumentValidator.EnsureArgumentNotNull(primaryIdxRecordSetHeader, "primaryIdxRecordSetHeader");
      var result = new Dictionary<Expression, List<RsExtractionResult>>();
      var indexCount = indexInfo.Count();
      var extractionResult = ProcessExpression(predicate, indexInfo, indexCount, primaryIdxRecordSetHeader);
      result.Add(predicate, extractionResult);
      return result;
    }

    private List<RsExtractionResult> ProcessExpressionPart(Conjunction<Expression> part,
      IEnumerable<IndexInfo> indexInfo, int indexCount, RecordSetHeader rsHeader)
    {
      var result = new List<RsExtractionResult>(indexCount);
      foreach (var info in indexInfo) {
        var resultPart = cnfParser.Parse(part, info, rsHeader);
        var extractionResult = new RsExtractionResult(info, resultPart);
        result.Add(extractionResult);
      }
      return result;
    }

    private List<RsExtractionResult> ProcessExpression(Expression exp,
      IEnumerable<IndexInfo> indexInfo, int indexCount, RecordSetHeader rsHeader)
    {
      var result = new List<RsExtractionResult>(indexCount);
      foreach (var info in indexInfo) {
        var resultPart = generalParser.Parse(exp, info, rsHeader);
        var extractionResult = new RsExtractionResult(info, resultPart);
        result.Add(extractionResult);
      }
      return result;
    }

    // Constructors

    public RangeSetExtractor(DomainModel domainModel)
    {
      cnfParser = new CnfParser(domainModel);
      generalParser = new GeneralPredicateParser(domainModel);
    }
  }
}