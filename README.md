# Kernel Memory with Postgres

[//]: # ([![Nuget package]&#40;https://img.shields.io/nuget/vpre/Microsoft.KernelMemory.Postgres&#41;]&#40;https://www.nuget.org/packages/Microsoft.KernelMemory.Postgres/&#41;)
[![License: MIT](https://img.shields.io/github/license/microsoft/kernel-memory)](https://github.com/microsoft/kernel-memory/blob/main/LICENSE)
[![Discord](https://img.shields.io/discord/1063152441819942922?label=Discord&logo=discord&logoColor=white&color=d82679)](https://aka.ms/SKDiscord)

**[Kernel Memory](https://github.com/microsoft/semantic-memory)** (KM)
is an open-source service and plugin specialized in the efficient indexing of datasets
through custom continuous data hybrid pipelines.

This repository contains the Postgres adapter allowing to use Kernel Memory with Postgres.

To use Postgres with Kernel Memory:

1. Verify your Postgres instance supports vectors, e.g. run `SELECT * FROM pg_extension`

[//]: # (2. install the [Microsoft.KernelMemory.Postgres]&#40;https://www.nuget.org/packages/Microsoft.KernelMemory.Postgres&#41; package)

2. add to appsettings.json (or appsettings.development.json) Postgres connection string, for example:

    ```json
    {
      "KernelMemory": {
        "Services": {
          "Postgres": {
            "ConnectionString": "Host=localhost;Port=5432;Username=myuser;Password=mypassword"
          }
        }
      }
    }
    ```
3. configure KM builder to store memories in Postgres, for example:
    ```csharp
    // using Microsoft.KernelMemory;
    // using Microsoft.KernelMemory.Postgres;
    // using Microsoft.Extensions.Configuration;

    var postgresConfig = new PostgresConfig();

    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile("appsettings.Development.json", optional: true)
        .Build()
        .BindSection("KernelMemory:Services:Postgres", postgresConfig);

    var memory = new KernelMemoryBuilder()
        .WithPostgres(postgresConfig)
        .WithAzureOpenAITextGeneration(azureOpenAIConfig)
        .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIConfig)
        .Build();
    ```

## Neighbor search indexes, quality and performance

The connector does not create IVFFlat or HNSW indexes on Postgres tables, and
uses exact nearest neighbor search.

Depending on your scenario you might want to create these indexes manually,
considering precision and performance trade-offs, or you can customize the
SQL used to create tables via configuration.

> An **IVFFlat** index divides vectors into lists, and then searches a subset
> of those lists that are closest to the query vector. It has **faster build times**
> and uses **less memory** than HNSW, but has **lower query performance**
> (in terms of speed-recall tradeoff).

> An **HNSW** index creates a multilayer graph. It has **slower build times**
> and uses **more memory** than IVFFlat, but has **better query performance**
> (in terms of speed-recall tradeoff). There’s no training step like IVFFlat,
> so the index can be created without any data in the table.

See https://github.com/pgvector/pgvector for more information.

## Memory Indexes and Postgres tables

The Postgres memory connector will create "memory indexes" automatically, one
DB table for each memory index.

Table names have a configurable **prefix**, used to filter out other tables that
might be present. The prefix is mandatory, cannot be empty, we suggest using
the default `km_` prefix.

Overall we recommend not mixing external tables in the same DB used for
Kernel Memory.

## Column names and table schema

The connector uses a default schema with predefined columns and indexes.

You can change the field names, and if you need to add additional columns
or indexes, you can also customize the `CREATE TABLE` SQL statement. You
can use this approach, for example, to use IVFFlat or HNSW.

See `PostgresConfig` class for details.