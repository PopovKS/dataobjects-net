// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2007.08.08

using System.Reflection;
using NUnit.Framework;
using Xtensive.Core.Testing;
using Xtensive.Storage.Configuration;
using Xtensive.Storage.Configuration.TypeRegistry;
using Xtensive.Storage.Tests.RegistryModel1;

namespace Xtensive.Storage.Tests.RegistryModel1
{
  public class A : Entity
  {
  }

  public class B : A
  {
  }

  public class C
  {
  }

  public class D : A
  {
  }
}

namespace Xtensive.Storage.Tests.RegistryModel2
{
  public class A : Entity
  {
  }

  public class B : A
  {
  }

  public class C
  {
  }

  public class D : A
  {
  }
}

namespace Xtensive.Storage.Tests.Configuration
{
  [TestFixture]
  public class RegistryTest
  {
    [Test]
    public void HierarchyTest()
    {
      DomainConfiguration config = new DomainConfiguration();
      TypeRegistry typeRegistry = config.Types;
      typeRegistry.Register(typeof (A).Assembly, "Xtensive.Storage.Tests.RegistryModel1");
      Assert.IsTrue(typeRegistry.Contains(typeof (A)));
      Assert.IsTrue(typeRegistry.Contains(typeof (B)));
      Assert.IsFalse(typeRegistry.Contains(typeof (C)));
      Assert.IsTrue(typeRegistry.Contains(typeof (D)));

      config = new DomainConfiguration();
      typeRegistry = config.Types;
      typeRegistry.Register(typeof (B).Assembly, "Xtensive.Storage.Tests.RegistryModel1");
      Assert.IsTrue(typeRegistry.Contains(typeof (A)));
      Assert.IsTrue(typeRegistry.Contains(typeof (B)));
      Assert.IsFalse(typeRegistry.Contains(typeof (C)));
      Assert.IsTrue(typeRegistry.Contains(typeof (D)));
    }

    [Test]
    public void ContiniousRegistrationTest()
    {
      DomainConfiguration config = new DomainConfiguration();
      TypeRegistry typeRegistry = config.Types;
      typeRegistry.Register(typeof (A).Assembly, "Xtensive.Storage.Tests.RegistryModel1");
      long amount = typeRegistry.Count;
      typeRegistry.Register(typeof (A).Assembly, "Xtensive.Storage.Tests.RegistryModel2");
      Assert.Less(amount, typeRegistry.Count);
    }

    [Test]
    public void CloneTest()
    {
      DomainConfiguration config = new DomainConfiguration();
      TypeRegistry registry1 = config.Types;
      registry1.Register(typeof (A).Assembly, "Xtensive.Storage.Tests.RegistryModel1");
      TypeRegistry registry2 = registry1.Clone() as TypeRegistry;
      Assert.IsNotNull(registry2);
      Assert.AreEqual(registry1.Count, registry2.Count);
    }

    [Test]
    public void NamespaceFilterTest()
    {
      DomainConfiguration config = new DomainConfiguration();
      TypeRegistry typeRegistry = config.Types;
      typeRegistry.Register(Assembly.GetExecutingAssembly(), "Xtensive.Storage.Tests.RegistryModel2");
      Assert.IsFalse(typeRegistry.Contains(typeof (A)));
      Assert.IsFalse(typeRegistry.Contains(typeof (B)));
      Assert.IsFalse(typeRegistry.Contains(typeof (C)));
      Assert.IsFalse(typeRegistry.Contains(typeof (D)));
      Assert.IsTrue(typeRegistry.Contains(typeof (RegistryModel2.A)));
      Assert.IsTrue(typeRegistry.Contains(typeof (RegistryModel2.B)));
      Assert.IsFalse(typeRegistry.Contains(typeof (RegistryModel2.C)));
      Assert.IsTrue(typeRegistry.Contains(typeof (RegistryModel2.D)));
    }

    [Test]
    public void InvalidRegistrationTest()
    {
      var config = new DomainConfiguration();
      var types = config.Types;
      AssertEx.ThrowsArgumentNullException(() => types.Register((Assembly) null));
      AssertEx.ThrowsArgumentNullException(() => types.Register((Assembly) null, "Xtensive.Storage.Tests.RegistryModel1"));
      AssertEx.ThrowsArgumentException(() => types.Register(Assembly.GetExecutingAssembly(), ""));
    }
  }
}