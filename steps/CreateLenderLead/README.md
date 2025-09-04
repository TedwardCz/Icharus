# Icharus CRM Lead Creation Tool

A .NET 6.0 console application that creates leads in CRM using the CRM SDK.

## Project Overview

This application replicates the functionality to create a Lead in CRM using the CRM SDK approach. It provides both a real implementation using the CRM SDK and a mock implementation for testing purposes.

## Features

- Create a single lead with specified properties
- Create multiple test leads with random data
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
# Run with default environment (UAT)
dotnet run

# Run with specific environment
dotnet run -- --env DEV
dotnet run -- --env UAT
dotnet run -- --env PROD

# Short form for environment parameter
dotnet run -- -e PROD
```

### With Mock Implementation

```
dotnet run -c MockDebug
```

## Lead Properties

The following properties are set when creating a lead:

- First Name
- Last Name
- Address (Line1, City, State, Zip)
- Email
- Phone Numbers (Most Recent, Home, Mobile, Other)
- Owner (Team)

## Implementation Details

### CRM SDK Implementation

The application uses the `CRM.CoreService.Nuget.Standard` package to interact with the CRM system. It creates a lead entity with the required properties and assigns it to the specified team.

### Mock Implementation

For testing purposes, the application includes a mock implementation that simulates lead creation without requiring the CRM SDK. This is useful for development and testing when access to the CRM system is not available.

## Error Handling

The application includes comprehensive error handling for:

- Configuration validation
- Input parameter validation
- CRM SDK exceptions

## Future Enhancements

- Add support for additional lead properties
- Implement lead qualification
- Add unit tests
- Add logging
