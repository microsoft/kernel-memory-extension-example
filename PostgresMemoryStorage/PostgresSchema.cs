// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.Postgres;

internal static class PostgresSchema
{
    private static readonly Regex s_schemaNameRegex = new(@"^[a-zA-Z0-9_]+$");
    private static readonly Regex s_tableNameRegex = new(@"^[a-zA-Z0-9_]+$");

    // TODO: make these configurable
    public const string FieldsId = "id";
    public const string FieldsEmbedding = "embedding";
    public const string FieldsTags = "tags";
    public const string FieldsContent = "content";
    public const string FieldsPayload = "payload";
    public const string FieldsUpdatedAt = "last_update";

    /// <summary>
    /// This is used to filter the list of tables when retrieving the list.
    /// Only tables with this comment are considered Indexes.
    /// TODO: allow to turn off/customize the filtering logic.
    /// </summary>
    public const string TableComment = "KernelMemoryIndex";

    /// <summary>
    /// Copy payload from MemoryRecord, excluding the content, which is stored separately.
    /// </summary>
    /// <param name="record">Source record to copy from</param>
    /// <returns>New dictionary with all the payload, except for content</returns>
    public static Dictionary<string, object> GetPayload(MemoryRecord? record)
    {
        if (record == null)
        {
            return new Dictionary<string, object>();
        }

        return record.Payload.Where(kv => kv.Key != Constants.ReservedPayloadTextField).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Extract content from MemoryRecord
    /// </summary>
    /// <param name="record">Source record to extract from</param>
    /// <returns>Text content, or empty string if none found</returns>
    public static string GetContent(MemoryRecord? record)
    {
        if (record == null)
        {
            return string.Empty;
        }

        if (record.Payload.TryGetValue(Constants.ReservedPayloadTextField, out object? value))
        {
            return (string)value;
        }

        return string.Empty;
    }

    public static string[] GetTags(MemoryRecord? record)
    {
        if (record == null)
        {
            return Array.Empty<string>();
        }

        return record.Tags.Pairs.Select(tag => $"{tag.Key}{Constants.ReservedEqualsChar}{tag.Value}").ToArray();
    }

    public static void ValidateSchemaName(string name)
    {
        if (s_schemaNameRegex.IsMatch(name)) { return; }

        throw new PostgresException("The schema name contains invalid chars");
    }

    public static void ValidateTableName(string name)
    {
        if (s_tableNameRegex.IsMatch(name)) { return; }

        throw new PostgresException("The table/index name contains invalid chars");
    }
}
