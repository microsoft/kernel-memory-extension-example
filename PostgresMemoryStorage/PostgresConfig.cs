// Copyright (c) Microsoft. All rights reserved.

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
    public string ConnString { get; set; } = string.Empty;

    /// <summary>
    /// Name of the schema where to read and write records.
    /// </summary>
    public string Schema { get; set; } = DefaultSchema;
}
