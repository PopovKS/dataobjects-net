// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.08.21

using Xtensive.Core.Tuples;

namespace Xtensive.Storage.Providers.Sql
{
  internal sealed class SqlPersistTask : SqlTask
  {
    public SqlPersistRequest Request;
    public Tuple Tuple;

    // Constructors

    public SqlPersistTask(SqlPersistRequest request, Tuple tuple)
    {
      Request = request;
      Tuple = tuple;
    }
  }
}