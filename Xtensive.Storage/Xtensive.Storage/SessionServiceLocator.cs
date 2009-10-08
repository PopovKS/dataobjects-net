// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.11.15

using System;
using System.Collections.Generic;
using Microsoft.Practices.ServiceLocation;
using Xtensive.Core.Aspects;

namespace Xtensive.Storage
{
  /// <summary>
  /// Provides access to services.
  /// </summary>
  public sealed class SessionServiceLocator : SessionBound,
    IServiceLocator
  {
    private IServiceLocator locator;

    /// <summary>
    /// Set the delegate that is used to retrieve the current container.
    /// </summary>
    /// <param name="newProvider">Delegate that, when called, will return
    /// the current session container.</param>
    [Infrastructure]
    public void SetLocatorProvider(ServiceLocatorProvider newProvider)
    {
      locator = newProvider();
    }

    /// <inheritdoc/>
    [Infrastructure]
    public TService GetInstance<TService>()
    {
      using (Session.Activate())
        return GetLocator().GetInstance<TService>();
    }

    /// <inheritdoc/>
    [Infrastructure]
    public TService GetInstance<TService>(string name)
    {
      using (Session.Activate())
        return GetLocator().GetInstance<TService>(name);
    }

    /// <inheritdoc/>
    [Infrastructure]
    public object GetInstance(Type type)
    {
      using (Session.Activate())
        return GetLocator().GetInstance(type);
    }

    /// <inheritdoc/>
    [Infrastructure]
    public object GetInstance(Type type, string name)
    {
      using (Session.Activate())
        return GetLocator().GetInstance(type, name);
    }

    /// <inheritdoc/>
    [Infrastructure]
    public IEnumerable<TService> GetAllInstances<TService>()
    {
      using (Session.Activate())
        return GetLocator().GetAllInstances<TService>();
    }

    /// <inheritdoc/>
    [Infrastructure]
    public IEnumerable<object> GetAllInstances(Type serviceType)
    {
      using (Session.Activate())
        return GetLocator().GetAllInstances(serviceType);
    }

    /// <inheritdoc/>
    [Infrastructure]
    public object GetService(Type serviceType)
    {
      return GetInstance(serviceType, null);
    }

    private IServiceLocator GetLocator()
    {
      return locator ?? Session.Domain.Services;
    }


    // Constructors

    internal SessionServiceLocator(Session session)
      : base(session)
    {
    }
  }
}