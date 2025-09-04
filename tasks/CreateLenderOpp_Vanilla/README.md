# CreateLenderOpp

This script automates the process of creating and qualifying a lender lead in CRM by executing two programs in sequence:
1. CreateLenderLead - Creates a new lead in the CRM system
2. QualifyLenderLead - Qualifies the newly created lead, converting it to an opportunity

## Prerequisites

Before running this script, ensure that:

1. Both the CreateLenderLead and QualifyLenderLead projects have been built
2. The executable files are located at:
   - `..\..\steps\CreateLenderLead\Implementation\bin\Debug\net8.0\CreateLenderLead.exe`
   - `..\..\steps\QualifyLenderLead\Implementation\bin\Debug\net6.0\QualifyLead.exe`
3. You have appropriate permissions to access the CRM environment

## Usage

Navigate to C:\customApps\Icharus\tasks\CreateLenderOpp_Vanilla
Execute the following:
   $opportunityId = .\CreateLenderOpp.ps1; Write-Host "$opportunityId"
Your opp has been created in UAT

### Basic Usage

To run the script with default settings (UAT environment, live mode):

```powershell
.\CreateLenderOpp.ps1
```

### Using Mock Mode

To run the script in mock mode (no actual CRM connection):

```powershell
.\CreateLenderOpp.ps1 -UseMock
```

### Specifying Environment

To specify a different environment (DEV, UAT, or PROD):

```powershell
.\CreateLenderOpp.ps1 -Environment DEV
```

or

```powershell
.\CreateLenderOpp.ps1 -Environment PROD
```

## How It Works

1. The script first validates the environment parameter
2. It checks if the required executables exist
3. It executes CreateLenderLead and captures the output
4. It extracts the lead ID from the output
5. It passes the lead ID to QualifyLenderLead
6. It displays the result ID from the qualification process

## Troubleshooting

If you encounter issues:

1. Verify that both executables have been built and exist at the expected locations
2. Check that you have appropriate network connectivity and VPN access (if required)
3. Ensure your CRM credentials and permissions are valid
4. Review the console output for specific error messages

## Notes

- The script uses regular expressions to extract the lead ID and result ID from the console output
- If the lead ID cannot be extracted, the script will exit with an error
- The default environment is UAT if not specified
