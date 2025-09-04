# Environment Configuration

This document explains how to configure the PopulateLenderOpportunity tool for different environments.

## Configuration Files

The application uses the following configuration files:

- `App.config`: Base configuration file with default settings
- `App.Debug.config`: Configuration for DEV environment
- `App.UAT.config`: Configuration for UAT environment
- `App.PROD.config`: Configuration for PROD environment

## Required Settings

Each configuration file must contain the following settings:

- `ApplicationToken`: The authentication token for the CRM API
- `BusinessUnit`: The business unit to use (e.g., VUHL, VUR, CRM)
- `CrmServiceEnvironment`: The environment to use (DEV, UAT, PROD)

## Environment Selection

The environment can be specified using the `--env` or `-e` command-line argument:

```
dotnet run --env DEV
dotnet run -e UAT
dotnet run -e PROD
```

If no environment is specified, the default environment (UAT) will be used.

## Token Configuration

Before using the tool, you must configure the appropriate ApplicationToken values in each environment-specific configuration file:

1. Open the relevant configuration file (e.g., `App.Debug.config`, `App.UAT.config`, or `App.PROD.config`)
2. Replace the placeholder token value with the actual token for that environment
3. Save the file

**Note**: Do not commit configuration files with actual token values to source control.
