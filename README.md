# Simpel Bogføring - .NET Console Application

This is a simple accounting application (bogføringsprogram) built as a production-ready .NET console application template with modern C# patterns and best practices.

## Application Overview

This application simulates a basic accounting system with the following features:
- Account management (kontoplan)
- Transaction recording (posteringer)
- Period-based accounting (regnskabsperioder)
- VAT handling (momsberegning)

## Data Structure

The application uses a simple file-based data structure in the `/data` folder:

### `regnskab.json`
Contains main accounting period information:
- Company name (regnskabsNavn)
- Period dates (periodeFra/periodeTil)
- VAT amounts (tilgodehavendeMoms/skyldigMoms)

### `kontoplan.csv`
Chart of accounts with:
- Account number (nr)
- Account name (navn)
- Account type (type): drift, status
- VAT code (moms): U25, INGEN

### `posteringer-{month}-{year}.csv`
Monthly transaction files with:
- Transaction date (dato)
- Account number (konto)
- Description (tekst)
- Amount (beløb)

## Architecture Overview

This template demonstrates a clean, maintainable architecture using:

- **Dependency Injection**: Microsoft.Extensions.DependencyInjection for IoC container
- **Configuration Management**: Hierarchical configuration with JSON files and environment variables
- **Structured Logging**: Serilog with console and file sinks
- **Command-Line Parsing**: CommandLineParser for robust argument handling
- **Environment Support**: Separate configurations for Development/Production environments
- **Code Analysis**: AsyncFixer and Meziantou.Analyzer for code quality

## Project Structure

```
├── Program.cs              # Application entry point with DI setup
├── App.cs                  # Main application logic
├── AppSettings.cs          # Configuration model
├── CommandArgs.cs          # Command-line arguments model
├── DemoService.cs          # Example service with DI
├── appsettings.json        # Default configuration
├── appsettings.Development.json # Development overrides
├── .env                    # Environment variables
├── .vscode/
│   ├── launch.json        # Debug configurations
│   └── tasks.json         # Build tasks
└── logs/                  # Generated log files
```

## Building the Application

### Debug Build
```bash
dotnet build
```

### Release Build
```bash
dotnet build --configuration Release
```

### Clean Build
```bash
dotnet clean
dotnet build
```

## Running the Application

### Basic Run
```bash
dotnet run
```

### With Verbose Output
```bash
dotnet run -- --verbose
```

### Run Specific Configuration
```bash
# Development
dotnet run --configuration Debug -- --verbose

# Production
dotnet run --configuration Release
```

## Command-Line Arguments

- `--verbose` or `-v`: Enable verbose output and detailed logging

Example:
```bash
dotnet run -- --verbose
```

## Configuration

Configuration follows a hierarchical approach with the following precedence (highest to lowest):

1. **Environment Variables** (highest priority)
2. **appsettings.{Environment}.json**
3. **appsettings.json**
4. **.env file** (loaded at startup)

### Configuration Sections

#### AppSettings
- `ApplicationTitle`: The display name of the application

### Environment Variable Override Pattern

You can override any configuration value using environment variables with the following pattern:

```bash
# Override AppSettings:ApplicationTitle
set AppSettings__ApplicationTitle=My Custom App Title

# Or in .env file
AppSettings__ApplicationTitle=My Custom App Title
```

### Environment-Specific Configuration

Set the `DOTNET_ENVIRONMENT` variable to control which appsettings file is loaded:

```bash
# Development (uses appsettings.Development.json)
set DOTNET_ENVIRONMENT=Development

# Production (uses appsettings.json only)
set DOTNET_ENVIRONMENT=Production
```

## Logging

The application uses Serilog for structured logging with the following features:

### Log Levels
- **Debug**: Detailed diagnostic information (Development mode)
- **Information**: General application flow
- **Warning**: Potentially harmful situations
- **Error**: Error events that don't stop execution
- **Fatal**: Critical errors that may cause termination

### Log Output

#### Console Logging
All log levels are written to the console with timestamps and structured formatting.

#### File Logging
- **Production**: `logs/app-{Date}.log` (7 days retention)
- **Development**: `logs/app-dev-{Date}.log` (3 days retention)

#### Log Format
```
[HH:mm:ss INF] SourceContext: Message content
[yyyy-MM-dd HH:mm:ss.fff zzz INF] SourceContext: Message content (file logs)
```

### Verbose Mode
When `--verbose` flag is used:
- Console shows Debug level messages
- More detailed information is logged
- Service operations are traced

## Development

### VS Code Debugging

Four debug configurations are available:

1. **Debug (Console)**: Debug build with verbose output in VS Code terminal
2. **Debug (External Terminal)**: Debug build in external terminal window
3. **Release (Console)**: Release build in VS Code terminal
4. **Release (External Terminal)**: Release build in external terminal

### Code Quality

The project includes code analyzers:

- **AsyncFixer**: Ensures proper async/await patterns
- **Meziantou.Analyzer**: General code quality improvements

## Customization

This is a template designed to be customized for your specific needs:

1. **Replace Demo Services**: Remove `DemoService` and add your business logic
2. **Extend Configuration**: Add new sections to `AppSettings.cs` and configuration files
3. **Add Commands**: Extend `CommandArgs.cs` with additional command-line options
4. **Implement Features**: Use the established patterns for dependency injection and logging

## Dependencies

- Microsoft.Extensions.Hosting (9.x)
- Microsoft.Extensions.Configuration.* (9.x)
- Serilog.Extensions.Hosting (9.x)
- Serilog.Sinks.Console (6.x)
- Serilog.Sinks.File (7.x)
- CommandLineParser (2.x)
- DotNetEnv (3.x)
- AsyncFixer (1.6.x)
- Meziantou.Analyzer (2.x)

## License

This template is provided as-is for educational and development purposes.