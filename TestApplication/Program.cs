// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Postgres;

namespace TestApplication;

internal class Program
{
    public static void Main(string[] args)
    {
        // Concatenate our 'WithPostgres()' after 'WithOpenAIDefaults()' from the core nuget
        var test1 = new KernelMemoryBuilder()
            .WithOpenAIDefaults("api key")
            .WithPostgres("conn string")
            .Build();

        // Concatenate our 'WithPostgres()' before 'WithOpenAIDefaults()' from the core nuget
        var test2 = new KernelMemoryBuilder()
            .WithPostgres("conn string")
            .WithOpenAIDefaults("api key")
            .Build();

        // Concatenate our 'WithPostgres()' before and after KM builder extension methods from the core nuget
        var test3 = new KernelMemoryBuilder()
            .WithSimpleFileStorage()
            .WithPostgres("conn string")
            .WithOpenAIDefaults("api key")
            .Build();

        Console.WriteLine("Test complete");
    }
}
