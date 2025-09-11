# Icharus

A comprehensive .NET CRM automation solution for creating, qualifying, and populating leads and opportunities in both Lender and Realty CRM systems.

## Overview

Icharus provides a modular architecture for CRM operations with centralized configuration management, supporting multiple environments (DEV, UAT, PROD) with runtime environment switching.

## Architecture

### Directory Structure

```
Icharus/
├── app.env.config/          # Centralized configuration files
│   ├── App.config           # Base configuration
│   ├── App.Debug.config     # DEV environment overrides
│   ├── App.UAT.config       # UAT environment overrides
│   └── App.PROD.config      # PROD environment overrides
├── setup/                   # Build and setup scripts
│   └── setup.ps1           # Builds all projects
├── steps/                   # Individual CRM operations
│   ├── CreateLenderLead/    # Create lender leads
│   ├── CreateRealtyLead/    # Create realty leads
│   ├── PopulateLenderOpportunity/  # Populate lender opportunities
│   ├── PopulateRealtyOpportunity/  # Populate realty opportunities
│   ├── QualifyLenderLead/   # Convert lender leads to opportunities
│   ├── QualifyRealtyLead/   # Convert realty leads to opportunities
│   ├── SearchLenderLeadByRssLinkId/    # Search lender leads by RSS Link ID
│   ├── SearchRealtyLeadByRssLinkId/    # Search realty leads by RSS Link ID
│   ├── SearchLenderOpportunityByRssLinkId/  # Search lender opportunities by RSS Link ID
│   └── SearchRealtyOpportunityByRssLinkId/  # Search realty opportunities by RSS Link ID
└── tasks/                   # Orchestrated workflows
    ├── CreateLenderOpp_Vanilla/  # Full lender workflow
    └── CreateRealtyOpp_Vanilla/  # Full realty workflow
```

## Quick Start

### 1. Build All Projects
```powershell
cd setup
.\setup.ps1
```

### 2. Run Individual Steps
```powershell
# Create a lender lead
cd steps\CreateLenderLead\Implementation
dotnet run -- --env UAT

# Qualify the lead to opportunity
cd ..\..\..\QualifyLenderLead\Implementation
dotnet run -- --env UAT --lead <LEAD_ID>
```

### 3. Run Complete Workflows
```powershell
# Complete lender workflow (Create → Qualify → Populate)
cd tasks\CreateLenderOpp_Vanilla
.\CreateLenderOpp.ps1 -Environment UAT

# Complete realty workflow
cd ..\CreateRealtyOpp_Vanilla
.\CreateRealtyOpp.ps1 -Environment UAT
```

## Configuration

### Centralized Configuration System

All projects use centralized configuration files located in `app.env.config/`:

- **App.config** - Base configuration with default values
- **App.Debug.config** - DEV environment overrides
- **App.UAT.config** - UAT environment overrides  
- **App.PROD.config** - PROD environment overrides

### Key Configuration Values

#### Lender CRM
- `LenderApplicationToken` - Authentication token
- `LenderBusinessUnit` - Business unit (CRM)
- `LenderCrmServiceEnvironment` - Target environment
- `LenderLeadAdmin` - Team GUID for lead ownership
- `LenderLoanOfficer` - Loan officer GUID

#### Realty CRM
- `RealtyApplicationToken` - Authentication token
- `RealtyBusinessUnit` - Business unit (CRM)
- `RealtyCrmServiceEnvironment` - Target environment
- `RealtyLeadAdmin` - Team GUID for lead ownership
- `RealtyLoanOfficer` - Agent GUID

### Runtime Environment Switching

No rebuilding required - specify environment at runtime:

```powershell
# Switch environments dynamically
dotnet run -- --env DEV
dotnet run -- --env UAT
dotnet run -- --env PROD
```

## Projects

### Steps (Individual Operations)

