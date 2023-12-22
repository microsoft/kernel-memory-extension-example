// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.KernelMemory.Configuration;

namespace Microsoft.KernelMemory.Postgres;

/// <summary>
/// Postgres configuration
/// </summary>
public class PostgresConfig
{
    /// <summary>
    /// Key for the Columns dictionary
    /// </summary>
    public const string ColumnId = "id";

    /// <summary>
    /// Key for the Columns dictionary
    /// </summary>
    public const string ColumnEmbedding = "embedding";

    /// <summary>
    /// Key for the Columns dictionary
    /// </summary>
    public const string ColumnTags = "tags";

    /// <summary>
    /// Key for the Columns dictionary
    /// </summary>
    public const string ColumnContent = "content";

    /// <summary>
    /// Key for the Columns dictionary
    /// </summary>
    public const string ColumnPayload = "payload";

    /// <summary>
    /// Name of the default schema
    /// </summary>
    public const string DefaultSchema = "public";

    /// <summary>
    /// Default prefix used for table names
    /// </summary>
    public const string DefaultTableNamePrefix = "km_";

    /// <summary>
    /// Connection string required to connect to Postgres
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Name of the schema where to read and write records.
    /// </summary>
    public string Schema { get; set; } = DefaultSchema;

    /// <summary>
    /// Mandatory prefix to add to tables created by KM.
    /// This is used to distinguish KM tables from others in the same schema.
    /// </summary>
    public string TableNamePrefix { get; set; } = DefaultTableNamePrefix;

    /// <summary>
    /// Configurable column names used with Postgres
    /// </summary>
    public Dictionary<string, string> Columns { get; set; }

    /// <summary>
    /// Placeholder used in CreateTableSql
    /// </summary>
    public const string SqlPlaceholdersTableName = "%%tableName%%";

    /// <summary>
    /// Placeholder used in CreateTableSql
    /// </summary>
    public const string SqlPlaceholdersVectorSize = "%%vectorSize%%";

    /// <summary>
    /// Optional, custom SQL statements for creating new tables, in case
    /// you need to add custom columns, indexing, etc.
    /// The SQL must contain two placeholders: %%tableName%% and %%vectorSize%%.
    /// You can put the SQL in one line or split it over multiple lines for
    /// readability. Lines are automatically merged with a new line char.
    /// Example:
    ///   BEGIN;
    ///   CREATE TABLE IF NOT EXISTS %%tableName%% (
    ///     id           TEXT NOT NULL PRIMARY KEY,
    ///     embedding    vector(%%vectorSize%%),
    ///     tags         TEXT[] DEFAULT '{}'::TEXT[] NOT NULL,
    ///     content      TEXT DEFAULT '' NOT NULL,
    ///     payload      JSONB DEFAULT '{}'::JSONB NOT NULL,
    ///     some_text    TEXT DEFAULT '',
    ///     last_update  TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
    ///   );
    ///   CREATE INDEX IF NOT EXISTS idx_tags ON %%tableName%% USING GIN(tags);
    ///   COMMIT;
    /// </summary>
    public List<string> CreateTableSql { get; set; } = new();

    /// <summary>
    /// Create a new instance of the configuration
    /// </summary>
    public PostgresConfig()
    {
        this.Columns = new Dictionary<string, string>
        {
            [ColumnId] = "id",
            [ColumnEmbedding] = "embedding",
            [ColumnTags] = "tags",
            [ColumnContent] = "content",
            [ColumnPayload] = "payload"
        };
    }

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate()
    {
        // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        this.TableNamePrefix = this.TableNamePrefix?.Trim() ?? string.Empty;
        this.ConnectionString = this.ConnectionString?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(this.ConnectionString))
        {
            throw new ConfigurationException("The connection string is empty.");
        }

        if (string.IsNullOrWhiteSpace(this.TableNamePrefix))
        {
            throw new ConfigurationException("The table name prefix is empty.");
        }

        if (!this.Columns.TryGetValue(ColumnId, out var columnName))
        {
            throw new ConfigurationException("The name of the Id column is not defined.");
        }

        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ConfigurationException("The name of the Id column is empty.");
        }

        if (!this.Columns.TryGetValue(ColumnEmbedding, out columnName))
        {
            throw new ConfigurationException("The name of the Embedding column is not defined.");
        }

        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ConfigurationException("The name of the Embedding column is empty.");
        }

        if (!this.Columns.TryGetValue(ColumnTags, out columnName))
        {
            throw new ConfigurationException("The name of the Tags column is not defined.");
        }

        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ConfigurationException("The name of the Tags column is empty.");
        }

        if (!this.Columns.TryGetValue(ColumnContent, out columnName))
        {
            throw new ConfigurationException("The name of the Content column is not defined.");
        }

        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ConfigurationException("The name of the Content column is empty.");
        }

        if (!this.Columns.TryGetValue(ColumnPayload, out columnName))
        {
            throw new ConfigurationException("The name of the Payload column is not defined.");
        }

        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ConfigurationException("The name of the Payload column is empty.");
        }

        if (this.CreateTableSql?.Count > 0)
        {
            var sql = string.Join('\n', this.CreateTableSql).Trim();
            if (!sql.Contains(SqlPlaceholdersTableName, StringComparison.Ordinal))
            {
                throw new ConfigurationException(
                    "The custom SQL to create tables is not valid, " +
                    $"it should contain a {SqlPlaceholdersTableName} placeholder.");
            }

            if (!sql.Contains(SqlPlaceholdersVectorSize, StringComparison.Ordinal))
            {
                throw new ConfigurationException(
                    "The custom SQL to create tables is not valid, " +
                    $"it should contain a {SqlPlaceholdersVectorSize} placeholder.");
            }
        }

        this.Columns[ColumnId] = this.Columns[ColumnId].Trim();
        this.Columns[ColumnEmbedding] = this.Columns[ColumnEmbedding].Trim();
        this.Columns[ColumnTags] = this.Columns[ColumnTags].Trim();
        this.Columns[ColumnContent] = this.Columns[ColumnContent].Trim();
        this.Columns[ColumnPayload] = this.Columns[ColumnPayload].Trim();
    }
}
