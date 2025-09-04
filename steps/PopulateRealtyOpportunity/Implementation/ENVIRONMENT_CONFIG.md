# Environment Configuration for PopulateRealtyOpportunity

This document explains how to configure the PopulateRealtyOpportunity tool for different environments (DEV, UAT, PROD).

## Configuration Approach

The PopulateRealtyOpportunity tool uses a centralized configuration approach where all environment-specific settings are stored in shared configuration files located at:

```
C:\customApps\Icharus\app.env.config\
```

## Configuration Files

The following configuration files are used:

- `App.Debug.config` - Used for DEV environment
- `App.UAT.config` - Used for UAT environment  
- `App.PROD.config` - Used for PROD environment

## Environment Switching

The tool automatically loads the appropriate configuration based on the `--env` parameter:

- `--env DEV` → Loads `App.Debug.config`
- `--env UAT` → Loads `App.UAT.config`
- `--env PROD` → Loads `App.PROD.config`

If no environment is specified, UAT is used as the default.

## Required Configuration Keys

The following configuration keys must be present in each environment config file:

### Realty CRM Configuration
- `RealtyApplicationToken` - Application token for Realty CRM authentication
- `RealtyBusinessUnit` - Business unit (should be "RSS" for Realty Search Solutions)
- `RealtyCrmServiceEnvironment` - CRM service environment identifier
- `RealtyLeadAdmin` - GUID for Realty lead admin user (legacy, not actively used)

## Configuration Values

All Realty CRM environments use:
- **Business Unit**: `RSS` (Realty Search Solutions)
- **Application Token**: `e8686d45-b882-4f31-92d1-b0312877d0b7` (valid across all environments)
- **User GUID**: `c5f10e5c-694a-e911-a831-000d3a1a274b` (same across all environments)

## Usage Examples

```bash
# Run with DEV environment
dotnet run --env DEV --opportunity 12345678-1234-1234-1234-123456789012

# Run with UAT environment (default)
dotnet run --opportunity 12345678-1234-1234-1234-123456789012

# Run with PROD environment
dotnet run --env PROD --opportunity 12345678-1234-1234-1234-123456789012
```

## Notes

- No rebuilding is required when switching environments
- Configuration is loaded at runtime based on the `--env` parameter
- All config files are centralized for easier maintenance
- The tool validates all required configuration values at startup
