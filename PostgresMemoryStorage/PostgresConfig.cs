// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Configuration;

namespace Microsoft.KernelMemory.Postgres;

/// <summary>
/// Postgres configuration
/// </summary>
public class PostgresConfig
{
    /// <summary>
    /// Name of the default schema
    /// </summary>
    public const string DefaultSchema = "public";

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
    public string TableNamePrefix { get; set; } = string.Empty;

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
    }
}
