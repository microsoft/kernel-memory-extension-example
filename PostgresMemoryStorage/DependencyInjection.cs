// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.Postgres;

/// <summary>
/// Extensions for KernelMemoryBuilder
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Kernel Memory Builder extension method to add Postgres memory connector.
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="config">Postgres configuration</param>
    public static IKernelMemoryBuilder WithPostgres(this IKernelMemoryBuilder builder, PostgresConfig config)
    {
        builder.Services.AddPostgresAsVectorDb(config);
        return builder;
    }

    /// <summary>
    /// Kernel Memory Builder extension method to add Postgres memory connector.
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="connString">Postgres connection string</param>
    public static IKernelMemoryBuilder WithPostgres(this IKernelMemoryBuilder builder, string connString)
    {
        builder.Services.AddPostgresAsVectorDb(connString);
        return builder;
    }
}

/// <summary>
/// Extensions for KernelMemoryBuilder and generic DI
/// </summary>
public static partial class DependencyInjection
{
    /// <summary>
    /// Inject Postgres as the default implementation of IVectorDb
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="config">Postgres configuration</param>
    public static IServiceCollection AddPostgresAsVectorDb(this IServiceCollection services, PostgresConfig config)
    {
        return services
            .AddSingleton<PostgresConfig>(config)
            .AddSingleton<IVectorDb, PostgresMemory>();
    }

    /// <summary>
    /// Inject Postgres as the default implementation of IVectorDb
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connString">Postgres connection string</param>
    public static IServiceCollection AddPostgresAsVectorDb(this IServiceCollection services, string connString)
    {
        var config = new PostgresConfig { ConnString = connString };
        return services.AddPostgresAsVectorDb(config);
    }
}
