# TestAgentAttachment_myQueueRss

This script automates the complete realty workflow plus agent attachment testing by executing four programs in sequence:
1. CreateRealtyLead - Creates a new realty lead in the CRM system
2. QualifyRealtyLead - Qualifies the newly created lead, converting it to an opportunity
3. PopulateRealtyOpportunity - Completes the opportunity population with required fields
4. myQueueRssAttachAgent - Tests web automation to attach an agent to the opportunity

## Prerequisites

Before running this script, ensure that:

1. All four projects have been built in Debug configuration:
   - CreateRealtyLead
   - QualifyRealtyLead  
   - PopulateRealtyOpportunity
   - myQueueRssAttachAgent

2. The executable files are located at:
   - `..\..\steps\CreateRealtyLead\Implementation\bin\Debug\net8.0\CreateRealtyLead.exe`
   - `..\..\steps\QualifyRealtyLead\Implementation\bin\Debug\net8.0\QualifyRealtyLead.exe`
   - `..\..\steps\PopulateRealtyOpportunity\Implementation\bin\Debug\net6.0\PopulateRealtyOpportunity.exe`
   - `..\..\steps\myQueueRssAttachAgent\Implementation\bin\Debug\net8.0\myQueueRssAttachAgent.exe`

3. You have appropriate permissions to access the CRM environment
4. Chrome browser is installed for web automation
5. VPN connection is active (if required for your environment)

## Usage

Navigate to `C:\customApps\Icharus\tasks\TestAgentAttachment_myQueueRss`

### Basic Usage

To run the script with default settings (UAT environment, live mode):

```powershell
.\TestAgentAttachment_myQueueRss.ps1
```

### Using Mock Mode

To run the script in mock mode for PopulateRealtyOpportunity (no actual CRM connection for that step):

```powershell
.\TestAgentAttachment_myQueueRss.ps1 -UseMock
```

### Specifying Environment

To specify a different environment (DEV, UAT, or PROD):

```powershell
.\TestAgentAttachment_myQueueRss.ps1 -Environment DEV
```

### Custom Lead Data

To specify custom lead information:

```powershell
.\TestAgentAttachment_myQueueRss.ps1 -FirstName "John" -LastName "Doe" -EmailAddress1 "john.doe@example.com"
```

## How It Works

1. **Step 1: CreateRealtyLead** - Creates a new realty lead with random or specified data
2. **Step 2: QualifyRealtyLead** - Converts the lead to a qualified opportunity
3. **Step 3: PopulateRealtyOpportunity** - Completes opportunity fields (rss_brand=0, closeprobability=33)
4. **Step 4: myQueueRssAttachAgent** - Launches web automation to test agent attachment

The script captures output from each step and passes IDs between programs:
- Lead ID flows from CreateRealtyLead → QualifyRealtyLead
- Opportunity ID flows from QualifyRealtyLead → PopulateRealtyOpportunity → myQueueRssAttachAgent

## Parameters

All parameters are optional. If not provided, random test data will be generated:

- `-Environment` - CRM environment (DEV, UAT, PROD). Default: UAT
- `-FirstName` - Lead's first name
- `-LastName` - Lead's last name  
- `-Address1_line1` - Street address
- `-Address1_city` - City
- `-Address1_stateorprovince` - State/Province
- `-Address1_postalcode` - Postal code
- `-EmailAddress1` - Email address
- `-Rss_mostrecentphonenumber` - Primary phone number
- `-Telephone1` - Home phone
- `-Telephone2` - Work phone  
- `-MobilePhone` - Mobile phone
- `-Vu_purchaselocationstate` - Purchase location state
- `-Vu_purchaselocationcity` - Purchase location city
- `-Vu_purchaselocationzip` - Purchase location zip
- `-Vu_subjectcity` - Subject property city
- `-Vu_subjectstate` - Subject property state
- `-Vu_subjectzipcode` - Subject property zip
- `-Vu_vurpurpose` - VUR purpose code
- `-UseMock` - Use mock mode for PopulateRealtyOpportunity

## Output

The script returns the opportunity ID and displays:
- Lead ID created in step 1
- Opportunity ID created in step 2  
- Confirmation of opportunity population in step 3
- Results of agent attachment automation in step 4

## Troubleshooting

If you encounter issues:

1. **Build Errors**: Verify all four projects have been built successfully
2. **Path Errors**: Check that executables exist at expected locations
3. **CRM Connection**: Ensure VPN is connected and CRM credentials are valid
4. **Web Automation**: Verify Chrome browser is installed and accessible
5. **Permissions**: Check that you have appropriate CRM and system permissions

## Notes

- The script creates a complete end-to-end test from fresh lead to agent attachment
- Uses regular expressions to extract IDs from console output
- Web automation step may take several minutes to complete
- All steps use the same environment configuration
- Mock mode only affects PopulateRealtyOpportunity step - other steps always use live CRM
