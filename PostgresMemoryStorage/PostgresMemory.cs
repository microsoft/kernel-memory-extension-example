// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using Pgvector;

namespace Microsoft.KernelMemory.Postgres;

/// <summary>
/// Postgres connector for Kernel Memory.
/// </summary>
public class PostgresMemory : IMemoryDb, IDisposable
{
    private readonly ILogger<PostgresMemory> _log;
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly PostgresDbClient _db;

    /// <summary>
    /// Create a new instance of Postgres KM connector
    /// </summary>
    /// <param name="config">Postgres configuration</param>
    /// <param name="embeddingGenerator">Text embedding generator</param>
    /// <param name="log">Application logger</param>
    public PostgresMemory(
        PostgresConfig config,
        ITextEmbeddingGenerator embeddingGenerator,
        ILogger<PostgresMemory>? log = null)
    {
        this._log = log ?? DefaultLogger<PostgresMemory>.Instance;

        this._embeddingGenerator = embeddingGenerator;
        if (this._embeddingGenerator == null)
        {
            throw new PostgresException("Embedding generator not configured");
        }

        this._db = new PostgresDbClient(config.ConnString, config.Schema);
    }

    /// <inheritdoc />
    public async Task CreateIndexAsync(
        string index,
        int vectorSize,
        CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        if (await this._db.DoesTableExistsAsync(index, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await this._db.CreateTableAsync(index, vectorSize, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetIndexesAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<string>();
        var tables = this._db.GetTablesAsync(cancellationToken).ConfigureAwait(false);
        await foreach (string name in tables)
        {
            result.Add(name);
        }

        return result;
    }

    /// <inheritdoc />
    public Task DeleteIndexAsync(
        string index,
        CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        return this._db.DeleteTableAsync(index, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(
        string index,
        MemoryRecord record,
        CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        await this._db.UpsertAsync(
            tableName: index,
            id: record.Id,
            embedding: new Vector(record.Vector.Data),
            tags: PostgresSchema.GetTags(record),
            content: PostgresSchema.GetContent(record),
            payload: JsonSerializer.Serialize(PostgresSchema.GetPayload(record)),
            lastUpdate: DateTimeOffset.UtcNow,
            cancellationToken).ConfigureAwait(false);

        return record.Id;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string index,
        string text,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = 1,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = new CancellationToken())
    {
        index = NormalizeIndexName(index);

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
        index = NormalizeIndexName(index);

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
        index = NormalizeIndexName(index);

        return this._db.DeleteAsync(tableName: index, id: record.Id, cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            (this._db as IDisposable)?.Dispose();
        }
    }

    #region private ================================================================================

    private static string NormalizeIndexName(string index)
    {
        PostgresSchema.ValidateTableName(index);

        if (string.IsNullOrWhiteSpace(index))
        {
            index = Constants.DefaultIndex;
        }

        return index.Trim();
    }

    #endregion
}
