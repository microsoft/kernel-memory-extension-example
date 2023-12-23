// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Configuration;

namespace UnitTests;

public class PostgresConfigTests
{
    [Fact]
    public void ItRequiresOnlyAConnStringToBeValid()
    {
        // Arrange
        var config1 = new PostgresConfig();
        var config2 = new PostgresConfig { ConnectionString = "test string" };

        // Act - Assert exception occurs
        Assert.Throws<ConfigurationException>(() => config1.Validate());

        // Act - Assert no exception occurs
        config2.Validate();
    }
}
