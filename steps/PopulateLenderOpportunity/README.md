# PopulateLenderOpportunity

This step is part of the Icharus workflow and is designed to populate data on a CRM Opportunity. It takes an Opportunity GUID as input and uses the CRM Core Service to update the opportunity with additional data.

## Prerequisites

- .NET 6.0 SDK or later
- Access to CRM environment (DEV, UAT, or PROD)
- Valid application token for CRM authentication

## Configuration

Before using this tool, you need to configure the appropriate environment settings:

1. Open the relevant configuration file:
   - `Implementation/App.Debug.config` for DEV environment
   - `Implementation/App.UAT.config` for UAT environment
   - `Implementation/App.PROD.config` for PROD environment

2. Update the `ApplicationToken` value with a valid token for the corresponding environment.

See `Implementation/ENVIRONMENT_CONFIG.md` for more details on configuration.

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
2. Connects to the CRM environment using the Core Service
3. Retrieves the opportunity entity from CRM
4. Updates the opportunity with additional data
5. Saves the changes back to CRM

The tool will output the status of the operation and copy the Opportunity GUID to the clipboard for convenience.
