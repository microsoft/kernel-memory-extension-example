// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;

namespace FunctionalTests;

public class IndexCreationTests : BaseTestCase
{
    public IndexCreationTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Fact]
    public async Task ItNormalizesIndexNames()
    {
        // Arrange
        string indexNameWithDashes = "name-with-dashes";
        string indexNameWithUnderscores = "name_with_underscore";

        var memory = new KernelMemoryBuilder()
            .WithPostgres(this.PostgresConfiguration)
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile, Directory = "_files" })
            // .WithOpenAI(this.OpenAIConfiguration)
            .WithAzureOpenAITextGeneration(this.AzureOpenAITextConfiguration)
            .WithAzureOpenAITextEmbeddingGeneration(this.AzureOpenAIEmbeddingConfiguration)
            .Build();

        // Act - Assert no exception occurs
        await memory.ImportTextAsync("something", index: indexNameWithDashes);
        await memory.ImportTextAsync("something", index: indexNameWithUnderscores);
    }
}
