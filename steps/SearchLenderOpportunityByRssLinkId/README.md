# SearchLenderOpportunityByRssLinkId

A .NET 8.0 console application that searches for lender opportunities in CRM using RSS Link ID.

## Project Overview

This application searches for existing opportunities in the Lender CRM system using an RSS Link ID as the search criteria. It provides both a real implementation using the CRM SDK and a mock implementation for testing purposes.

## Features

- Search for opportunities by RSS Link ID
- Retrieve comprehensive opportunity data including customer information
- Support for both customer display names and GUIDs
- Configuration validation
- Input parameter validation
- Error handling
- Mock implementation for testing without CRM SDK
- Human-readable and JSON output formats

## Requirements

- .NET 8.0 SDK
- Access to the private NuGet repository (nugetgallery.p.vu.local/api/v2) for the CRM SDK package
- CRM environment credentials

## Configuration

The application uses centralized configuration files located at `C:\customApps\Icharus\app.env.config\`:

- `LenderCrmServiceEnvironment`: The CRM environment to connect to (e.g., "UAT")
- `LenderBusinessUnit`: The business unit to use (e.g., "CRM")
- `LenderApplicationToken`: The application token for authentication
- `LenderLeadAdmin`: The GUID of the team that will own the created leads
- `LenderLoanOfficer`: The GUID of the loan officer

### Environment Configuration

The application supports multiple environments (DEV, UAT, PROD) with centralized configuration files:

- `App.config` - Base configuration file with default values
- `App.Debug.config` - DEV environment overrides
- `App.UAT.config` - UAT environment overrides
- `App.PROD.config` - PROD environment overrides

All configuration files are centralized in the `app.env.config` directory and loaded at runtime based on the `--env` parameter.

The application loads configuration at runtime from the centralized files - no rebuilding required for different environments.

## Build Configurations

The project supports the following build configurations:

- **Debug**: Standard debug build with CRM SDK integration
- **MockDebug**: Debug build with mock implementation (no CRM SDK required)
- **Release**: Release build with CRM SDK integration

All configurations use the same centralized configuration files for runtime environment switching.

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
# Run with required RSS Link ID parameter and default environment (UAT)
dotnet run -- --rsslinkid 12345678-1234-1234-1234-123456789012

# Short form for RSS Link ID parameter
dotnet run -- -r 12345678-1234-1234-1234-123456789012

# Run with specific environment
dotnet run -- --env DEV --rsslinkid 12345678-1234-1234-1234-123456789012
dotnet run -- --env UAT --rsslinkid 12345678-1234-1234-1234-123456789012
dotnet run -- --env PROD --rsslinkid 12345678-1234-1234-1234-123456789012

# Short form for both parameters
dotnet run -- -e PROD -r 12345678-1234-1234-1234-123456789012

# JSON output format
dotnet run -- --rsslinkid 12345678-1234-1234-1234-123456789012 --json
```

### With Mock Implementation

```
# Run with mock implementation and required RSS Link ID
dotnet run -c MockDebug -- --rsslinkid 12345678-1234-1234-1234-123456789012

# Short form
dotnet run -c MockDebug -- -r 12345678-1234-1234-1234-123456789012
```

## Opportunity Search

The application searches for existing opportunities in CRM using the RSS Link ID. The RSS Link ID is a required parameter that must be provided when running the application.

The application will:
1. Connect to the CRM environment using the provided configuration
2. Query the opportunity entity filtering by the `vu_rsslinkid` field
3. Retrieve comprehensive opportunity data including customer information
4. Return results in either human-readable or JSON format

## Retrieved Data

The following opportunity data is retrieved:

- **Opportunity ID**: Unique identifier for the opportunity
- **Name**: Opportunity name/title
- **Customer Name**: Display name of the customer (with fallback to GUID)
- **Borrower GUID**: Customer GUID for programmatic use
- **Borrower First Name**: First name from borrower fields
- **Borrower Last Name**: Last name from borrower fields
- **Status Code**: Current opportunity status
- **Estimated Value**: Monetary value of the opportunity
- **Estimated Close Date**: Expected closing date
- **Created On**: Opportunity creation timestamp

## Implementation Details

### CRM SDK Implementation

The application uses the `Crm.CoreService.NuGet.Standard` package to interact with the CRM system. It constructs a QueryExpression to search for opportunities by RSS Link ID and retrieves comprehensive opportunity data including customer display names using FormattedValues.

### Mock Implementation

For testing purposes, the application includes a mock implementation that simulates opportunity search without requiring the CRM SDK. This is useful for development and testing when access to the CRM system is not available. The mock implementation generates realistic test data.

## Output Formats

### Human-Readable Format
```
Found 1 opportunity(s) for RSS Link ID: 12345678-1234-1234-1234-123456789012

Opportunity #1:
  ID: 87654321-4321-4321-4321-210987654321
  Name: Sample Opportunity
  Customer: John Smith
  Borrower GUID: 11111111-2222-3333-4444-555555555555
  Borrower First Name: John
  Borrower Last Name: Smith
  Status: Open
  Estimated Value: $250,000.00
  Estimated Close Date: 2024-03-15
  Created: 2024-01-15 10:30:00
```

### JSON Format
```json
{
  "success": true,
  "rssLinkId": "12345678-1234-1234-1234-123456789012",
  "environment": "UAT",
  "executionTimeMs": 1250,
  "count": 1,
  "opportunities": [
    {
      "opportunityId": "87654321-4321-4321-4321-210987654321",
      "name": "Sample Opportunity",
      "customerName": "John Smith",
      "borrowerGuid": "11111111-2222-3333-4444-555555555555",
      "borrowerFirstName": "John",
      "borrowerLastName": "Smith",
      "statusCode": "Open",
      "estimatedValue": 250000.0,
      "estimatedCloseDate": "2024-03-15",
      "createdOn": "2024-01-15 10:30:00"
    }
  ]
}
```

## Error Handling

The application includes comprehensive error handling for:

- Configuration validation
- Input parameter validation
- CRM SDK exceptions
- Network connectivity issues
- Invalid RSS Link ID format

## Integration with Icharus

This tool follows the standard Icharus step pattern:
- Uses centralized configuration system
- Supports multiple environments (DEV/UAT/PROD)
- Returns success/failure exit codes for orchestration
- Can be chained with other Icharus steps in PowerShell workflows
- Supports both mock and real CRM modes
