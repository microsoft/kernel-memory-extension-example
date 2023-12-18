// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Pgvector;

namespace Microsoft.KernelMemory.Postgres.Db;

/// <summary>
/// An implementation of a client for Postgres. This class is used to managing postgres database operations.
/// </summary>
internal sealed class PostgresDbClient : IDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    private readonly string _schema;
    private readonly string _tableNamePrefix;
    private readonly string _createTableSql;
    private readonly string _colId;
    private readonly string _colEmbedding;
    private readonly string _colTags;
    private readonly string _colContent;
    private readonly string _colPayload;
    private readonly string _columnsListNoEmbeddings;
    private readonly string _columnsListWithEmbeddings;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresDbClient"/> class.
    /// </summary>
    /// <param name="config">Configuration</param>
    public PostgresDbClient(PostgresConfig config)
    {
        config.Validate();

        NpgsqlDataSourceBuilder dataSourceBuilder = new(config.ConnectionString);
        dataSourceBuilder.UseVector();
        this._dataSource = dataSourceBuilder.Build();
        this._schema = config.Schema;
        this._tableNamePrefix = config.TableNamePrefix;

        this._colId = config.Columns[PostgresConfig.ColumnId];
        this._colEmbedding = config.Columns[PostgresConfig.ColumnEmbedding];
        this._colTags = config.Columns[PostgresConfig.ColumnTags];
        this._colContent = config.Columns[PostgresConfig.ColumnContent];
        this._colPayload = config.Columns[PostgresConfig.ColumnPayload];

        PostgresSchema.ValidateSchemaName(this._schema);
        PostgresSchema.ValidateTableNamePrefix(this._tableNamePrefix);
        PostgresSchema.ValidateFieldName(this._colId);
        PostgresSchema.ValidateFieldName(this._colEmbedding);
        PostgresSchema.ValidateFieldName(this._colTags);
        PostgresSchema.ValidateFieldName(this._colContent);
        PostgresSchema.ValidateFieldName(this._colPayload);

        this._columnsListNoEmbeddings = $"{this._colId},{this._colTags},{this._colContent},{this._colPayload}";
        this._columnsListWithEmbeddings = $"{this._colId},{this._colTags},{this._colContent},{this._colPayload},{this._colEmbedding}";

        this._createTableSql = string.Empty;
        if (config.CreateTableSql?.Count > 0)
        {
            this._createTableSql = string.Join('\n', config.CreateTableSql).Trim();
        }
    }

    /// <summary>
    /// Check if a table exists.
    /// </summary>
    /// <param name="tableName">The name assigned to a table of entries</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>True if the table exists</returns>
    public async Task<bool> DoesTableExistsAsync(
        string tableName,
        CancellationToken cancellationToken = default)
    {
        tableName = this.WithTableNamePrefix(tableName);

        NpgsqlConnection connection = await this._dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using (connection)
        {
            using NpgsqlCommand cmd = connection.CreateCommand();

#pragma warning disable CA2100 // SQL reviewed
            cmd.CommandText = $@"
                SELECT table_name
                FROM information_schema.tables
                    WHERE table_schema = @schema
                        AND table_name = @table
                        AND table_type = 'BASE TABLE'
                LIMIT 1
            ";

            cmd.Parameters.AddWithValue("@schema", this._schema);
            cmd.Parameters.AddWithValue("@table", tableName);
#pragma warning restore CA2100

            using NpgsqlDataReader dataReader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return dataReader.GetString(dataReader.GetOrdinal("table_name")) == tableName;
            }

            return false;
        }
    }

    /// <summary>
    /// Create a table.
    /// </summary>
    /// <param name="tableName">The name assigned to a table of entries</param>
    /// <param name="vectorSize">Embedding vectors dimension</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    public async Task CreateTableAsync(
        string tableName,
        int vectorSize,
        CancellationToken cancellationToken = default)
    {
        tableName = this.WithSchemaAndTableNamePrefix(tableName);

        NpgsqlConnection connection = await this._dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using (connection)
        {
            using NpgsqlCommand cmd = connection.CreateCommand();

#pragma warning disable CA2100 // SQL reviewed
            if (!string.IsNullOrEmpty(this._createTableSql))
            {
                cmd.CommandText = this._createTableSql
                    .Replace(PostgresConfig.SqlPlaceholdersTableName, tableName, StringComparison.Ordinal)
                    .Replace(PostgresConfig.SqlPlaceholdersVectorSize, $"{vectorSize}", StringComparison.Ordinal);
            }
            else
            {
                cmd.CommandText = $@"
                    BEGIN;
                    CREATE TABLE IF NOT EXISTS {tableName} (
                        {this._colId}        TEXT NOT NULL PRIMARY KEY,
                        {this._colEmbedding} vector({vectorSize}),
                        {this._colTags}      TEXT[] DEFAULT '{{}}'::TEXT[] NOT NULL,
                        {this._colContent}   TEXT DEFAULT '' NOT NULL,
                        {this._colPayload}   JSONB DEFAULT '{{}}'::JSONB NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS idx_tags ON {tableName} USING GIN({this._colTags});
                    COMMIT;";
#pragma warning restore CA2100
            }

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Get all tables
    /// </summary>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>A group of tables</returns>
    public async IAsyncEnumerable<string> GetTablesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        NpgsqlConnection connection = await this._dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using (connection)
        {
            using NpgsqlCommand cmd = connection.CreateCommand();

            cmd.CommandText = @"SELECT table_name FROM information_schema.tables
                                WHERE table_schema = @schema AND table_type = 'BASE TABLE';";
            cmd.Parameters.AddWithValue("@schema", this._schema);

            using NpgsqlDataReader dataReader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var tableNameWithPrefix = dataReader.GetString(dataReader.GetOrdinal("table_name"));
                if (tableNameWithPrefix.StartsWith(this._tableNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    yield return tableNameWithPrefix.Remove(0, this._tableNamePrefix.Length);
                }
            }
        }
    }

    /// <summary>
    /// Delete a table.
    /// </summary>
    /// <param name="tableName">Name of the table to delete</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    public async Task DeleteTableAsync(
        string tableName,
        CancellationToken cancellationToken = default)
    {
        tableName = this.WithSchemaAndTableNamePrefix(tableName);
        NpgsqlConnection connection = await this._dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using (connection)
        {
            using NpgsqlCommand cmd = connection.CreateCommand();

#pragma warning disable CA2100 // SQL reviewed
            cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
#pragma warning restore CA2100

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Upsert entry into a table.
    /// </summary>
    /// <param name="tableName">The name assigned to a table of entries</param>
    /// <param name="record">Record to create/update</param>
    /// <param name="lastUpdate">The timestamp of the entry</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    public async Task UpsertAsync(
        string tableName,
        PostgresMemoryRecord record,
        DateTimeOffset? lastUpdate = null,
        CancellationToken cancellationToken = default)
    {
        tableName = this.WithSchemaAndTableNamePrefix(tableName);

        const string EmptyPayload = "{}";
        const string EmptyContent = "";
        string[] emptyTags = Array.Empty<string>();

        NpgsqlConnection connection = await this._dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using (connection)
        {
            using NpgsqlCommand cmd = connection.CreateCommand();

#pragma warning disable CA2100 // SQL reviewed
            cmd.CommandText = $@"
                INSERT INTO {tableName}
                    ({this._colId}, {this._colEmbedding}, {this._colTags}, {this._colContent}, {this._colPayload})
                    VALUES
                    (@id, @embedding, @tags, @content, @payload)
                ON CONFLICT ({this._colId})
                DO UPDATE SET
                    {this._colEmbedding} = @embedding,
                    {this._colTags}      = @tags,
                    {this._colContent}   = @content,
                    {this._colPayload}   = @payload
            ";

            cmd.Parameters.AddWithValue("@id", record.Id);
            cmd.Parameters.AddWithValue("@embedding", record.Embedding);
            cmd.Parameters.AddWithValue("@tags", NpgsqlDbType.Array | NpgsqlDbType.Text, record.Tags.ToArray() ?? emptyTags);
            cmd.Parameters.AddWithValue("@content", NpgsqlDbType.Text, record.Content ?? EmptyContent);
            cmd.Parameters.AddWithValue("@payload", NpgsqlDbType.Jsonb, record.Payload ?? EmptyPayload);
#pragma warning restore CA2100

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Get a list of records
    /// </summary>
    /// <param name="tableName">Table containing the records to fetch</param>
    /// <param name="target">Source vector to compare for similarity</param>
    /// <param name="minSimilarity">Minimum similarity threshold</param>
    /// <param name="filterSql">SQL filter to apply</param>
    /// <param name="sqlUserValues">List of user values passed with placeholders to avoid SQL injection</param>
    /// <param name="limit">Max number of records to retrieve</param>
    /// <param name="offset">Records to skip from the top</param>
    /// <param name="withEmbeddings">Whether to include embedding vectors</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    public async IAsyncEnumerable<(PostgresMemoryRecord record, double similarity)> GetSimilarAsync(
        string tableName,
        Vector target,
        double minSimilarity,
        string? filterSql = null,
        Dictionary<string, object>? sqlUserValues = null,
        int limit = 1,
        int offset = 0,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        tableName = this.WithSchemaAndTableNamePrefix(tableName);

        if (limit <= 0) { limit = int.MaxValue; }

        // Column names
        string columns = withEmbeddings ? this._columnsListWithEmbeddings : this._columnsListNoEmbeddings;
        string similarityActualValue = "__similarity";
        string similarityPlaceholder = "@__min_similarity";

        // Filtering logic, including filter by similarity
        filterSql = filterSql?.Trim().Replace(PostgresSchema.PlaceholdersTags, this._colTags, StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(filterSql))
        {
            filterSql = "TRUE";
        }

        if (sqlUserValues == null) { sqlUserValues = new(); }

        sqlUserValues[similarityPlaceholder] = minSimilarity;

        NpgsqlConnection connection = await this._dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using (connection)
        {
            using NpgsqlCommand cmd = connection.CreateCommand();

#pragma warning disable CA2100 // SQL reviewed
            cmd.CommandText = @$"
                SELECT {columns}, 1 - ({this._colEmbedding} <=> @embedding) AS {similarityActualValue}
                FROM {tableName}
                WHERE {filterSql}
                LIMIT @limit
                OFFSET @offset
            ";

            cmd.Parameters.AddWithValue("@embedding", target);
            cmd.Parameters.AddWithValue("@limit", limit);
            cmd.Parameters.AddWithValue("@offset", offset);

            foreach (KeyValuePair<string, object> kv in sqlUserValues)
            {
                cmd.Parameters.AddWithValue(kv.Key, kv.Value);
            }
#pragma warning restore CA2100

            using NpgsqlDataReader dataReader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            var run = true;
            while (run && await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                double similarity = dataReader.GetDouble(dataReader.GetOrdinal(similarityActualValue));
                if (similarity < minSimilarity)
                {
                    run = false;
                    continue;
                }

                yield return (this.ReadEntry(dataReader, withEmbeddings), similarity);
            }
        }
    }

    /// <summary>
    /// Get a list of records
    /// </summary>
    /// <param name="tableName">Table containing the records to fetch</param>
    /// <param name="filterSql">SQL filter to apply</param>
    /// <param name="sqlUserValues">List of user values passed with placeholders to avoid SQL injection</param>
    /// <param name="orderBySql">SQL to order the records</param>
    /// <param name="limit">Max number of records to retrieve</param>
    /// <param name="offset">Records to skip from the top</param>
    /// <param name="withEmbeddings">Whether to include embedding vectors</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    public async IAsyncEnumerable<PostgresMemoryRecord> GetListAsync(
        string tableName,
        string? filterSql = null,
        Dictionary<string, object>? sqlUserValues = null,
        string? orderBySql = null,
        int limit = 1,
        int offset = 0,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        tableName = this.WithSchemaAndTableNamePrefix(tableName);

        if (limit <= 0) { limit = int.MaxValue; }

        string columns = withEmbeddings ? this._columnsListWithEmbeddings : this._columnsListNoEmbeddings;

        // Filtering logic
        filterSql = filterSql?.Trim().Replace(PostgresSchema.PlaceholdersTags, this._colTags, StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(filterSql))
        {
            filterSql = "TRUE";
        }

        // Custom ordering
        if (string.IsNullOrWhiteSpace(orderBySql))
        {
            orderBySql = this._colId;
        }

        NpgsqlConnection connection = await this._dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using (connection)
        {
            using NpgsqlCommand cmd = connection.CreateCommand();

#pragma warning disable CA2100 // SQL reviewed
            cmd.CommandText = @$"
                SELECT {columns} FROM {tableName}
                WHERE {filterSql}
                ORDER BY {orderBySql}
                LIMIT @limit
                OFFSET @offset
            ";

            cmd.Parameters.AddWithValue("@limit", limit);
            cmd.Parameters.AddWithValue("@offset", offset);

            if (sqlUserValues != null)
            {
                foreach (KeyValuePair<string, object> kv in sqlUserValues)
                {
                    cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                }
            }
#pragma warning restore CA2100

            using NpgsqlDataReader dataReader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return this.ReadEntry(dataReader, withEmbeddings);
            }
        }
    }

    /// <summary>
    /// Delete an entry
    /// </summary>
    /// <param name="tableName">The name assigned to a table of entries</param>
    /// <param name="id">The key of the entry to delete</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    public async Task DeleteAsync(
        string tableName,
        string id,
        CancellationToken cancellationToken = default)
    {
        tableName = this.WithSchemaAndTableNamePrefix(tableName);

        NpgsqlConnection connection = await this._dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using (connection)
        {
            using NpgsqlCommand cmd = connection.CreateCommand();

#pragma warning disable CA2100 // SQL reviewed
            cmd.CommandText = $"DELETE FROM {tableName} WHERE {this._colId}=@id";
            cmd.Parameters.AddWithValue("@id", id);
#pragma warning restore CA2100

            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Npgsql.PostgresException e) when (e.SqlState == "42P01")
            {
                // ignore "undefined table", ie the table hasn't been created yet
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the managed resources
    /// </summary>
    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            (this._dataSource as IDisposable)?.Dispose();
        }
    }

    private PostgresMemoryRecord ReadEntry(NpgsqlDataReader dataReader, bool withEmbeddings)
    {
        string id = dataReader.GetString(dataReader.GetOrdinal(this._colId));
        string content = dataReader.GetString(dataReader.GetOrdinal(this._colContent));
        string payload = dataReader.GetString(dataReader.GetOrdinal(this._colPayload));
        List<string> tags = dataReader.GetFieldValue<List<string>>(dataReader.GetOrdinal(this._colTags));

        Vector embedding = withEmbeddings
            ? dataReader.GetFieldValue<Vector>(dataReader.GetOrdinal(this._colEmbedding))
            : new Vector(new ReadOnlyMemory<float>());

        return new PostgresMemoryRecord
        {
            Id = id,
            Embedding = embedding,
            Tags = tags,
            Content = content,
            Payload = payload
        };
    }

    /// <summary>
    /// Get full table name with schema from table name
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns>Valid table name including schema</returns>
    private string WithSchemaAndTableNamePrefix(string tableName)
    {
        tableName = this.WithTableNamePrefix(tableName);
        PostgresSchema.ValidateTableName(tableName);

        return $"{this._schema}.\"{tableName}\"";
    }

    private string WithTableNamePrefix(string tableName)
    {
        return $"{this._tableNamePrefix}{tableName}";
    }
}
