# Agent Instructions for .NET Console Application

## Build Commands
```bash
dotnet clean
dotnet build
dotnet build --configuration Release
```

## Run Commands
```bash
dotnet run
dotnet run -- --verbose
dotnet run --configuration Debug -- --verbose
dotnet run --configuration Release
```

## Test Verification Steps
1. Check build succeeds: `dotnet build`
2. Verify console output: `dotnet run -- --verbose`
3. Check log file creation: `dir logs`
4. Test environment override: `set AppSettings__ApplicationTitle=TestApp && dotnet run`
5. Verify configuration loading: Check logs for configuration values