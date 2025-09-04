# PopulateRealtyOpportunity

This step is part of the Icharus workflow and is designed to populate data on a Realty CRM Opportunity. It takes an Opportunity GUID as input and uses the CRM Core Service to update the opportunity with additional data.

## Prerequisites

- .NET 8.0 SDK or later
- Access to Realty CRM environment (DEV, UAT, or PROD)
- Valid application token for Realty CRM authentication

## Configuration

This tool uses centralized configuration files located at:
```
C:\customApps\Icharus\app.env.config\
```

The configuration is automatically loaded based on the environment parameter:
- DEV environment uses `App.Debug.config`
- UAT environment uses `App.UAT.config`
- PROD environment uses `App.PROD.config`

See `Implementation/ENVIRONMENT_CONFIG.md` for detailed configuration information.

## Building the Tool

To build the tool, navigate to the Implementation directory and run:

```
dotnet build
```

## Running the Tool

To run the tool, use the following command:

```
dotnet run [--env|-e <environment>] --opportunity|-o <opportunityGuid>
```

### Examples:

Run with default environment (UAT):
```
dotnet run --opportunity 12345678-1234-1234-1234-123456789012
```

Run with specific environment:
```
dotnet run --env PROD --opportunity 12345678-1234-1234-1234-123456789012
```

Or using short parameter names:
```
dotnet run -e DEV -o 12345678-1234-1234-1234-123456789012
```

## What This Tool Does

1. Takes an Opportunity GUID as input
2. Connects to the Realty CRM environment using the Core Service
3. Retrieves the opportunity entity from Realty CRM
4. Updates the opportunity with additional Realty-specific data
5. Saves the changes back to Realty CRM

The tool will output the status of the operation and copy the Opportunity GUID to the clipboard for convenience.

## Realty CRM Integration

This tool is specifically designed for Realty CRM integration and uses:
- RSS (Realty Search Solutions) business unit
- Realty-specific field mappings
- Centralized configuration with Realty CRM tokens and GUIDs