| Project | Purpose | Target Framework |
|---------|---------|------------------|
| CreateLenderLead | Create lender leads in CRM | .NET 8.0 |
| CreateRealtyLead | Create realty leads in CRM | .NET 8.0 |
| QualifyLenderLead | Convert lender leads to opportunities | .NET 6.0 |
| QualifyRealtyLead | Convert realty leads to opportunities | .NET 8.0 |
| PopulateLenderOpportunity | Populate lender opportunity data | .NET 6.0 |
| PopulateRealtyOpportunity | Populate realty opportunity data | .NET 6.0 |
| SearchLenderLeadByRssLinkId | Search lender leads by RSS Link ID | .NET 8.0 |
| SearchRealtyLeadByRssLinkId | Search realty leads by RSS Link ID | .NET 8.0 |
| SearchLenderOpportunityByRssLinkId | Search lender opportunities by RSS Link ID | .NET 8.0 |
| SearchRealtyOpportunityByRssLinkId | Search realty opportunities by RSS Link ID | .NET 8.0 |

### Tasks (Orchestrated Workflows)

| Task | Description | Steps |
|------|-------------|-------|
| CreateLenderOpp_Vanilla | Complete lender workflow | CreateLenderLead → QualifyLenderLead → PopulateLenderOpportunity |
| CreateRealtyOpp_Vanilla | Complete realty workflow | CreateRealtyLead → QualifyRealtyLead → PopulateRealtyOpportunity |

## Build Configurations

Each project supports multiple build configurations:

- **Debug** - Standard build with CRM SDK integration
- **MockDebug** - Mock implementation for testing without CRM
- **Release** - Production build with CRM SDK integration

```powershell
# Build with mock implementation
.\setup.ps1 -Configuration MockDebug

# Build for release
.\setup.ps1 -Configuration Release
```

## Requirements

- .NET 6.0 SDK (minimum)
- .NET 8.0 SDK (for newer projects)
- Access to private NuGet repository: `nugetgallery.p.vu.local/api/v2`
- CRM environment credentials
- VPN access (if required for CRM connectivity)

## Usage Examples

### Create a Single Lead
```powershell
cd steps\CreateLenderLead\Implementation
dotnet run -- --env UAT --firstname "John" --lastname "Doe" --emailaddress1 "john.doe@example.com"
```

### Run Complete Workflow with Custom Parameters
```powershell
cd tasks\CreateLenderOpp_Vanilla
.\CreateLenderOpp.ps1 -Environment UAT -FirstName "Jane" -LastName "Smith" -EmailAddress1 "jane.smith@example.com"
```

### Mock Mode for Testing
```powershell
# Test without CRM connectivity
.\CreateLenderOpp.ps1 -UseMock
```

## Development

### Adding New Configuration Settings

1. Add setting to base `app.env.config\App.config`
2. Add environment-specific values to each environment config file
3. Update Program.cs configuration loading if needed
4. No rebuilding required - changes take effect immediately

### Project Structure

Each step project follows this structure:
```
StepName/
├── Implementation/
│   ├── Program.cs           # Main application logic
│   ├── StepName.csproj      # Project file with centralized config reference
│   ├── NuGet.config         # Private package source configuration
│   └── bin/                 # Build outputs
└── README.md                # Step-specific documentation
```

## Troubleshooting

### Common Issues

1. **Build Failures**: Ensure access to private NuGet repository
2. **CRM Connection Issues**: Verify VPN connectivity and credentials
3. **Configuration Errors**: Check centralized config files in `app.env.config/`
4. **Environment Issues**: Verify environment parameter matches available configs

### Mock Mode

Use mock implementations for development and testing:
```powershell
# Build with mock SDK
.\setup.ps1 -Configuration MockDebug

# Run in mock mode
.\CreateLenderOpp.ps1 -UseMock
```

## License

MIT License - See LICENSE file for details.

## Contributing

1. Follow the centralized configuration approach
2. Update documentation when adding new features
3. Test with mock implementations before CRM integration
4. Maintain backward compatibility with existing workflows
