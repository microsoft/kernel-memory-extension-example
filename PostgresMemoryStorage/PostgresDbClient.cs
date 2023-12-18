// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Pgvector;

namespace Microsoft.KernelMemory.Postgres;

/// <summary>
/// An implementation of a client for Postgres. This class is used to managing postgres database operations.
/// </summary>
internal sealed class PostgresDbClient : IDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    private readonly string _schema;
    private readonly string _tableNamePrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresDbClient"/> class.
    /// </summary>
    /// <param name="config">Configuration</param>
    public PostgresDbClient(PostgresConfig config)
    {
        NpgsqlDataSourceBuilder dataSourceBuilder = new(config.ConnectionString);
        dataSourceBuilder.UseVector();
        this._dataSource = dataSourceBuilder.Build();
        this._schema = config.Schema;
        this._tableNamePrefix = config.TableNamePrefix;

        PostgresSchema.ValidateSchemaName(this._schema);
        PostgresSchema.ValidateTableNamePrefix(this._tableNamePrefix);
    }

    /// <summary>
    /// Check if a table exists.
    /// </summary>
    /// <param name="tableName">The name assigned to a table of entries.</param>
    /// <param name="cancellationToken">Async task cancellation token.</param>
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
    /// <param name="tableName">The name assigned to a table of entries.</param>
    /// <param name="vectorSize">Embedding vectors dimension</param>
    /// <param name="cancellationToken">Async task cancellation token.</param>
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
            cmd.CommandText = $@"
                CREATE TABLE IF NOT EXISTS {tableName} (
                   {PostgresSchema.FieldsId} TEXT NOT NULL PRIMARY KEY,
                   {PostgresSchema.FieldsEmbedding} vector({vectorSize}),
                   {PostgresSchema.FieldsTags} TEXT[] DEFAULT '{{}}'::TEXT[] NOT NULL,
                   {PostgresSchema.FieldsContent} TEXT DEFAULT '' NOT NULL,
                   {PostgresSchema.FieldsPayload} JSONB DEFAULT '{{}}'::JSONB NOT NULL,
                   {PostgresSchema.FieldsUpdatedAt} TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
                );
            ";
#pragma warning restore CA2100

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Get all tables.
    /// </summary>
    /// <param name="cancellationToken">Async task cancellation token.</param>
    /// <returns>A group of tables.</returns>
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
    /// <param name="cancellationToken">Async task cancellation token.</param>
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
    /// <param name="tableName">The name assigned to a table of entries.</param>
    /// <param name="id">The key of the entry to upsert.</param>
    /// <param name="embedding">The embedding of the entry.</param>
    /// <param name="tags">Optional labels attached to the record, for filtering purposes.</param>
    /// <param name="content">Text content used to calculate the embedding</param>
    /// <param name="payload">The metadata of the entry.</param>
    /// <param name="lastUpdate">The timestamp of the entry.</param>
    /// <param name="cancellationToken">Async task cancellation token.</param>
    public async Task UpsertAsync(
        string tableName,
        string id,
        Vector embedding,
        string[]? tags,
        string? content,
        string? payload,
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
                    ({PostgresSchema.FieldsId},{PostgresSchema.FieldsEmbedding},{PostgresSchema.FieldsTags},
                     {PostgresSchema.FieldsContent},{PostgresSchema.FieldsPayload},{PostgresSchema.FieldsUpdatedAt})
                    VALUES
                    (@id, @embedding, @tags,
                     @content, @payload, @timestamp)
                ON CONFLICT ({PostgresSchema.FieldsId})
                DO UPDATE SET
                    {PostgresSchema.FieldsEmbedding} = @embedding,
                    {PostgresSchema.FieldsTags}      = @tags,
                    {PostgresSchema.FieldsContent}   = @content,
                    {PostgresSchema.FieldsPayload}   = @payload,
                    {PostgresSchema.FieldsUpdatedAt} = @timestamp
            ";

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@embedding", embedding);
            cmd.Parameters.AddWithValue("@tags", NpgsqlDbType.Array | NpgsqlDbType.Text, tags ?? emptyTags);
            cmd.Parameters.AddWithValue("@content", NpgsqlDbType.Text, content ?? EmptyContent);
            cmd.Parameters.AddWithValue("@payload", NpgsqlDbType.Jsonb, payload ?? EmptyPayload);
            cmd.Parameters.AddWithValue("@timestamp", NpgsqlDbType.TimestampTz, lastUpdate ?? DateTimeOffset.UtcNow);
#pragma warning restore CA2100

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Delete an entry.
    /// </summary>
    /// <param name="tableName">The name assigned to a table of entries.</param>
    /// <param name="id">The key of the entry to delete.</param>
    /// <param name="cancellationToken">Async task cancellation token.</param>
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
            cmd.CommandText = $"DELETE FROM {tableName} WHERE {PostgresSchema.FieldsId}=@id";
            cmd.Parameters.AddWithValue("@id", id);
#pragma warning restore CA2100

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
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
    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            (this._dataSource as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Get full table name with schema from table name.
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
