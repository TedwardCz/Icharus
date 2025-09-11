# PopulateLenderOpportunity

This step is part of the Icharus workflow and is designed to populate data on a CRM Opportunity. It takes an Opportunity GUID as input and uses the CRM Core Service to update the opportunity with additional data.

## Prerequisites

- .NET 6.0 SDK or later
- Access to CRM environment (DEV, UAT, or PROD)
- Valid application token for CRM authentication

## Configuration

The application uses centralized configuration files located at `C:\customApps\Icharus\app.env.config\`:

- `App.config` - Base configuration file with default values
- `App.Debug.config` - DEV environment overrides
- `App.UAT.config` - UAT environment overrides
- `App.PROD.config` - PROD environment overrides

All configuration files are centralized in the `app.env.config` directory and loaded at runtime based on the `--env` parameter.

The application loads configuration at runtime from the centralized files - no rebuilding required for different environments.

### Key Configuration Values

- `LenderApplicationToken` - Authentication token for CRM
- `LenderBusinessUnit` - Business unit (e.g., "CRM")
- `LenderCrmServiceEnvironment` - Target CRM environment
- `LenderLeadAdmin` - Team GUID for lead ownership
- `LenderLoanOfficer` - Loan officer GUID

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
