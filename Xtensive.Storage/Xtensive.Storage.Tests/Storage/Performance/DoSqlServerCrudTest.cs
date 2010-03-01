// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2008.09.08

using NUnit.Framework;
using Xtensive.Storage.Configuration;

namespace Xtensive.Storage.Tests.Storage.Performance
{
  [TestFixture]
  [Explicit]
  public class DoSqlServerCrudTest : DoCrudTest
  {
    protected override DomainConfiguration CreateConfiguration()
    {
      return DomainConfigurationFactory.CreateForCrudTest("mssql2005");
    }
  }
}
