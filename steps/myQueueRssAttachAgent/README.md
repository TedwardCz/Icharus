# myQueueRssAttachAgent

This step is part of the Icharus workflow and is designed to automate the agent attachment process in the myQueue RSS system. It uses Selenium WebDriver to navigate to an opportunity, populate the Attach Agent form, and complete the agent attachment process.

## Prerequisites

- .NET 6.0 SDK or later
- Chrome browser installed
- Access to myQueue RSS environment (DEV, UAT, or PROD)
- Valid application token for authentication

## Configuration

This tool uses centralized configuration files located at:
```
C:\customApps\Icharus\app.env.config\
```

The configuration is automatically loaded based on the environment parameter:
- DEV environment uses `App.Debug.config`
- UAT environment uses `App.UAT.config`
- PROD environment uses `App.PROD.config`

## Building the Tool

To build the tool, navigate to the Implementation directory and run:

```
dotnet build
```

## Running the Tool

To run the tool, use the following command:

```
dotnet run [--env|-e <environment>] --opportunity|-o <opportunityGuid> [--city <city>] [--state <state>] [--zipcode <zipcode>]
```

### Parameters:

- `--env` or `-e`: Environment (DEV, UAT, PROD) - defaults to UAT
- `--opportunity` or `-o`: The GUID of the opportunity to attach agent to (required)
- `--city`: City to populate in the form - defaults to Springfield
- `--state`: State to populate in the form - defaults to KS
- `--zipcode`: Zip code to populate in the form - defaults to 00920

### Examples:

Run with default environment (UAT) and default form values:
```
dotnet run --opportunity 12345678-1234-1234-1234-123456789012
```

Run with specific environment:
```
dotnet run --env PROD --opportunity 12345678-1234-1234-1234-123456789012
```

Run with custom form values:
```
dotnet run --opportunity 12345678-1234-1234-1234-123456789012 --city Austin --state TX --zipcode 78701
```

Or using short parameter names:
```
dotnet run -e DEV -o 12345678-1234-1234-1234-123456789012 --city Springfield --state KS --zipcode 00920
```

## What This Tool Does

1. Takes an Opportunity GUID as input
2. Navigates to the opportunity page in myQueue RSS
3. Clicks the "Attach Agent" tab
4. Populates the form with specified city, state, and zip code
5. Submits the form to search for agents
6. Waits for agent results to load
7. Clicks the "Attach Agent" button to attach the first available agent
8. Verifies the attachment was successful
9. Takes screenshots at each major step for debugging

The tool will output the status of each operation and save screenshots to the current directory for troubleshooting.

## Mock Implementation

The tool supports a mock mode for testing without actual browser automation. This can be enabled through the centralized configuration by setting `UseMockImplementation` to `true`.

## Screenshots

The tool automatically captures screenshots at key points:
- After loading the Attach Agent form
- After populating form fields
- After agent search results load
- After successful agent attachment
- On any failures for debugging

## Troubleshooting

### If Chrome Driver Issues
- WebDriverManager automatically downloads the correct ChromeDriver
- Ensure Chrome browser is installed and up to date
- Check that Windows Defender/antivirus isn't blocking the driver

### If URL Access Issues
- Verify VPN/network access to `.vu.local` domains
- Check environment configuration in centralized config files
- Ensure the opportunity GUID exists and is accessible

### If Form Elements Not Found
- Screenshots will be saved to help debug element location issues
- The tool uses XPath selectors that may need updating if the UI changes
- Check browser console for JavaScript errors that might affect page loading

## Integration with Icharus

This tool follows the standard Icharus step pattern:
- Uses centralized configuration system
- Supports multiple environments (DEV/UAT/PROD)
- Returns success/failure exit codes for orchestration
- Can be chained with other Icharus steps in PowerShell workflows
