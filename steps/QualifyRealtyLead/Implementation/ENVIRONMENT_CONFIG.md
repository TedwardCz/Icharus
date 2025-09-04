# Icharus Realty Environment Configuration Guide

This document explains how to use the multi-environment configuration system for the Realty Lead Qualification tool.

## Available Environments

The QualifyRealtyLead project supports the following environments:

1. **DEV** (Development) - For local development and testing
2. **UAT** (User Acceptance Testing) - For testing in the UAT environment
3. **PROD** (Production) - For production deployment

## Configuration Files

The environment-specific settings are managed through centralized configuration files located at `C:\customApps\Icharus\app.env.config\`:

- `App.config` - Base configuration file
- `App.Debug.config` - DEV environment settings (maps to DEV)
- `App.UAT.config` - UAT environment settings
- `App.PROD.config` - PROD environment settings

## How to Switch Environments

### Runtime Environment Selection (Recommended)

The application supports specifying the environment at runtime using command-line arguments. This is the recommended approach as it doesn't require rebuilding the application for different environments.

```powershell
# Run with DEV environment
dotnet run -- --env DEV --lead 12345678-1234-1234-1234-123456789012

# Run with UAT environment (also works with lowercase)
dotnet run -- --env uat --lead 12345678-1234-1234-1234-123456789012

# Run with PROD environment (can also use short form -e)
dotnet run -- -e PROD --lead 12345678-1234-1234-1234-123456789012

# Without specifying an environment (defaults to UAT)
dotnet run -- --lead 12345678-1234-1234-1234-123456789012
```

### Using Visual Studio

If you're using Visual Studio:

1. Right-click on the project in Solution Explorer
2. Select "Properties"
3. Go to the "Debug" tab
4. Set command line arguments: `--env DEV --lead 12345678-1234-1234-1234-123456789012`
5. Build and run the project

## Default Environment

The default environment is **UAT**. If no specific environment is selected, the application will use the UAT configuration.

## Environment-Specific Settings

Each environment has its own settings in the corresponding centralized config file:

### DEV Environment (App.Debug.config)
- RealtyCrmServiceEnvironment: DEV
- RealtyBusinessUnit: RSS
- RealtyApplicationToken: e8686d45-b882-4f31-92d1-b0312877d0b7
- RealtyLeadAdmin: c5f10e5c-694a-e911-a831-000d3a1a274b
- RealtyLoanOfficer: c5f10e5c-694a-e911-a831-000d3a1a274b

### UAT Environment (App.UAT.config)
- RealtyCrmServiceEnvironment: UAT
- RealtyBusinessUnit: RSS
- RealtyApplicationToken: e8686d45-b882-4f31-92d1-b0312877d0b7
- RealtyLeadAdmin: c5f10e5c-694a-e911-a831-000d3a1a274b
- RealtyLoanOfficer: c5f10e5c-694a-e911-a831-000d3a1a274b

### PROD Environment (App.PROD.config)
- RealtyCrmServiceEnvironment: PROD
- RealtyBusinessUnit: RSS
- RealtyApplicationToken: e8686d45-b882-4f31-92d1-b0312877d0b7
- RealtyLeadAdmin: c5f10e5c-694a-e911-a831-000d3a1a274b
- RealtyLoanOfficer: c5f10e5c-694a-e911-a831-000d3a1a274b

## Customizing Environment Settings

To modify settings for a specific environment:

1. Open the corresponding centralized config file (e.g., `C:\customApps\Icharus\app.env.config\App.PROD.config`)
2. Update the Realty-specific values as needed
3. Save the file
4. Run the application with the desired environment

## Configuration Loading

The application uses runtime configuration loading that:
1. Loads base settings from the local App.config
2. Overrides with environment-specific settings from the centralized config files
3. Maps DEV environment to use App.Debug.config for consistency

## Adding New Configuration Settings

To add a new Realty-specific setting to all environments:

1. Add the setting to the base `App.config` file with a default value
2. Add the setting with environment-specific values to each centralized environment config file
3. Update the Program.cs configuration loading logic if needed
