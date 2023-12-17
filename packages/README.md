# Build

You can use this folder to place local builds of your `*.nupkg` files.

Use the `build.sh` script to populate this folder.

[nuget.config](../nuget.config) is configured to look for packages here:

```xml
<?xml version="1.0" encoding="utf-8"?>

<configuration>

  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="Local" value="packages" />
  </packageSources>

  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    
    <packageSource key="Local">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>

</configuration>
```

# Test

The repository contains a [TestApplication](../TestApplication/) that uses
local nuget packages, so you can test them before publishing.

# Release

Note that local packages are not signed, and if you want to publish signed
binaries you shold set up a separate build process using your private keys.

# Troubleshooting

If local testing is not picking up your build, bear in mind that nuget keeps
several caches, where there might be a previous build with the same version
number.

To see the where dotnet caches packages:

    dotnet nuget locals global-packages -l

This command should delete the cached builds, which can be useful if you
are rebuilding with the same version number:

    rm -fR ~/.nuget/packages/<PACKAGE ID>/
