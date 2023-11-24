// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.Postgres;

/// <summary>
/// Postgres connector for Kernel Memory.
/// </summary>
public class PostgresMemory : IVectorDb
{
    private readonly ILogger<PostgresMemory> _log;

    /// <summary>
    /// Create a new instance of Postgres KM connector
    /// </summary>
    /// <param name="config">Postgres configuration</param>
    /// <param name="log">Application logger</param>
    public PostgresMemory(
        PostgresConfig config,
        ILogger<PostgresMemory>? log = null)
    {
        this._log = log ?? DefaultLogger<PostgresMemory>.Instance;
    }

    /// <inheritdoc />
    public Task CreateIndexAsync(
        string index,
        int vectorSize,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetIndexesAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task DeleteIndexAsync(
        string index,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<string> UpsertAsync(
        string index,
        MemoryRecord record,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string index,
        Embedding embedding,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = 1,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default)
    {
        if (filters != null)
        {
            foreach (MemoryFilter filter in filters)
            {
                if (filter is PostgresMemoryFilter extendedFilter)
                {
                    // use PostgresMemoryFilter filtering logic
                }

                // use MemoryFilter filtering logic
            }
        }

        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<MemoryRecord> GetListAsync(
        string index,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default)
    {
        if (filters != null)
        {
            foreach (MemoryFilter filter in filters)
            {
                if (filter is PostgresMemoryFilter extendedFilter)
                {
                    // use PostgresMemoryFilter filtering logic
                }

                // use MemoryFilter filtering logic
            }
        }

        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        string index,
        MemoryRecord record,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
