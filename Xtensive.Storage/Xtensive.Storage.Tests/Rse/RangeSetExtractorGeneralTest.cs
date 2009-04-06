// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.04.03

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NUnit.Framework;
using Xtensive.Core;
using Xtensive.Core.Collections;
using Xtensive.Core.Tuples;
using Xtensive.Indexing;
using Xtensive.Storage.Model;
using Xtensive.Storage.Rse;
using Xtensive.Storage.Rse.Optimization.IndexSelection;
using Xtensive.Storage.Tests.Storage.SnakesModel;

namespace Xtensive.Storage.Tests.Rse
{
  public class RangeSetExtractorGeneralTest : BaseRangeSetExtractorTest
  {
    [Test]
    public void SimpleTest()
    {
      TypeInfo snakeType = Domain.Model.Types[typeof(ClearSnake)];
      IndexInfo indexInfo = snakeType.Indexes.GetIndex(LengthField);
      RecordSetHeader rsHeader = snakeType.Indexes.PrimaryIndex.GetRecordSetHeader();
      int lengthIdx = GetFieldIndex(rsHeader, LengthField);
      int nameIdx = GetFieldIndex(rsHeader, NameField);

      Expression<Func<Tuple, bool>> predicate =
         (t) => t.GetValue<string>(nameIdx).CompareTo("abc") >= 0 
           && (t.GetValue<int>(lengthIdx).CompareTo(1) < 0 || t.GetValue<int?>(lengthIdx) >= 3)
           || !(10 >= t.GetValue<int?>(lengthIdx)) && !(new bool()) || lengthIdx is string;

      var expectedRanges = CreateRangesForComplexTest(indexInfo, LengthField);
      TestExpression(predicate, indexInfo, rsHeader, expectedRanges);
    }

    private static IEnumerable<Range<Entire<Tuple>>> CreateRangesForComplexTest(IndexInfo indexInfo,
      string keyFieldName)
    {
      var keyFieldIndex = indexInfo.GetRecordSetHeader().IndexOf(keyFieldName);
      var result = new SetSlim<Range<Entire<Tuple>>>();
      Entire<Tuple> expectedFirst = new Entire<Tuple>(InfinityType.Negative);
      Entire<Tuple> expectedSecond = new Entire<Tuple>(
        CreateTuple(indexInfo.KeyTupleDescriptor, keyFieldIndex, 1), Direction.Negative);
      var expectedRange0 = new Range<Entire<Tuple>>(expectedFirst, expectedSecond);
      result.Add(expectedRange0);

      expectedFirst = new Entire<Tuple>(CreateTuple(indexInfo.KeyTupleDescriptor, keyFieldIndex, 3));
      expectedSecond = new Entire<Tuple>(InfinityType.Positive);
      var expectedRange1 = new Range<Entire<Tuple>>(expectedFirst, expectedSecond);
      result.Add(expectedRange1);
      return result;
    }

    [Test]
    public void DifferentFieldInversionTest()
    {
      TypeInfo snakeType = Domain.Model.Types[typeof(ClearSnake)];
      IndexInfo indexInfo = snakeType.Indexes.GetIndex(LengthField);
      RecordSetHeader rsHeader = snakeType.Indexes.PrimaryIndex.GetRecordSetHeader();
      int lengthIdx = GetFieldIndex(rsHeader, LengthField);
      int nameIdx = GetFieldIndex(rsHeader, NameField);

      Expression<Func<Tuple, bool>> predicate0 =
        (t) => !(t.GetValue<string>(nameIdx).CompareTo("abc") < 0 || t.GetValue<int?>(lengthIdx) >= 3
            || !(true && t.GetValue<int?>(lengthIdx) >= 1))
          || !(lengthIdx is int && !(lengthIdx is string));

      Expression<Func<Tuple, bool>> predicate1 =
        (t) => t.GetValue<string>(nameIdx).CompareTo("abc") >= 0 && t.GetValue<int?>(lengthIdx) < 3 
          || !(lengthIdx is int && !(lengthIdx is string));
      var rsExtractor = new RangeSetExtractor(Domain.Model);
      var result0 = rsExtractor.Extract(predicate0, new[] {indexInfo}, rsHeader);
      var result1 = rsExtractor.Extract(predicate1, new[] { indexInfo }, rsHeader);
      var expectedRanges = CreateRangesForDifferentFieldInversionTest(indexInfo, LengthField);
      var rs0 = result0.GetRangeSetForSingleIndex();
      CheckRanges(expectedRanges, rs0);
      var rs1 = result1.GetRangeSetForSingleIndex();
      CheckRanges(rs0, rs1);
    }

    private static IEnumerable<Range<Entire<Tuple>>> CreateRangesForDifferentFieldInversionTest(
      IndexInfo indexInfo, string keyFieldName)
    {
      var keyFieldIndex = indexInfo.GetRecordSetHeader().IndexOf(keyFieldName);
      var result = new SetSlim<Range<Entire<Tuple>>>();
      Entire<Tuple> expectedFirst = new Entire<Tuple>(InfinityType.Negative);
      Entire<Tuple> expectedSecond = new Entire<Tuple>(
        CreateTuple(indexInfo.KeyTupleDescriptor, keyFieldIndex, 3), Direction.Negative);
      var expectedRange0 = new Range<Entire<Tuple>>(expectedFirst, expectedSecond);
      result.Add(expectedRange0);
      return result;
    }
  }
}