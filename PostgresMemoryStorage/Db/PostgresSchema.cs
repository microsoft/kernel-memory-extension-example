// Copyright (c) Microsoft. All rights reserved.

using System.Text.RegularExpressions;

namespace Microsoft.KernelMemory.Postgres.Db;

internal static class PostgresSchema
{
    private static readonly Regex s_schemaNameRegex = new(@"^[a-zA-Z0-9_]+$");
    private static readonly Regex s_tableNameRegex = new(@"^[a-zA-Z0-9_]+$");

    public const string PlaceholdersTags = "{{$tags}}";

    public static void ValidateSchemaName(string name)
    {
        if (s_schemaNameRegex.IsMatch(name)) { return; }

        throw new PostgresException($"The schema name '{name}' contains invalid chars");
    }

    public static void ValidateTableName(string name)
    {
        if (s_tableNameRegex.IsMatch(name)) { return; }

        throw new PostgresException("The table/index name contains invalid chars");
    }

    public static void ValidateTableNamePrefix(string name)
    {
        if (s_tableNameRegex.IsMatch(name)) { return; }

        throw new PostgresException($"The table name prefix '{name}' contains invalid chars");
    }

    public static void ValidateFieldName(string name)
    {
        if (s_tableNameRegex.IsMatch(name)) { return; }

        throw new PostgresException($"The field name '{name}' contains invalid chars");
    }
}
