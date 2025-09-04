# Icharus CRM Lead Qualification Tool

A .NET 6.0 console application that qualifies leads in CRM using the CRM SDK.

## Project Overview

This application replicates the functionality to qualify a Lead in CRM using the CRM SDK approach. It provides both a real implementation using the CRM SDK and a mock implementation for testing purposes.

## Features

- Qualify a single lead with specified properties
- Qualify multiple test leads with random data
- Configuration validation
- Input parameter validation
- Error handling
- Mock implementation for testing without CRM SDK

## Requirements

- .NET 6.0 SDK
- Access to the private NuGet repository (nugetgallery.p.vu.local/api/v2) for the CRM SDK package
- CRM environment credentials

## Configuration

The application uses the following configuration values from App.config:

- `CrmServiceEnvironment`: The CRM environment to connect to (e.g., "UAT")
- `BusinessUnit`: The business unit to use (e.g., "CRM")
- `ApplicationToken`: The application token for authentication
- `_vuhlLeadAdmin`: The GUID of the team that will own the created leads

### Environment Configuration

The application supports multiple environments (DEV, UAT, PROD) with environment-specific configuration files:

- `App.config` - Base configuration file
- `App.Debug.config` - DEV environment settings
- `App.UAT.config` - UAT environment settings
- `App.PROD.config` - PROD environment settings

See the `ENVIRONMENT_CONFIG.md` file in the Implementation directory for detailed instructions on environment configuration.

## Build Configurations

The project supports the following build configurations:

- **Debug**: Standard debug build with CRM SDK integration
- **MockDebug**: Debug build with mock implementation (no CRM SDK required)
- **Release**: Release build with CRM SDK integration

## Building the Project

### With CRM SDK (requires access to private NuGet repository)

```
dotnet build
```

### With Mock Implementation (no CRM SDK required)

```
dotnet build -c MockDebug
```

## Running the Application

### With CRM SDK

```
# Run with required lead GUID parameter and default environment (UAT)
dotnet run -- --lead 12345678-1234-1234-1234-123456789012

# Short form for lead GUID parameter
dotnet run -- -l 12345678-1234-1234-1234-123456789012

# Run with specific environment
dotnet run -- --env DEV --lead 12345678-1234-1234-1234-123456789012
dotnet run -- --env UAT --lead 12345678-1234-1234-1234-123456789012
dotnet run -- --env PROD --lead 12345678-1234-1234-1234-123456789012

# Short form for both parameters
dotnet run -- -e PROD -l 12345678-1234-1234-1234-123456789012
```

### With Mock Implementation

```
# Run with mock implementation and required lead GUID
dotnet run -c MockDebug -- --lead 12345678-1234-1234-1234-123456789012

# Short form
dotnet run -c MockDebug -- -l 12345678-1234-1234-1234-123456789012
```

## Lead Qualification

The application qualifies an existing lead in CRM using its GUID. The lead GUID is a required parameter that must be provided when running the application.

The application will:
1. Locate the lead in CRM using the provided GUID
2. Update the lead's status to qualified
3. Return the result ID (which could be the lead ID or an opportunity ID in a full implementation)

## Implementation Details

### CRM SDK Implementation

The application uses the `CRM.CoreService.Nuget.Standard` package to interact with the CRM system. It locates an existing lead by its GUID and updates its status to qualified.

### Mock Implementation

For testing purposes, the application includes a mock implementation that simulates lead qualification without requiring the CRM SDK. This is useful for development and testing when access to the CRM system is not available. The mock implementation will accept any valid GUID and return a new GUID as the result ID.

## Error Handling

The application includes comprehensive error handling for:

- Configuration validation
- Input parameter validation
- CRM SDK exceptions

## Future Enhancements

- Add support for additional lead properties
- Implement additional qualification criteria
- Add unit tests
- Add logging
