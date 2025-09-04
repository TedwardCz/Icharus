# Icharus Environment Configuration Guide

This document explains how to use the multi-environment configuration system in the Icharus project.

## Available Environments

The Icharus project supports the following environments:

1. **DEV** (Development) - For local development and testing
2. **UAT** (User Acceptance Testing) - For testing in the UAT environment
3. **PROD** (Production) - For production deployment

## Configuration Files

The environment-specific settings are managed through centralized configuration files located at `C:\customApps\Icharus\app.env.config\`:

- `App.config` - Base configuration file with default values
- `App.Debug.config` - DEV environment overrides
- `App.UAT.config` - UAT environment overrides
- `App.PROD.config` - PROD environment overrides

## How to Switch Environments

### Runtime Environment Selection (Recommended)

The application now supports specifying the environment at runtime using command-line arguments. This is the recommended approach as it doesn't require rebuilding the application for different environments.

```powershell
# Run with DEV environment
dotnet run -- --env DEV

# Run with UAT environment (also works with lowercase)
dotnet run -- --env uat

# Run with PROD environment (can also use short form -e)
dotnet run -- -e PROD

# Without specifying an environment (defaults to UAT)
dotnet run
```

### Using Visual Studio

If you're using Visual Studio and prefer the build-time approach:

1. Right-click on the project in Solution Explorer
2. Select "Properties"
3. Go to the "Build" tab
4. Select the desired configuration (Debug, UAT, or PROD)
5. Build and run the project

### Using Command Line with Build Configurations

Alternatively, you can still use build configurations if needed:

```powershell
# For DEV environment
dotnet build -c Debug
dotnet run -c Debug

# For UAT environment
dotnet build -c UAT
dotnet run -c UAT

# For PROD environment
dotnet build -c PROD
dotnet run -c PROD
```

## Default Environment

The default environment is **UAT**. If no specific environment is selected, the application will use the UAT configuration.

## Environment-Specific Settings

Each environment has its own settings in the corresponding config file:

### DEV Environment
- CrmServiceEnvironment: DEV
- Other settings are the same as UAT for now

### UAT Environment
- CrmServiceEnvironment: UAT
- BusinessUnit: CRM
- ApplicationToken: ee37e594-32bd-4fbe-baf4-2b5b3a75c576
- _vuhlLeadAdmin: ec4fb50d-ec7e-e811-8183-e0071b6ac101

### PROD Environment
- CrmServiceEnvironment: PROD
- Other settings are the same as UAT for now

## Customizing Environment Settings

To modify settings for a specific environment:

1. Open the corresponding config file (e.g., `App.PROD.config`)
2. Update the values as needed
3. Save the file
4. Rebuild the project for that environment

## How Runtime Configuration Loading Works

The application loads configuration at runtime based on the `--env` parameter:

1. Loads base settings from the centralized `App.config`
2. Applies environment-specific overrides from the corresponding config file (e.g., `App.PROD.config`)
3. No build-time transformations or rebuilding required

Configuration files use standard XML format with `<add key="..." value="..." />` elements.

## Adding New Configuration Settings

To add a new setting to all environments:

1. Add the setting to the base `App.config` file in the `app.env.config` directory
2. Add the setting with environment-specific values to each environment config file in the same directory
3. No project rebuilding required - changes take effect immediately
