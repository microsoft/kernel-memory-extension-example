// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.Postgres;

/// <summary>
/// Postgres configuration
/// </summary>
public class PostgresConfig
{
    /// <summary>
    /// Connection string required to connect to Postgres
    /// </summary>
    public string ConnString { get; set; } = string.Empty;
}
