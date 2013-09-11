﻿// Copyright (C) 2013 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kulakov
// Created:    2013.08.16

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Providers;
using Xtensive.Sql;
using Xtensive.Sql.Compiler;
using Xtensive.Sql.Ddl;
using Xtensive.Sql.Dml;
using Xtensive.Sql.Model;
using Xtensive.Sql.Tests;
using Model1 = Xtensive.Orm.Tests.Storage.IgnoreRulesValidateModel1;
using Model2 = Xtensive.Orm.Tests.Storage.IgnoreRulesValidateModel2;
using Model3 = Xtensive.Orm.Tests.Storage.IgnoreRulesValidateModel3;
using Model4 = Xtensive.Orm.Tests.Storage.IgnoreRulesValidateModel4;

namespace Xtensive.Orm.Tests.Storage.IgnoreRulesValidateModel1
{
  public abstract class Person : Entity
  {
    [Field]
    public string FirstName { get; set; }

    [Field]
    public string LastName { get; set; }

    [Field]
    public DateTime Birthday { get; set; }
  }

  [HierarchyRoot]
  public class Customer : Person
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public EntitySet<Order> Orders { get; set; }

  }

  [HierarchyRoot]
  public class Author : Person
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public EntitySet<Book> Books { get; set; }

    [Field]
    public IgnoredTable IgnoredColumn { get; set; }
  }
  
  [HierarchyRoot]
  public class Book : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public string Title { get; set; }

    [Field(Length = 13)]
    public string ISBN { get; set; }

    [Field]
    [Association(PairTo = "Books")]
    public EntitySet<Author> Authors { get; set; }

    [Field]
    public EntitySet<Order> Orders { get; set; }
  }

  [HierarchyRoot]
  [Index("SomeIgnoredField", "Book", "Customer")]
  public class Order : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public string SomeIgnoredField { get; set; }

    [Field]
    [Association(PairTo = "Orders")]
    public Book Book { get; set; }

    [Field]
    [Association(PairTo = "Orders")]
    public Customer Customer { get; set; }
  }

  [HierarchyRoot]
  public class IgnoredTable : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public string SomeField { get; set; }

    [Field]
    public Book Book { get; set; }

    [Field]
    public Customer Customer { get; set; }

    [Field]
    public Order Order { get; set; }

    [Field]
    [Association(PairTo = "IgnoredColumn")]
    public Author Author { get; set; }
  }
}

namespace Xtensive.Orm.Tests.Storage.IgnoreRulesValidateModel2
{
  public abstract class Person : Entity
  {
    [Field]
    public string FirstName { get; set; }

    [Field]
    public string LastName { get; set; }

    [Field]
    public DateTime Birthday { get; set; }
  }

  [HierarchyRoot]
  public class Customer : Person
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public EntitySet<Order> Orders { get; set; }

  }

  [HierarchyRoot]
  public class Author : Person
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public EntitySet<Book> Books { get; set; }
  }

  [HierarchyRoot]
  public class Book : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public string Title { get; set; }

    [Field(Length = 13)]
    public string ISBN { get; set; }

    [Field]
    [Association(PairTo = "Books")]
    public EntitySet<Author> Authors { get; set; }

    [Field]
    public EntitySet<Order> Orders { get; set; }
  }

  [HierarchyRoot]
  public class Order : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    [Association(PairTo = "Orders")]
    public Book Book { get; set; }

    [Field]
    [Association(PairTo = "Orders")]
    public Customer Customer { get; set; }
  }
}

namespace Xtensive.Orm.Tests.Storage.IgnoreRulesValidateModel3
{
  [HierarchyRoot]
  public class MyEntity1 : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public string FirstColumn { get; set; }
  }
  [HierarchyRoot]
  public class MyEntity2 : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public string FirstColumn { get; set; }
  }
}

namespace Xtensive.Orm.Tests.Storage.IgnoreRulesValidateModel4
{
  [HierarchyRoot]
  public class MyEntity1 : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public string FirstColumn { get; set; }
  }
}

namespace Xtensive.Orm.Tests.Storage
{
  [TestFixture]
  public class IgnoreRulesValidateTest
  {
    private SqlDriver sqlDriver;
    private Key changedOrderKey;
    private bool isMultidatabaseTest;
    private bool isMultischemaTest;

    [TestFixtureSetUp]
    public void Setup()
    {
      sqlDriver = TestSqlDriver.Create(GetConnectionInfo());
    }

    [Test]
    public void PerformUpdateTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      BuildDomainAndFillData();
      UpgradeDomain(DomainUpgradeMode.Perform);
      BuildDomainInValidateMode();
    }
    
