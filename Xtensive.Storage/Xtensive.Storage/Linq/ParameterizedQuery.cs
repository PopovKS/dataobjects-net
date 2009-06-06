// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.05.27

using System;
using System.Collections.Generic;
using Xtensive.Core.Parameters;
using Xtensive.Core.Tuples;

namespace Xtensive.Storage.Linq
{
  [Serializable]
  internal sealed class ParameterizedQuery<TResult> : TranslatedQuery<TResult>
  {
    public readonly Parameter QueryParameter;

    public ParameterizedQuery(TranslatedQuery<TResult> translatedQuery, Parameter parameter, IDictionary<Parameter<Tuple>, Tuple> tupleParameterBindings)
      : base(translatedQuery.DataSource, translatedQuery.Materializer, tupleParameterBindings)
    {
      QueryParameter = parameter;
    }
  }
}