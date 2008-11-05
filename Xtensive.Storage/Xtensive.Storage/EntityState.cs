// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.07.07

using System;
using System.Diagnostics;
using Xtensive.Core;
using Xtensive.Core.Aspects;
using Xtensive.Core.Tuples;
using Xtensive.Storage.Internals;
using Xtensive.Storage.Model;
using Xtensive.Storage.Resources;
using Activator=Xtensive.Storage.Internals.Activator;

namespace Xtensive.Storage
{
  /// <summary>
  /// The underlying state of the <see cref="Storage.Entity"/>.
  /// </summary>
  public sealed class EntityState : TransactionalStateContainer<DifferentialTuple>
  {
    private Key key;
    private PersistenceState persistenceState;
    private bool hasEntity;
    private Entity entity;

    /// <summary>
    /// Gets the key.
    /// </summary>
    [Infrastructure]
    public Key Key {
      get { return key; }
    }

    /// <summary>
    /// Gets the entity type.
    /// </summary>
    [Infrastructure]
    public TypeInfo Type
    {
      [DebuggerStepThrough]
      get { return key.Type; }
    }

    /// <summary>
    /// Gets the values as <see cref="DifferentialTuple"/>.
    /// </summary>
    [Infrastructure]
    public DifferentialTuple Data {
      get { return State; }
      private set { State = value; }
    }

    /// <summary>
    /// Gets the owner of this instance.
    /// </summary>
    /// <exception cref="NotSupportedException">Property value is already set.</exception>
    [Infrastructure]
    public Entity Entity {
      get {
        var isRemoved = IsRemoved;
        if (!hasEntity) {
          entity = isRemoved ? null : Activator.CreateEntity(Type.UnderlyingType, this);
          hasEntity = true;
        }
        return isRemoved ? null : entity;
      }
      internal set {
        if (hasEntity)
          throw Exceptions.AlreadyInitialized("Entity");
        entity = value;
        hasEntity = true;
      }
    }

    /// <summary>
    /// Gets or sets the persistence state.
    /// </summary>
    [Infrastructure]
    public PersistenceState PersistenceState {
      get { return persistenceState; }
      set {
        if (value==persistenceState)
          return;
        persistenceState = value;
        if (value==PersistenceState.Synchronized)
          return;
        Session.EntityStateRegistry.Register(this);
      }
    }

    /// <summary>
    /// Gets a value indicating whether this entity is removed.
    /// </summary>
    [Infrastructure]
    public bool IsRemoved {
      get {
        return Data==null;
      }
    }

    /// <summary>
    /// Ensures the entity is not removed and its data is actual.
    /// </summary>
    [Infrastructure]
    public void EnsureNotRemoved()
    {
      if (IsRemoved)
        throw new InvalidOperationException(Strings.ExEntityIsRemoved);
    }

    /// <summary>
    /// Updates the entity state to the most current one.
    /// </summary>
    /// <param name="tuple">The state change tuple, or a new state tuple. 
    /// If <see langword="null" />, the entity is considered as removed.</param>
    [Infrastructure]
    public void Update(Tuple tuple)
    {
      EnsureStateIsActual();
      if (tuple==null) // Entity is removed
        Data = null;
      else {
        var data = Data;
        if (data==null) // Entity was marked as removed before
          Data = new DifferentialTuple(tuple.ToRegular());
        else
          data.Origin.MergeWith(tuple);
      }
    }

    /// <inheritdoc/>
    protected override DifferentialTuple LoadState()
    {
      Data = null;
      Fetcher.Fetch(key);
      return Data;
    }

    #region Equality members

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
      return ReferenceEquals(this, obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
      return Key.GetHashCode();
    }

    #endregion

    /// <inheritdoc/>
    [Infrastructure]
    public override string ToString()
    {
      return string.Format("Key = '{0}', Data = {1}, State = {2}", Key, Data.ToRegular(), PersistenceState);
    }


    // Constructors

    internal EntityState(Session session, Key key, Tuple data)
      : base(session)
    {
      ArgumentValidator.EnsureArgumentNotNull(key, "key");
      this.key = key;
      Update(data);
    }
  }
}