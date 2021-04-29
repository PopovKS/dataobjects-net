// Copyright (C) 2016-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kulakov
// Created:    2016.12.13

using System.Text;
using NUnit.Framework;
using Xtensive.Core;
using Xtensive.Orm.Configuration;
using part1 = Xtensive.Orm.Tests.Upgrade.NodeBasedExtractedModelBuilderTestModel.Part1;
using part2 = Xtensive.Orm.Tests.Upgrade.NodeBasedExtractedModelBuilderTestModel.Part2;

namespace Xtensive.Orm.Tests.Upgrade.NodeBasedExtractedModelBuilderTestModel
{
  namespace Part1
  {
    [HierarchyRoot]
    public class TestEntity1 : Entity
    {
      [Field, Key]
      public int Id { get; set; }

      [Field]
      public string Text { get; set; }
    }
  }

  namespace Part2
  {
    [HierarchyRoot]
    public class TestEntity2 : Entity
    {
      [Field, Key]
      public int Id { get; set; }

      [Field]
      public string Text { get; set; }
    }
  }
}

namespace Xtensive.Orm.Tests.Upgrade
{
  public class NodeBasedExtractedModelBuilderTest
  {
    private const string DOTests1Db = WellKnownDatabases.MultiDatabase.AdditionalDb1;
    private const string DOTests2Db = WellKnownDatabases.MultiDatabase.AdditionalDb2;
    private const string DOTests3Db = WellKnownDatabases.MultiDatabase.AdditionalDb3;
    private const string DOTests4Db = WellKnownDatabases.MultiDatabase.AdditionalDb4;

    private const string dbo = WellKnownSchemas.SqlServerDefaultSchema;
    private const string Schema1 = WellKnownSchemas.Schema1;

    [Test]
    public void MultischemaWithDatabaseSwitchingTest()
    {
      Require.ProviderIs(StorageProvider.SqlServer);
      var masterConnection = BuildConnectionToMaster(DomainConfigurationFactory.Create().ConnectionInfo);
      var configuration = new DomainConfiguration(masterConnection);
      configuration.Types.Register(typeof(part1.TestEntity1));
      configuration.UpgradeMode = DomainUpgradeMode.Recreate;
      configuration.DefaultSchema = dbo;
      configuration.ConnectionInitializationSql = $"USE [{DOTests1Db}]";
      using (var domain = Domain.Build(configuration)) {
        var domainCopyNode = new NodeConfiguration("1") {
          ConnectionInfo = masterConnection,
          ConnectionInitializationSql = $"USE [{DOTests1Db}]",
          UpgradeMode = DomainUpgradeMode.Recreate
        };
        domainCopyNode.SchemaMapping.Add(dbo, dbo);
        _ = domain.StorageNodeManager.AddNode(domainCopyNode);

        var anotherDatabaseNode = new NodeConfiguration("2") {
          ConnectionInfo = masterConnection,
          ConnectionInitializationSql = $"USE [{DOTests2Db}]",
          UpgradeMode = DomainUpgradeMode.Recreate
        };
        anotherDatabaseNode.SchemaMapping.Add(dbo, dbo);
        _ = domain.StorageNodeManager.AddNode(anotherDatabaseNode);

        var thirdDatabaseNode = new NodeConfiguration("3") {
          ConnectionInfo = masterConnection,
          ConnectionInitializationSql = $"USE [{DOTests3Db}]",
          UpgradeMode = DomainUpgradeMode.Recreate
        };
        thirdDatabaseNode.SchemaMapping.Add(dbo, Schema1);
        _ = domain.StorageNodeManager.AddNode(thirdDatabaseNode);
      }

      configuration = new DomainConfiguration(masterConnection);
      configuration.Types.Register(typeof(part1.TestEntity1));
      configuration.DefaultSchema = dbo;
      configuration.UpgradeMode = DomainUpgradeMode.Skip;
      configuration.ConnectionInitializationSql = $"USE [{DOTests1Db}]";
      using (var domain = Domain.Build(configuration)) {
        var domainCopyNode = new NodeConfiguration("1") {
          ConnectionInfo = masterConnection,
          ConnectionInitializationSql = $"USE [{DOTests1Db}]",
          UpgradeMode = DomainUpgradeMode.Skip
        };
        domainCopyNode.SchemaMapping.Add(dbo, dbo);
        _ = domain.StorageNodeManager.AddNode(domainCopyNode);

        var anotherDatabaseNode = new NodeConfiguration("2") {
          ConnectionInfo = masterConnection,
          ConnectionInitializationSql = $"USE [{DOTests2Db}]",
          UpgradeMode = DomainUpgradeMode.Skip
        };
        anotherDatabaseNode.SchemaMapping.Add(dbo, dbo);
        _ = domain.StorageNodeManager.AddNode(anotherDatabaseNode);

        var thirdDatabaseNode = new NodeConfiguration("3") {
          ConnectionInfo = masterConnection,
          ConnectionInitializationSql = $"USE [{DOTests3Db}]",
          UpgradeMode = DomainUpgradeMode.Skip
        };
        thirdDatabaseNode.SchemaMapping.Add(dbo, Schema1);
        _ = domain.StorageNodeManager.AddNode(thirdDatabaseNode);

        var testEntity1Type = domain.Model.Types[typeof(part1.TestEntity1)];
        var defaultNode = domain.StorageNodeManager.GetNode(WellKnown.DefaultNodeId);
        var table = defaultNode.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests1Db));

