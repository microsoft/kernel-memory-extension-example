// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Postgres;

namespace TestApplication;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var postgresConfig = new PostgresConfig();
        var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();
        var azureOpenAITextConfig = new AzureOpenAIConfig();

        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .BindSection("KernelMemory:Services:Postgres", postgresConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig);

        // Concatenate our 'WithPostgres()' after 'WithOpenAIDefaults()' from the core nuget
        var mem1 = new KernelMemoryBuilder()
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
            .WithPostgres(postgresConfig)
            .Build();

        // Concatenate our 'WithPostgres()' before 'WithOpenAIDefaults()' from the core nuget
        var mem2 = new KernelMemoryBuilder()
            .WithPostgres(postgresConfig)
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
            .Build();

        // Concatenate our 'WithPostgres()' before and after KM builder extension methods from the core nuget
        var mem3 = new KernelMemoryBuilder()
            .WithSimpleFileStorage()
            .WithAzureOpenAITextGeneration(azureOpenAITextConfig)
            .WithPostgres(postgresConfig)
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
            .Build();

        await mem1.DeleteIndexAsync("index1");
        await mem2.DeleteIndexAsync("index2");
        await mem3.DeleteIndexAsync("index3");

        var doc1 = await mem1.ImportTextAsync("this is a test 1", index: "index1");
        var doc2 = await mem2.ImportTextAsync("this is a test 2", index: "index2");
        var doc3 = await mem3.ImportTextAsync("this is a test 3", index: "index3");

        Console.WriteLine("\nInsert done. Press ENTER to list indexes...");
        Console.ReadLine();

        foreach (var s in await mem1.ListIndexesAsync())
        {
            Console.WriteLine(s.Name);
        }

        Console.WriteLine("\nDelete done. Press ENTER to delete indexes...");
        Console.ReadLine();

        await mem1.DeleteIndexAsync("index1");
        await mem2.DeleteIndexAsync("index2");
        await mem3.DeleteIndexAsync("index3");

        Console.WriteLine("\n=== end ===");
    }
}