    [Test]
    public void PerformSafelyUpdateTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      BuildDomainAndFillData();
      UpgradeDomain(DomainUpgradeMode.PerformSafely);
      BuildDomainInValidateMode();
    }

    [Test]
    public void IgnoreSimpleColumnTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initialDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initialDomain.Dispose();
      Catalog catalog = GetCatalog();
      CreateColumn(catalog, "dbo", "MyEntity2", "SimpleIgnoredColumn", SqlType.VarChar);
      IgnoreRuleCollection ignoreRuleCollection = new IgnoreRuleCollection();
      ignoreRuleCollection.IgnoreColumn("SimpleIgnoredColumn").WhenTable("MyEntity2");
      var validatedDomain = BuildDomain(DomainUpgradeMode.Validate, typeof (Model3.MyEntity1), ignoreRuleCollection);
      validatedDomain.Dispose();
    }

    [Test]
    public void IgnoreReferencedColumnTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initialDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initialDomain.Dispose();
      Catalog catalog = GetCatalog();
      CreateColumn(catalog, "dbo", "MyEntity2", "ReferencedIgnoredColumn", SqlType.Int64);
      CreateForeignKey(catalog, "dbo", "MyEntity2", "ReferencedIgnoredColumn", "MyEntity1", "Id", "FK_MyEntity2_MyEntity1_MyEntity1ID");
      var ignoreRuleCollection = new IgnoreRuleCollection();
      ignoreRuleCollection.IgnoreColumn("ReferencedIgnoredColumn").WhenTable("MyEntity2").WhenSchema("dbo");
      var validatedDomain = BuildDomain(DomainUpgradeMode.Validate, typeof (Model3.MyEntity1), ignoreRuleCollection);
      validatedDomain.Dispose();
    }

    [Test]
    public void IgnoreSimpleTableTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initDomain.Dispose();

      Catalog catalog = GetCatalog();
      var addedColumnsNames = new[] {"Id", "FirstColumn"};
      var addedColumnsTypes = new[] {SqlType.Int32, SqlType.VarChar};
      CreateTable(catalog, "dbo", "MyIgnoredEntity", addedColumnsNames, addedColumnsTypes);
      CreatePrimaryKey(catalog, "dbo", "MyIgnoredEntity", "Id", "PK_MyIgnoredEntity_Id");
      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreTable("MyIgnoredEntity");
      var validateDomain = BuildDomain(DomainUpgradeMode.Validate, typeof (Model3.MyEntity1), ignoreRules);
      validateDomain.Dispose();
    }

    [Test]
    public void IgnoreReferencedTableTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initDomain.Dispose();

      Catalog catalog = GetCatalog();
      var addedColumnsNames = new[] {"Id", "FirstColumn", "MyEntity2Id"};
      var addedColumnsTypes = new[] {SqlType.Int32, SqlType.VarChar, SqlType.Int64};
      CreateTable(catalog, "dbo", "MyIgnoredEntity", addedColumnsNames, addedColumnsTypes);
      CreatePrimaryKey(catalog, "dbo", "MyIgnoredEntity", "Id", "PK_MyIgnoredEntity_Id");
      CreateForeignKey(catalog, "dbo", "MyIgnoredEntity", "MyEntity2Id", "MyEntity2", "Id", "FK_MyIgnoredEntity_MyEntity2_Id");
      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreTable("MyIgnoredEntity");
      var validateDomain = BuildDomain(DomainUpgradeMode.Validate, typeof (Model3.MyEntity1), ignoreRules);
      validateDomain.Dispose();
    }

    [Test]
    public void InsertIntoTableWithIgnoredColumnTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initialDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initialDomain.Dispose();

      Catalog catalog = GetCatalog();
      CreateColumn(catalog, "dbo", "Myentity2", "SimpleIgnoredColumn", SqlType.VarChar);
      IgnoreRuleCollection ignoreRuleCollection = new IgnoreRuleCollection();
      ignoreRuleCollection.IgnoreColumn("SimpleIgnoredColumn").WhenTable("MyEntity2");
      using (var validatedDomain = BuildDomain(DomainUpgradeMode.Validate, typeof (Model3.MyEntity1), ignoreRuleCollection))
      using (var session = validatedDomain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        new Model3.MyEntity2() { FirstColumn = "some test" };
        transaction.Complete();
      }
      ValidateMyEntity(catalog, "dbo");
    }

    [Test]
    [ExpectedException(typeof(CheckConstraintViolationException))]
    public void InsertIntoTableWithNotNullableIgnoredColumnTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initialDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initialDomain.Dispose();

      Catalog catalog = GetCatalog();
      catalog.Schemas["dbo"].Tables["MyEntity2"].CreateColumn("SimpleIgnoredColumn", new SqlValueType(SqlType.VarChar, 30)).IsNullable = false;
      SqlAlterTable alter = SqlDdl.Alter(catalog.Schemas["dbo"].Tables["MyEntity2"],
        SqlDdl.AddColumn(catalog.Schemas["dbo"].Tables["MyEntity2"].TableColumns["SimpleIgnoredColumn"]));
      var commandText = sqlDriver.Compile(alter).GetCommandText();
      ExecuteNonQuery(commandText);
      IgnoreRuleCollection ignoreRuleCollection = new IgnoreRuleCollection();
      ignoreRuleCollection.IgnoreColumn("SimpleIgnoredColumn").WhenTable("MyEntity2");
      using (var validatedDomain = BuildDomain(DomainUpgradeMode.Validate, typeof (Model3.MyEntity1), ignoreRuleCollection))
      using (var session = validatedDomain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        new Model3.MyEntity2 { FirstColumn = "some test" };
        transaction.Complete();
      }
    }

    [Test]
    [ExpectedException(typeof(SchemaSynchronizationException))]
    public void DropTableWithIgnoredColumnTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initialDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initialDomain.Dispose();
      Catalog catalog = GetCatalog();
      CreateColumn(catalog, "dbo", "MyEntity2", "SimpleIgnoredColumn", SqlType.VarChar);
      IgnoreRuleCollection ignoreRuleCollection = new IgnoreRuleCollection();
      ignoreRuleCollection.IgnoreColumn("SimpleIgnoredColumn").WhenTable("MyEntity2");
      var performDomain = BuildDomain(DomainUpgradeMode.Perform, typeof (Model4.MyEntity1), ignoreRuleCollection);
      performDomain.Dispose();
    }

    [Test]
    [ExpectedException(typeof(SchemaSynchronizationException))]
    public void DropReferencedTableTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initialDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initialDomain.Dispose();
      Catalog catalog = GetCatalog();
      CreateColumn(catalog, "dbo", "MyEntity2", "ReferencedIgnoredColumn", SqlType.Int64);
      CreateForeignKey(catalog, "dbo", "MyEntity2", "ReferencedIgnoredColumn", "MyEntity1", "Id", "FK_MyEntity2_MyEntity1_MyEntity1ID");
      var ignoreRuleCollection = new IgnoreRuleCollection();
      ignoreRuleCollection.IgnoreColumn("ReferencedIgnoredColumn").WhenTable("MyEntity2").WhenSchema("dbo");
      var performDomain = BuildDomain(DomainUpgradeMode.Perform, typeof(Model4.MyEntity1), ignoreRuleCollection);
      performDomain.Dispose();
    }

    [Test]
    public void IgnoreColumnsByMaskValidateTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initDomain.Dispose();
      Catalog catalog = GetCatalog();
      CreateColumn(catalog, "dbo", "MyEntity2", "IgnoreFirstColumn", SqlType.VarChar);
      CreateColumn(catalog, "dbo", "MyEntity2", "IgnoreSecondColumn", SqlType.VarChar);
      CreateColumn(catalog, "dbo", "MyEntity2", "IgnoreThirdColumn", SqlType.VarChar);
      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("Ignore*");
      var validateDomain = BuildDomain(DomainUpgradeMode.Validate, typeof (Model3.MyEntity1), ignoreRules);
      validateDomain.Dispose();
    }

    [Test]
    public void IgnoreTablesByMaskValidateTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initDomain.Dispose();
      Catalog catalog = GetCatalog();
      var addedColumnsNames = new[] {"Id", "FirstColumn", "SecondColumn"};
      var addedColumnsTypes = new[] {SqlType.Int64, SqlType.VarChar, SqlType.VarChar};
      CreateTable(catalog, "dbo", "IgnoredFirstTable", addedColumnsNames, addedColumnsTypes);
      CreatePrimaryKey(catalog, "dbo", "IgnoredFirstTable", "Id", "PK_IgnoredFirstTable_Id");
      CreateTable(catalog, "dbo", "IgnoredSecondTable", addedColumnsNames, addedColumnsTypes);
      CreatePrimaryKey(catalog, "dbo", "IgnoredSecondTable", "Id", "PK_IgnoredSecondTable_Id");
      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreTable("Ignored*");
      var validateDomain = BuildDomain(DomainUpgradeMode.Validate, typeof (Model3.MyEntity1), ignoreRules);
      validateDomain.Dispose();
    }

    [Test]
    public void IgnoreAllColumnsInTableByMaskValidateTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initDomain.Dispose();
      Catalog catalog = GetCatalog();
      var addedColumnsNames = new[] {"Id", "FirstColumn", "SecondColumn"};
      var addedColumnsTypes = new[] {SqlType.Int64, SqlType.VarChar, SqlType.VarChar};
      CreateTable(catalog, "dbo", "IgnoredTable", addedColumnsNames, addedColumnsTypes);
      CreatePrimaryKey(catalog, "dbo", "IgnoredTable", "Id", "PK_IgnoredTable_Id");
      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("*").WhenTable("IgnoredTable");
      var validateDomain = BuildDomain(DomainUpgradeMode.Validate, typeof (Model3.MyEntity1), ignoreRules);
    }

    [Test]
    public void UpgradeDomainWithIgnoreRuleByMaskInPerformModeTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initDomain.Dispose();
      Catalog catalog = GetCatalog();
      CreateColumn(catalog, "dbo", "MyEntity2", "IgnoredFirstColumn", SqlType.VarChar);
      CreateColumn(catalog, "dbo", "MyEntity2", "IgnoredSecondColumn", SqlType.VarChar);
      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("Ignored*").WhenTable("MyEntity2");
      using (var domain = BuildDomain(DomainUpgradeMode.Perform, typeof (Model3.MyEntity1), ignoreRules))
      using (var session = domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        new Model3.MyEntity1 {FirstColumn = "Some text"};
        new Model3.MyEntity2 {FirstColumn = "Second some test"};
        transaction.Complete();
      }
      SqlTableRef myEntity2 = SqlDml.TableRef(catalog.Schemas["dbo"].Tables["MyEntity2"]);
      SqlSelect select = SqlDml.Select(myEntity2);
      var compileConfiguration = new SqlCompilerConfiguration();
      compileConfiguration.DatabaseQualifiedObjects = false;
      var commandText = sqlDriver.Compile(select, compileConfiguration).GetCommandText();
      Assert.That(ExecuteQuery(commandText, 2), Is.EqualTo(DBNull.Value));
      Assert.That(ExecuteQuery(commandText, 3), Is.EqualTo(DBNull.Value));
    }

    [Test]
    public void UpgradeDomainWithIgnoreRuleByMaskInPerformSafelyModeTest()
    {
      ClearMultidatabaseAndMultischemaFlags();
      var initDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model3.MyEntity1));
      initDomain.Dispose();
      Catalog catalog = GetCatalog();
      CreateColumn(catalog, "dbo", "MyEntity2", "IgnoredFirstColumn", SqlType.VarChar);
      CreateColumn(catalog, "dbo", "MyEntity2", "IgnoredSecondColumn", SqlType.VarChar);
      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("Ignored*");
      using (var domain = BuildDomain(DomainUpgradeMode.PerformSafely, typeof (Model3.MyEntity1), ignoreRules))
      using (var session = domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        new Model3.MyEntity1 { FirstColumn = "Some text" };
        new Model3.MyEntity2 { FirstColumn = "Second some test" };
        transaction.Complete();
      }
      SqlTableRef myEntity2 = SqlDml.TableRef(catalog.Schemas["dbo"].Tables["MyEntity2"]);
      SqlSelect select = SqlDml.Select(myEntity2);
      var compileConfiguration = new SqlCompilerConfiguration();
      compileConfiguration.DatabaseQualifiedObjects = false;
      var commandText = sqlDriver.Compile(select, compileConfiguration).GetCommandText();
      Assert.That(ExecuteQuery(commandText, 2), Is.EqualTo(DBNull.Value));
      Assert.That(ExecuteQuery(commandText, 3), Is.EqualTo(DBNull.Value));
    }

    [Test]
    public void MultischemaValidateTest()
    {
      SetMultischema();
      Require.AllFeaturesSupported(ProviderFeatures.Multischema);
      var catalog = GetCatalog();
      CreateSchema(catalog, "Model1");
      CreateSchema(catalog, "Model2");
      var initDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model1.Customer), typeof (Model3.MyEntity1));
      initDomain.Dispose();
      catalog = GetCatalog();
      CreateColumn(catalog, "Model2", "MyEntity2", "ReferencedIgnoredColumn", SqlType.Int64);
      CreateForeignKey(catalog, "Model2", "MyEntity2", "ReferencedIgnoredColumn", "MyEntity1", "Id", "FK_MyEntity2_MyEntity1_MyEntity1ID");
      var addedColumnsNames = new[] {"Id", "FirstColumn", "MyEntity2Id"};
      var addedColumnsTypes = new[] {SqlType.Int32, SqlType.VarChar, SqlType.Int64};
      CreateTable(catalog, "Model2", "MyIgnoredEntity", addedColumnsNames, addedColumnsTypes);
      CreatePrimaryKey(catalog, "Model2", "MyIgnoredEntity", "Id", "PK_MyIgnoreTable_Id");
      CreateForeignKey(catalog, "Model2", "MyIgnoredEntity", "MyEntity2Id", "MyEntity2", "Id", "FK_MyIgnoredEntity_MyEntity2_Id");

      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("ReferencedIgnoredColumn").WhenSchema("Model2");
      ignoreRules.IgnoreTable("MyIgnoredEntity").WhenSchema("Model2");
      ignoreRules.IgnoreTable("IgnoredTable").WhenSchema("Model1");
      ignoreRules.IgnoreColumn("SomeIgnoredField").WhenTable("Order").WhenSchema("Model1");
      ignoreRules.IgnoreColumn("IgnoredColumn.Id").WhenTable("Author").WhenSchema("Model1");

      var validateDomain = BuildDomain(DomainUpgradeMode.Validate, typeof (Model2.Customer), typeof (Model3.MyEntity1), ignoreRules);
      validateDomain.Dispose();
    }

    [Test]
    public void MultidatabaseValidateTest()
    {
      SetMultidatabase();
      Require.AllFeaturesSupported(ProviderFeatures.Multidatabase);
      isMultidatabaseTest = true;
      var initDomain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model1.Customer), typeof (Model3.MyEntity1));
      initDomain.Dispose();
      var secondCatalog = GetCatalog(Multimapping.MultidatabaseTest.Database2Name);
      CreateColumn(secondCatalog, "dbo", "MyEntity2", "ReferencedIgnoredColumn", SqlType.Int64, true);
      CreateForeignKey(secondCatalog, "dbo", "MyEntity2", "ReferencedIgnoredColumn", "MyEntity1", "Id", "FK_MyEntity2_MyEntity1_MyEntity1ID", true);
      var addedColumnsNames = new[] {"Id", "FirstColumn", "MyEntity2Id"};
      var addedColumnsTypes = new[] {SqlType.Int32, SqlType.VarChar, SqlType.Int64};
      CreateTable(secondCatalog, "dbo", "MyIgnoredEntity", addedColumnsNames, addedColumnsTypes, true);
      CreatePrimaryKey(secondCatalog, "dbo", "MyIgnoredEntity", "Id", "PK_MyIgnoreTable_Id", true);
      CreateForeignKey(secondCatalog, "dbo", "MyIgnoredEntity", "MyEntity2Id", "MyEntity2", "Id", "FK_MyIgnoredEntity_MyEntity2_Id", true);
      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("ReferencedIgnoredColumn").WhenSchema("dbo").WhenDatabase(Multimapping.MultidatabaseTest.Database2Name);
      ignoreRules.IgnoreTable("MyIgnoredEntity").WhenDatabase(Multimapping.MultidatabaseTest.Database2Name);
      ignoreRules.IgnoreTable("IgnoredTable").WhenDatabase(Multimapping.MultidatabaseTest.Database1Name);
      ignoreRules.IgnoreColumn("SomeIgnoredField").WhenTable("Order").WhenDatabase(Multimapping.MultidatabaseTest.Database1Name);
      ignoreRules.IgnoreColumn("IgnoredColumn.Id").WhenTable("Author").WhenDatabase(Multimapping.MultidatabaseTest.Database1Name);

      var validateDomain = BuildDomain(DomainUpgradeMode.Validate, typeof (Model2.Customer), typeof (Model3.MyEntity1), ignoreRules);
      validateDomain.Dispose();
    }

    [Test]
    public void MultischemaUpgrageInPerformModeTest()
    {
      SetMultischema();
      Require.AllFeaturesSupported(ProviderFeatures.Multischema);
      isMultischemaTest = true;
      var catalog = GetCatalog();
      CreateSchema(catalog, "Model1");
      CreateSchema(catalog, "Model2");
      BuildDomainAndFillData();

      catalog = GetCatalog();
      CreateColumn(catalog, "Model2", "MyEntity2", "ReferencedIgnoredColumn", SqlType.Int64);
      CreateForeignKey(catalog, "Model2", "MyEntity2", "ReferencedIgnoredColumn", "MyEntity1", "Id", "FK_MyEntity2_MyEntity1_MyEntity1ID");
      var addedColumnsNames = new[] {"Id", "FirstColumn", "MyEntity2Id"};
      var addedColumnsTypes = new[] {SqlType.Int32, SqlType.VarChar, SqlType.Int64};
      CreateTable(catalog, "Model2", "MyIgnoredEntity", addedColumnsNames, addedColumnsTypes);
      CreatePrimaryKey(catalog, "Model2", "MyIgnoredEntity", "Id", "PK_MyIgnoreTable_Id");
      CreateForeignKey(catalog, "Model2", "MyIgnoredEntity", "MyEntity2Id", "MyEntity2", "Id", "FK_MyIgnoredEntity_MyEntity2_Id");

      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("ReferencedIgnoredColumn").WhenSchema("Model2");
      ignoreRules.IgnoreTable("MyIgnoredEntity").WhenSchema("Model2");
      ignoreRules.IgnoreTable("IgnoredTable").WhenSchema("Model1");
      ignoreRules.IgnoreColumn("SomeIgnoredField").WhenTable("Order").WhenSchema("Model1");
      ignoreRules.IgnoreColumn("IgnoredColumn.Id").WhenTable("Author").WhenSchema("Model1");
      UpgradeDomain(DomainUpgradeMode.Perform, ignoreRules);

      ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("ReferencedIgnoredColumn").WhenSchema("Model2");
      ignoreRules.IgnoreTable("MyIgnoredEntity").WhenSchema("Model2");
      BuildDomainInValidateMode(ignoreRules);
      ValidateMyEntity(catalog, "Model2");
    }

    [Test]
    public void MultischemaUpgrageInPerformSafelyModeTest()
    {
      SetMultischema();
      Require.AllFeaturesSupported(ProviderFeatures.Multischema);
      isMultischemaTest = true;
      var catalog = GetCatalog();
      CreateSchema(catalog, "Model1");
      CreateSchema(catalog, "Model2");
      BuildDomainAndFillData();

      catalog = GetCatalog();
      CreateColumn(catalog, "Model2", "MyEntity2", "ReferencedIgnoredColumn", SqlType.Int64);
      CreateForeignKey(catalog, "Model2", "MyEntity2", "ReferencedIgnoredColumn", "MyEntity1", "Id", "FK_MyEntity2_MyEntity1_MyEntity1ID");
      var addedColumnsNames = new[] {"Id", "FirstColumn", "MyEntity2Id"};
      var addedColumnsTypes = new[] {SqlType.Int32, SqlType.VarChar, SqlType.Int64};
      CreateTable(catalog, "Model2", "MyIgnoredEntity", addedColumnsNames, addedColumnsTypes);
      CreatePrimaryKey(catalog, "Model2", "MyIgnoredEntity", "Id", "PK_MyIgnoreTable_Id");
      CreateForeignKey(catalog, "Model2", "MyIgnoredEntity", "MyEntity2Id", "MyEntity2", "Id", "FK_MyIgnoredEntity_MyEntity2_Id");

      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("ReferencedIgnoredColumn").WhenSchema("Model2");
      ignoreRules.IgnoreTable("MyIgnoredEntity").WhenSchema("Model2");
      ignoreRules.IgnoreTable("IgnoredTable").WhenSchema("Model1");
      ignoreRules.IgnoreColumn("SomeIgnoredField").WhenTable("Order").WhenSchema("Model1");
      ignoreRules.IgnoreColumn("IgnoredColumn.Id").WhenTable("Author").WhenSchema("Model1");
      UpgradeDomain(DomainUpgradeMode.PerformSafely, ignoreRules);

      ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("ReferencedIgnoredColumn").WhenSchema("Model2");
      ignoreRules.IgnoreTable("MyIgnoredEntity").WhenSchema("Model2");
      BuildDomainInValidateMode(ignoreRules);
      ValidateMyEntity(catalog, "Model2");
    }

    [Test]
    public void MultidatabaseUpgradeInPerformModeTest()
    {
      SetMultidatabase();
      Require.AllFeaturesSupported(ProviderFeatures.Multidatabase);
      isMultidatabaseTest = true;
      BuildDomainAndFillData();

      var secondCatalog = GetCatalog(Multimapping.MultidatabaseTest.Database2Name);
      CreateColumn(secondCatalog, "dbo", "MyEntity2", "ReferencedIgnoredColumn", SqlType.Int64, true);
      CreateForeignKey(secondCatalog, "dbo", "MyEntity2", "ReferencedIgnoredColumn", "MyEntity1", "Id", "FK_MyEntity2_MyEntity1_MyEntity1ID", true);
      var addedColumnsNames = new[] {"Id", "FirstColumn", "MyEntity2Id"};
      var addedColumnsTypes = new[] {SqlType.Int32, SqlType.VarChar, SqlType.Int64};
      CreateTable(secondCatalog, "dbo", "MyIgnoredEntity", addedColumnsNames, addedColumnsTypes, true);
      CreatePrimaryKey(secondCatalog, "dbo", "MyIgnoredEntity", "Id", "PK_MyIgnoreTable_Id", true);
      CreateForeignKey(secondCatalog, "dbo", "MyIgnoredEntity", "MyEntity2Id", "MyEntity2", "Id", "FK_MyIgnoredEntity_MyEntity2_Id", true);

      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("ReferencedIgnoredColumn").WhenDatabase(Multimapping.MultidatabaseTest.Database2Name);
      ignoreRules.IgnoreTable("MyIgnoredEntity").WhenDatabase(Multimapping.MultidatabaseTest.Database2Name);
      ignoreRules.IgnoreTable("IgnoredTable").WhenDatabase(Multimapping.MultidatabaseTest.Database1Name);
      ignoreRules.IgnoreColumn("SomeIgnoredField").WhenTable("Order").WhenDatabase(Multimapping.MultidatabaseTest.Database1Name);
      ignoreRules.IgnoreColumn("IgnoredColumn.Id").WhenTable("Author").WhenDatabase(Multimapping.MultidatabaseTest.Database1Name);
      UpgradeDomain(DomainUpgradeMode.Perform, ignoreRules);

      ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("ReferencedIgnoredColumn").WhenDatabase(Multimapping.MultidatabaseTest.Database2Name);
      ignoreRules.IgnoreTable("MyIgnoredEntity").WhenDatabase(Multimapping.MultidatabaseTest.Database2Name);
      BuildDomainInValidateMode(ignoreRules);
      ValidateMyEntity(secondCatalog, "dbo", true);
    }

    [Test]
    public void MultidatabaseUpgradeInPerformSafelyModeTest()
    {
      SetMultidatabase();
      Require.AllFeaturesSupported(ProviderFeatures.Multidatabase);
      isMultidatabaseTest = true;
      BuildDomainAndFillData();

      var secondCatalog = GetCatalog(Multimapping.MultidatabaseTest.Database2Name);
      CreateColumn(secondCatalog, "dbo", "MyEntity2", "ReferencedIgnoredColumn", SqlType.Int64, true);
      CreateForeignKey(secondCatalog, "dbo", "MyEntity2", "ReferencedIgnoredColumn", "MyEntity1", "Id", "FK_MyEntity2_MyEntity1_MyEntity1ID", true);
      var addedColumnsNames = new[] {"Id", "FirstColumn", "MyEntity2Id"};
      var addedColumnsTypes = new[] {SqlType.Int32, SqlType.VarChar, SqlType.Int64};
      CreateTable(secondCatalog, "dbo", "MyIgnoredEntity", addedColumnsNames, addedColumnsTypes, true);
      CreatePrimaryKey(secondCatalog, "dbo", "MyIgnoredEntity", "Id", "PK_MyIgnoreTable_Id", true);
      CreateForeignKey(secondCatalog, "dbo", "MyIgnoredEntity", "MyEntity2Id", "MyEntity2", "Id", "FK_MyIgnoredEntity_MyEntity2_Id", true);

      var ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("ReferencedIgnoredColumn").WhenDatabase(Multimapping.MultidatabaseTest.Database2Name);
      ignoreRules.IgnoreTable("MyIgnoredEntity").WhenDatabase(Multimapping.MultidatabaseTest.Database2Name);
      ignoreRules.IgnoreTable("IgnoredTable").WhenDatabase(Multimapping.MultidatabaseTest.Database1Name);
      ignoreRules.IgnoreColumn("SomeIgnoredField").WhenTable("Order").WhenDatabase(Multimapping.MultidatabaseTest.Database1Name);
      ignoreRules.IgnoreColumn("IgnoredColumn.Id").WhenTable("Author").WhenDatabase(Multimapping.MultidatabaseTest.Database1Name);
      UpgradeDomain(DomainUpgradeMode.PerformSafely, ignoreRules);

      ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreColumn("ReferencedIgnoredColumn").WhenDatabase(Multimapping.MultidatabaseTest.Database2Name);
      ignoreRules.IgnoreTable("MyIgnoredEntity").WhenDatabase(Multimapping.MultidatabaseTest.Database2Name);
      BuildDomainInValidateMode(ignoreRules);
      ValidateMyEntity(secondCatalog, "dbo", true);
    }

    private Domain BuildDomain(DomainUpgradeMode mode, Type sourceType, IgnoreRuleCollection ignoreRules = null)
    {
      var configuration = DomainConfigurationFactory.Create();
      configuration.UpgradeMode = mode;
      configuration.Types.Register(sourceType.Assembly, sourceType.Namespace);
      if (ignoreRules!=null)
        configuration.IgnoreRules = ignoreRules;
      return Domain.Build(configuration);
    }

    private Domain BuildDomain(DomainUpgradeMode mode, Type fstSourceType,
      Type scndSourceType,  IgnoreRuleCollection ignoreRules = null)
    {
      var config = DomainConfigurationFactory.Create();
      config.UpgradeMode = mode;
      if (isMultischemaTest) {
        config.MappingRules.Map(fstSourceType.Assembly,fstSourceType.Namespace).ToSchema("Model1");
        config.MappingRules.Map(scndSourceType.Assembly, scndSourceType.Namespace).ToSchema("Model2");
      }
      else if(isMultidatabaseTest) {
        config.MappingRules.Map(fstSourceType.Assembly, fstSourceType.Namespace).ToDatabase(Multimapping.MultidatabaseTest.Database1Name);
        config.MappingRules.Map(scndSourceType.Assembly, scndSourceType.Namespace).ToDatabase(Multimapping.MultidatabaseTest.Database2Name);
      }
      config.DefaultSchema = "dbo";
      config.DefaultDatabase = GetConnectionInfo().ConnectionUrl.GetDatabase();
      config.Types.Register(fstSourceType.Assembly, fstSourceType.Namespace);
      config.Types.Register(scndSourceType.Assembly, scndSourceType.Namespace);
      if (ignoreRules!=null)
        config.IgnoreRules = ignoreRules;
      return Domain.Build(config);
    }

    private void BuildDomainAndFillData()
    {
      Domain domain;
      if (isMultidatabaseTest || isMultischemaTest)
        domain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model1.Customer), typeof (Model3.MyEntity1));
      else
        domain = BuildDomain(DomainUpgradeMode.Recreate, typeof (Model1.Customer));
      using (var session = domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var author = new Model1.Author {FirstName = "Иван", LastName = "Гончаров", Birthday = new DateTime(1812, 6, 18)};
        var book = new Model1.Book {ISBN = "9780140440409", Title = "Обломов"};
        book.Authors.Add(author);
        var customer = new Model1.Customer {FirstName = "Алексей", LastName = "Кулаков", Birthday = new DateTime(1988, 8, 31)};
        var order = new Model1.Order {Book = book, Customer = customer, SomeIgnoredField = "Secret information for FBI :)"};
        if(isMultidatabaseTest || isMultischemaTest) {
          new Model3.MyEntity1 {FirstColumn = "first"};
          new Model3.MyEntity2 {FirstColumn = "first"};
        }
        transaction.Complete();
      }
      domain.Dispose();
    }

    private void UpgradeDomain(DomainUpgradeMode mode)
    {
      IgnoreRuleCollection ignoreRules = new IgnoreRuleCollection();
      ignoreRules.IgnoreTable("IgnoredTable");
      ignoreRules.IgnoreColumn("SomeIgnoredField").WhenTable("Order");
      ignoreRules.IgnoreColumn("IgnoredColumn.Id").WhenTable("Author");
      using (var domain = BuildDomain(mode, typeof (Model2.Customer), ignoreRules))
      using (var session = domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var currentCustomer = session.Query.All<Model2.Customer>().First(c => c.LastName=="Кулаков");
        var order = session.Query.All<Model2.Order>().First(o => o.Customer.LastName==currentCustomer.LastName);
        var newCustomer = new Model2.Customer {FirstName = "Fred", LastName = "Smith", Birthday = new DateTime(1998, 7, 9)};
        order.Customer = newCustomer;
        changedOrderKey = order.Key;
        transaction.Complete();
      }
    }

    private void UpgradeDomain(DomainUpgradeMode mode, IgnoreRuleCollection ignoreRules)
    {
      using (var performDomain = BuildDomain(mode, typeof(Model2.Customer), typeof(Model3.MyEntity1), ignoreRules))
      using (var session = performDomain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var currentCustomer = session.Query.All<Model2.Customer>().First(c => c.LastName == "Кулаков");
        var order = session.Query.All<Model2.Order>().First(o => o.Customer.LastName == currentCustomer.LastName);
        var newCustomer = new Model2.Customer { FirstName = "Fred", LastName = "Smith", Birthday = new DateTime(1998, 7, 9) };
        order.Customer = newCustomer;
        changedOrderKey = order.Key;
        var currentEntity = session.Query.All<Model3.MyEntity2>().First(ent => ent.FirstColumn == "first");
        currentEntity.FirstColumn = "second";
        transaction.Complete();
      }
    }
    
    private void BuildDomainInValidateMode()
    {
      var configuration = DomainConfigurationFactory.Create();
      configuration.UpgradeMode = DomainUpgradeMode.Validate;
      configuration.Types.Register(typeof (Model1.Customer).Assembly, typeof (Model1.Customer).Namespace);
      using (var domain = Domain.Build(configuration))
      using (var session = domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var result = session.Query.All<Model1.Order>().First(o => o.Key==changedOrderKey);
        Assert.That(result.SomeIgnoredField, Is.EqualTo("Secret information for FBI :)"));
      }
    }

    private void BuildDomainInValidateMode(IgnoreRuleCollection ignoreRules)
    {
      using (var validateDomain = BuildDomain(DomainUpgradeMode.Validate, typeof(Model1.Customer), typeof(Model3.MyEntity1), ignoreRules))
      using (var session = validateDomain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var result = session.Query.All<Model1.Order>().First(o => o.Key == changedOrderKey);
        Assert.That(result.SomeIgnoredField, Is.EqualTo("Secret information for FBI :)"));
      }
    }

    private Catalog GetCatalog(string catalogName = null)
    {
      Catalog catalog;
      using (var sqlConnection = sqlDriver.CreateConnection()) {
        sqlConnection.Open();
        sqlConnection.BeginTransaction();
        if (catalogName==null)
          catalog = sqlDriver.ExtractCatalog(sqlConnection);
        else {
          var sqlExtractionTaskList = new List<SqlExtractionTask>();
          sqlExtractionTaskList.Add(new SqlExtractionTask(catalogName));
          catalog = sqlDriver.Extract(sqlConnection, sqlExtractionTaskList).Catalogs.First();
        }
        sqlConnection.Commit();
        sqlConnection.Close();
      }
      return catalog;
    }

    private ConnectionInfo GetConnectionInfo()
    {
      return DomainConfigurationFactory.Create().ConnectionInfo;
    }

    private void ExecuteNonQuery(string commandText)
    {
      using (var connection = sqlDriver.CreateConnection()) {
        connection.Open();
        connection.CreateCommand(commandText).ExecuteNonQuery();
        connection.Close();
      }
    }

    private object ExecuteQuery(string commandText, int returnedColumnIndex)
    {
      var result = new object();
      using (var connection = sqlDriver.CreateConnection()) {
        connection.Open();
        var reader = connection.CreateCommand(commandText).ExecuteReader();
        if (reader.Read())
          result = reader.GetValue(returnedColumnIndex);
        connection.Close();
      }
      return result;
    }

    private void ValidateMyEntity(Catalog catalog, string schema, bool useDatabasePrefix = false)
    {
      SqlTableRef myEntity2 = SqlDml.TableRef(catalog.Schemas[schema].Tables["MyEntity2"]);
      SqlSelect select = SqlDml.Select(myEntity2);
      var compileConfiguration = new SqlCompilerConfiguration();
      compileConfiguration.DatabaseQualifiedObjects = useDatabasePrefix;
      var commandText = sqlDriver.Compile(select, compileConfiguration).GetCommandText();
      Assert.That(ExecuteQuery(commandText, 2), Is.EqualTo(DBNull.Value));
    }

    private void CreateForeignKey(Catalog catalog, string schema, string table, string column, string referencedTable,
      string referencedColumn, string foreignKeyName, bool useDatabasePrefix = false)
    {
      var foreignKey = catalog.Schemas[schema].Tables[table].CreateForeignKey(foreignKeyName);
      foreignKey.Columns.Add(catalog.Schemas[schema].Tables[table].TableColumns[column]);
      foreignKey.ReferencedTable = catalog.Schemas[schema].Tables[referencedTable];
      foreignKey.ReferencedColumns.Add(catalog.Schemas[schema].Tables[referencedTable].TableColumns[referencedColumn]);
      var alter = SqlDdl.Alter(catalog.Schemas[schema].Tables[table], SqlDdl.AddConstraint(foreignKey));
      var sqlComplieConfig = new SqlCompilerConfiguration();
      sqlComplieConfig.DatabaseQualifiedObjects = useDatabasePrefix;
      string commandText = sqlDriver.Compile(alter, sqlComplieConfig).GetCommandText();
      ExecuteNonQuery(commandText);
    }

    private void CreatePrimaryKey(Catalog catalog, string schema, string table, string column, string primaryKeyName, bool useDatabasePrefix = false)
    {
      var primaryKey = catalog.Schemas[schema].Tables[table].CreatePrimaryKey(primaryKeyName,
        catalog.Schemas[schema].Tables[table].TableColumns[column]);
      var alter = SqlDdl.Alter(catalog.Schemas[schema].Tables[table], SqlDdl.AddConstraint(primaryKey));
      var sqlComplieConfig = new SqlCompilerConfiguration();
      sqlComplieConfig.DatabaseQualifiedObjects = useDatabasePrefix;
      string commandText = sqlDriver.Compile(alter, sqlComplieConfig).GetCommandText();
      ExecuteNonQuery(commandText);
    }

    private void CreateColumn(Catalog catalog, string schema, string table, string columnName, SqlType columnType, bool useDatabasePrefix = false)
    {
      var column = catalog.Schemas[schema].Tables[table].CreateColumn(columnName, new SqlValueType(columnType));
      column.IsNullable = true;
      SqlAlterTable alter = SqlDdl.Alter(catalog.Schemas[schema].Tables[table], SqlDdl.AddColumn(column));
      var sqlComplieConfig = new SqlCompilerConfiguration();
      sqlComplieConfig.DatabaseQualifiedObjects = useDatabasePrefix;
      string commandText = sqlDriver.Compile(alter, sqlComplieConfig).GetCommandText();
      ExecuteNonQuery(commandText);
    }

    private void CreateTable(Catalog catalog, string schema, string tableName, string[] columns, SqlType[] columnTypes, bool useDatabasePrefix = false)
    {
      var table = catalog.Schemas[schema].CreateTable(tableName);
      for (var i = 0; i < columns.Count(); i++) {
        new TableColumn(table, columns[i], new SqlValueType(columnTypes[i]));
      }
      SqlCreateTable create = SqlDdl.Create(table);
      var sqlComplieConfig = new SqlCompilerConfiguration();
      sqlComplieConfig.DatabaseQualifiedObjects = useDatabasePrefix;
      string commandText = sqlDriver.Compile(create, sqlComplieConfig).GetCommandText();
      ExecuteNonQuery(commandText);
    }

    private void CreateSchema(Catalog catalog, string schemaName)
    {
      if (catalog.Schemas[schemaName]==null) {
        catalog.CreateSchema(schemaName);
        SqlCreateSchema schemaCreate = SqlDdl.Create(catalog.Schemas[schemaName]);
        string commandText = sqlDriver.Compile(schemaCreate).GetCommandText();
        ExecuteNonQuery(commandText);
      }
    }

    private void SetMultidatabase()
    {
      isMultidatabaseTest = true;
      isMultischemaTest = false;
    }

    private void SetMultischema()
    {
      isMultidatabaseTest = false;
      isMultischemaTest = true;
    }

    private void ClearMultidatabaseAndMultischemaFlags()
    {
      isMultidatabaseTest = false;
      isMultischemaTest = false;
    }
  }
}
