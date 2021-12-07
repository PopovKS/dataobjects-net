// Copyright (C) 2009-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.10.30

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// Provides query features for <see cref="SqlProvider"/>s.
  /// </summary>
  public interface IProviderExecutor
  {
    /// <summary>
    /// Executes the specified request.
    /// </summary>
    /// <param name="request">The request to execute.</param>
    /// <returns><see cref="IEnumerator{Tuple}"/> that contains result of execution.</returns>
    IEnumerator<Tuple> ExecuteTupleReader(QueryRequest request);

    /// <summary>
    /// Asynchronously executes the specified request.
    /// </summary>
    /// <param name="request">The request to execute.</param>
    /// <param name="token">Token to cancel operation.</param>
    /// <returns>Task performing the operation.</returns>
    Task<IEnumerator<Tuple>> ExecuteTupleReaderAsync(QueryRequest request, CancellationToken token);

    /// <summary>
    /// Stores the specified tuples in specified table.
    /// </summary>
    /// <param name="descriptor">Persist descriptor.</param>
    /// <param name="tuples">The tuples to store.</param>
    void Store(IPersistDescriptor descriptor, IEnumerable<Tuple> tuples);

    /// <summary>
    /// Asynchronously stores the specified tuples in specified table.
    /// </summary>
    /// <param name="descriptor">Persist descriptor.</param>
    /// <param name="tuples">The tuples to store.</param>
    Task StoreAsync(EnumerationContext context, IPersistDescriptor descriptor, IEnumerable<Tuple> tuples, CancellationToken token);

    /// <summary>
    /// Clears the specified table.
    /// </summary>
    /// <param name="descriptor">Persist descriptor.</param>
    void Clear(IPersistDescriptor descriptor);

    /// <summary>
    /// Executes <see cref="Store"/> and <see cref="Clear"/> via single batch.
    /// </summary>
    /// <param name="descriptor">Persist descriptor</param>
    /// <param name="tuples">Tuples to store</param>
    void Overwrite(IPersistDescriptor descriptor, IEnumerable<Tuple> tuples);
  }
}