        var node1 = domain.StorageNodeManager.GetNode("1");
        table = node1.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests1Db));

        var node2 = domain.StorageNodeManager.GetNode("2");
        table = node2.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests2Db));

        var node3 = domain.StorageNodeManager.GetNode("3");
        table = node3.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(Schema1));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests3Db));
      }
    }

    [Test]
    public void MultischemaWithoutDatabaseSwitchingTest()
    {
      Require.ProviderIs(StorageProvider.SqlServer);
      var configuration = DomainConfigurationFactory.Create();
      configuration.Types.Register(typeof(part1.TestEntity1));
      configuration.UpgradeMode = DomainUpgradeMode.Recreate;
      configuration.DefaultSchema = dbo;

      using (var domain = Domain.Build(configuration)) {
        var domainCopyNode = new NodeConfiguration("1");
        domainCopyNode.SchemaMapping.Add(dbo, dbo);
        domainCopyNode.UpgradeMode = DomainUpgradeMode.Recreate;
        _ = domain.StorageNodeManager.AddNode(domainCopyNode);

        var anotherDatabaseNode = new NodeConfiguration("2");
        anotherDatabaseNode.SchemaMapping.Add(dbo, Schema1);
        anotherDatabaseNode.UpgradeMode = DomainUpgradeMode.Recreate;
        _ = domain.StorageNodeManager.AddNode(anotherDatabaseNode);
      }

      configuration = DomainConfigurationFactory.Create();
      configuration.Types.Register(typeof(part1.TestEntity1));
      configuration.DefaultSchema = dbo;
      configuration.UpgradeMode = DomainUpgradeMode.Skip;
      using (var domain = Domain.Build(configuration)) {
        var domainCopyNode = new NodeConfiguration("1") { UpgradeMode = DomainUpgradeMode.Skip };
        domainCopyNode.SchemaMapping.Add(dbo, dbo);
        _ = domain.StorageNodeManager.AddNode(domainCopyNode);

        var anotherDatabaseNode = new NodeConfiguration("2") { UpgradeMode = DomainUpgradeMode.Skip };
        anotherDatabaseNode.SchemaMapping.Add(dbo, Schema1);
        _ = domain.StorageNodeManager.AddNode(anotherDatabaseNode);

        var testEntity1Type = domain.Model.Types[typeof(part1.TestEntity1)];
        var defaultNode = domain.StorageNodeManager.GetNode(WellKnown.DefaultNodeId);
        var table = defaultNode.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(WellKnownDatabases.MultiDatabase.MainDb));

        var node1 = domain.StorageNodeManager.GetNode("1");
        table = node1.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(WellKnownDatabases.MultiDatabase.MainDb));

        var node2 = domain.StorageNodeManager.GetNode("2");
        table = node2.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(Schema1));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(WellKnownDatabases.MultiDatabase.MainDb));
      }
    }

    [Test]
    public void MultidatabaseNodesToOneDatabaseSetTest()
    {
      Require.ProviderIs(StorageProvider.SqlServer);

      var masterConnectionInfo = BuildConnectionToMaster(DomainConfigurationFactory.Create().ConnectionInfo);
      var configuration = new DomainConfiguration(masterConnectionInfo) {
        UpgradeMode = DomainUpgradeMode.Recreate,
        DefaultSchema = dbo,
        DefaultDatabase = DOTests1Db
      };
      configuration.Types.Register(typeof(part1.TestEntity1));
      configuration.Types.Register(typeof(part2.TestEntity2));
      configuration.MappingRules.Map(typeof(part1.TestEntity1).Namespace).ToDatabase(DOTests1Db);
      configuration.MappingRules.Map(typeof(part2.TestEntity2).Namespace).ToDatabase(DOTests2Db);

      using (var domain = Domain.Build(configuration)) {
        var domainCopyNode = new NodeConfiguration("1") {
          ConnectionInfo = masterConnectionInfo,
          UpgradeMode = DomainUpgradeMode.Recreate
        };
        domainCopyNode.SchemaMapping.Add(dbo, dbo);
        _ = domain.StorageNodeManager.AddNode(domainCopyNode);

        var anotherDatabaseNode = new NodeConfiguration("2") {
          ConnectionInfo = masterConnectionInfo,
          UpgradeMode = DomainUpgradeMode.Recreate
        };
        anotherDatabaseNode.SchemaMapping.Add(dbo, dbo);
        _ = domain.StorageNodeManager.AddNode(anotherDatabaseNode);

        var testEntity1Type = domain.Model.Types[typeof(part1.TestEntity1)];
        var testEntity2Type = domain.Model.Types[typeof(part2.TestEntity2)];

        var defaultNode = domain.StorageNodeManager.GetNode(WellKnown.DefaultNodeId);
        var table = defaultNode.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests1Db));
        table = defaultNode.Mapping[testEntity2Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests2Db));

        var node1 = domain.StorageNodeManager.GetNode("1");
        table = node1.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests1Db));
        table = node1.Mapping[testEntity2Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests2Db));

        var node2 = domain.StorageNodeManager.GetNode("2");
        table = node2.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests1Db));
        table = node2.Mapping[testEntity2Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests2Db));
      }

      configuration = new DomainConfiguration(masterConnectionInfo) {
        UpgradeMode = DomainUpgradeMode.Skip,
        DefaultSchema = dbo,
        DefaultDatabase = DOTests1Db
      };
      configuration.Types.Register(typeof(part1.TestEntity1));
      configuration.Types.Register(typeof(part2.TestEntity2));
      configuration.MappingRules.Map(typeof(part1.TestEntity1).Namespace).ToDatabase(DOTests1Db);
      configuration.MappingRules.Map(typeof(part2.TestEntity2).Namespace).ToDatabase(DOTests2Db);

      using (var domain = Domain.Build(configuration)) {
        var domainCopyNode = new NodeConfiguration("1") {
          ConnectionInfo = masterConnectionInfo,
          UpgradeMode = DomainUpgradeMode.Skip
        };
        domainCopyNode.SchemaMapping.Add(dbo, dbo);
        _ = domain.StorageNodeManager.AddNode(domainCopyNode);

        var anotherDatabaseNode = new NodeConfiguration("2") {
          ConnectionInfo = masterConnectionInfo,
          UpgradeMode = DomainUpgradeMode.Skip
        };
        anotherDatabaseNode.SchemaMapping.Add(dbo, dbo);
        _ = domain.StorageNodeManager.AddNode(anotherDatabaseNode);

        var testEntity1Type = domain.Model.Types[typeof(part1.TestEntity1)];
        var testEntity2Type = domain.Model.Types[typeof(part2.TestEntity2)];

        var defaultNode = domain.StorageNodeManager.GetNode(WellKnown.DefaultNodeId);
        var table = defaultNode.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests1Db));
        table = defaultNode.Mapping[testEntity2Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests2Db));

        var node1 = domain.StorageNodeManager.GetNode("1");
        table = node1.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests1Db));
        table = node1.Mapping[testEntity2Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests2Db));

        var node2 = domain.StorageNodeManager.GetNode("2");
        table = node2.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests1Db));
        table = node2.Mapping[testEntity2Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests2Db));
      }
    }

    [Test]
    public void MultidatabaseNodesToDifferentDatabaseSetTest()
    {
      Require.ProviderIs(StorageProvider.SqlServer);

      var masterConnectionInfo = BuildConnectionToMaster(DomainConfigurationFactory.Create().ConnectionInfo);
      var configuration = new DomainConfiguration(masterConnectionInfo) {
        UpgradeMode = DomainUpgradeMode.Recreate,
        DefaultSchema = dbo,
        DefaultDatabase = DOTests1Db
      };
      configuration.Types.Register(typeof(part1.TestEntity1));
      configuration.Types.Register(typeof(part2.TestEntity2));
      configuration.MappingRules.Map(typeof(part1.TestEntity1).Namespace).ToDatabase(DOTests1Db);
      configuration.MappingRules.Map(typeof(part2.TestEntity2).Namespace).ToDatabase(DOTests2Db);

      using (var domain = Domain.Build(configuration)) {
        var domainCopyNode = new NodeConfiguration("1") {
          ConnectionInfo = masterConnectionInfo,
          UpgradeMode = DomainUpgradeMode.Recreate
        };
        domainCopyNode.SchemaMapping.Add(dbo, dbo);
        domainCopyNode.DatabaseMapping.Add(DOTests1Db, DOTests1Db);
        domainCopyNode.DatabaseMapping.Add(DOTests2Db, DOTests2Db);
        _ = domain.StorageNodeManager.AddNode(domainCopyNode);

        var anotherDatabaseNode = new NodeConfiguration("2") {
          ConnectionInfo = masterConnectionInfo,
          UpgradeMode = DomainUpgradeMode.Recreate
        };
        anotherDatabaseNode.SchemaMapping.Add(dbo, dbo);
        anotherDatabaseNode.DatabaseMapping.Add(DOTests1Db, DOTests3Db);
        anotherDatabaseNode.DatabaseMapping.Add(DOTests2Db, DOTests4Db);
        _ = domain.StorageNodeManager.AddNode(anotherDatabaseNode);

        var testEntity1Type = domain.Model.Types[typeof(part1.TestEntity1)];
        var testEntity2Type = domain.Model.Types[typeof(part2.TestEntity2)];

        var defaultNode = domain.StorageNodeManager.GetNode(WellKnown.DefaultNodeId);
        var table = defaultNode.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests1Db));
        table = defaultNode.Mapping[testEntity2Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests2Db));

        var node1 = domain.StorageNodeManager.GetNode("1");
        table = node1.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests1Db));
        table = node1.Mapping[testEntity2Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests2Db));

        var node2 = domain.StorageNodeManager.GetNode("2");
        table = node2.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests3Db));
        table = node2.Mapping[testEntity2Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests4Db));
      }

      configuration = new DomainConfiguration(masterConnectionInfo) {
        UpgradeMode = DomainUpgradeMode.Skip,
        DefaultSchema = dbo,
        DefaultDatabase = DOTests1Db
      };
      configuration.Types.Register(typeof(part1.TestEntity1));
      configuration.Types.Register(typeof(part2.TestEntity2));
      configuration.MappingRules.Map(typeof(part1.TestEntity1).Namespace).ToDatabase(DOTests1Db);
      configuration.MappingRules.Map(typeof(part2.TestEntity2).Namespace).ToDatabase(DOTests2Db);

      using (var domain = Domain.Build(configuration)) {
        var domainCopyNode = new NodeConfiguration("1") {
          ConnectionInfo = masterConnectionInfo,
          UpgradeMode = DomainUpgradeMode.Skip
        };
        domainCopyNode.SchemaMapping.Add(dbo, dbo);
        domainCopyNode.DatabaseMapping.Add(DOTests1Db, DOTests1Db);
        domainCopyNode.DatabaseMapping.Add(DOTests2Db, DOTests2Db);
        _ = domain.StorageNodeManager.AddNode(domainCopyNode);

        var anotherDatabaseNode = new NodeConfiguration("2") {
          ConnectionInfo = masterConnectionInfo,
          UpgradeMode = DomainUpgradeMode.Skip
        };
        anotherDatabaseNode.SchemaMapping.Add(dbo, dbo);
        anotherDatabaseNode.DatabaseMapping.Add(DOTests1Db, DOTests3Db);
        anotherDatabaseNode.DatabaseMapping.Add(DOTests2Db, DOTests4Db);
        _ = domain.StorageNodeManager.AddNode(anotherDatabaseNode);

        var testEntity1Type = domain.Model.Types[typeof(part1.TestEntity1)];
        var testEntity2Type = domain.Model.Types[typeof(part2.TestEntity2)];

        var defaultNode = domain.StorageNodeManager.GetNode(WellKnown.DefaultNodeId);
        var table = defaultNode.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests1Db));
        table = defaultNode.Mapping[testEntity2Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests2Db));

        var node1 = domain.StorageNodeManager.GetNode("1");
        table = node1.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests1Db));
        table = node1.Mapping[testEntity2Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests2Db));

        var node2 = domain.StorageNodeManager.GetNode("2");
        table = node2.Mapping[testEntity1Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests3Db));
        table = node2.Mapping[testEntity2Type];
        Assert.That(table.Schema.Name, Is.EqualTo(dbo));
        Assert.That(table.Schema.Catalog.Name, Is.EqualTo(DOTests4Db));
      }
    }

    private ConnectionInfo BuildConnectionToMaster(ConnectionInfo connectionInfo)
    {
      var connectionStringTemplate = "{0}://{1}{2}/{3}{4}";
      var loginInfoTemplate = "{0}:{1}@";
      var hostTemplate = "{0}:{1}";
      var parameterTemplate = "{0}={1}";

      var protocol = connectionInfo.ConnectionUrl.Protocol;
      var loginInfo = (!connectionInfo.ConnectionUrl.User.IsNullOrEmpty()) 
        ? string.Format(loginInfoTemplate, connectionInfo.ConnectionUrl.User, connectionInfo.ConnectionUrl.Password)
        : string.Empty;
      var server = (connectionInfo.ConnectionUrl.Port > 0)
        ? string.Format(hostTemplate, connectionInfo.ConnectionUrl.Host, connectionInfo.ConnectionUrl.Port)
        : connectionInfo.ConnectionUrl.Host;
      var database = "master";

      var parameters = string.Empty;
      if (connectionInfo.ConnectionUrl.Params.Count > 0) {
        var stringBuilder = new StringBuilder("?");
        foreach (var parameter in connectionInfo.ConnectionUrl.Params) {
          _ = stringBuilder.Append(string.Format(parameterTemplate, parameter.Key, parameter.Value));
        }
      }
      return new ConnectionInfo(string.Format(connectionStringTemplate, protocol, loginInfo, server, database, parameters));
    }
  }
}
