﻿// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.03.16

// TODO: Refactor stupid MSSqlExtractorTests.cs and put all stuff here

using System;
using System.Data;
using NUnit.Framework;
using Xtensive.Sql.Common;
using Xtensive.Sql.Common.Mssql;
using Xtensive.Sql.Dom.Database;
using Xtensive.Sql.Dom.Database.Providers;
using Xtensive.Sql.Dom.Mssql.v2005;

namespace Xtensive.Sql.Dom.Tests.MsSql
{
  [TestFixture]
  public class ExtractorTest
  {
    private SqlConnection connection;
    private SqlDriver driver;

    private void ExecuteCommand(string commandText)
    {
      using (var command = connection.CreateCommand()) {
        command.CommandText = commandText;
        command.ExecuteNonQuery();
      }
    }

    private Model ExtractModel()
    {
      var provider = new SqlModelProvider(connection);
      return Model.Build(provider);
    }

    [TestFixtureSetUp]
    public void TestFixtureSetUp()
    {
      var connectionInfo = new ConnectionInfo(TestUrl.MsSql2005);
      driver = new MssqlDriver(connectionInfo);
      connection = (SqlConnection)driver.CreateConnection(connectionInfo);
      connection.Open();
    }

    [TestFixtureTearDown]
    public void TestFixtureTearDown()
    {
      if (connection != null && connection.State == ConnectionState.Open)
        connection.Close();
    }

    [Test]
    public void ExtractDomainsTest()
    {
      string createDomain =
        "create type test_type from bigint";
      string createTable =
        "create table table_with_domained_columns (id int primary key, value test_type)";
      string dropDomain =
        "if type_id('test_type') is not null drop type test_type";
      string dropTable =
        "if object_id('table_with_domained_columns') is not null drop table table_with_domained_columns";

      ExecuteCommand(dropTable);
      ExecuteCommand(dropDomain);
      ExecuteCommand(createDomain);
      ExecuteCommand(createTable);

      var schema = ExtractModel().DefaultServer.DefaultCatalog.DefaultSchema;
      var domains = schema.Domains;
      Assert.AreEqual(1, domains.Count);
      Assert.AreEqual("test_type", domains[0].Name);
      Assert.AreEqual(driver.ServerInfoProvider.DataTypesInfo["bigint"].SqlType, domains[0].DataType.DataType);

      var domain = schema.Tables["table_with_domained_columns"].TableColumns["value"].Domain;
      Assert.IsNotNull(domain);
      Assert.AreEqual("test_type", domain.Name);
    }
  }
}
