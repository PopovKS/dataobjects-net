// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.06.29

using System;

namespace Xtensive.Storage
{
  public partial class Session
  {
    /// <summary>
    /// Occurs when <see cref="Session"/> is about to <see cref="Dispose"/>.
    /// </summary>
    public event EventHandler OnDisposing;

    /// <summary>
    /// Occurs when <see cref="Session"/> is about to <see cref="Persist"/>.
    /// </summary>
    public event EventHandler OnPersisting;

    /// <summary>
    /// Occurs when <see cref="Session"/> persisted.
    /// </summary>
    public event EventHandler OnPersist;

    /// <summary>
    /// Occurs when <see cref="Entity"/> created.
    /// </summary>
    public event EventHandler<EntityEventArgs> OnCreateEntity;

    /// <summary>
    /// Occurs when <see cref="Entity"/> is about to remove.
    /// </summary>
    public event EventHandler<EntityEventArgs> OnRemovingEntity;

    /// <summary>
    /// Occurs when <see cref="Entity"/> removed.
    /// </summary>
    public event EventHandler<EntityEventArgs> OnRemoveEntity;

    /// <summary>
    /// The manager of <see cref="Entity"/>'s events.
    /// </summary>
    public EntityEventBroker EntityEventBroker { get; private set; }
  }
}