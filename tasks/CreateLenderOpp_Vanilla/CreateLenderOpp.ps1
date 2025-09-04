# CreateLenderOpp.ps1
# This script executes CreateLenderLead and QualifyLenderLead in sequence
# It captures the lead ID from CreateLenderLead and passes it to QualifyLenderLead
# Returns the OpportunityId as the result

param (
    [string]$Environment = "UAT",
    [string]$FirstName,
    [string]$LastName,
    [string]$Address1_line1,
    [string]$Address1_city,
    [string]$Address1_stateorprovince,
    [string]$Address1_postalcode,
    [string]$EmailAddress1,
    [string]$Vu_mostrecentphonenumber,
    [string]$Telephone2,
    [string]$MobilePhone,
    [string]$Telephone3,
    [string]$Vu_purchaselocationstate,
    [string]$Vu_purchaselocationzip,
    [switch]$UseMock
)

# Validate environment parameter
$validEnvironments = @("DEV", "UAT", "PROD")
if (-not ($validEnvironments -contains $Environment)) {
    Write-Host "Invalid environment: $Environment. Valid values are: DEV, UAT, PROD"
    Write-Host "Using default environment: UAT"
    $Environment = "UAT"
}

Write-Host "========================================"
Write-Host "CreateLenderOpp Script"
Write-Host "Environment: $Environment"
Write-Host "========================================"

# Paths to the executables - using Debug build configuration (environment configs loaded at runtime)
$createLeadPath = Join-Path -Path $PSScriptRoot -ChildPath "..\..\steps\CreateLenderLead\Implementation\bin\Debug\net8.0\CreateLenderLead.exe"
$qualifyLeadPath = Join-Path -Path $PSScriptRoot -ChildPath "..\..\steps\QualifyLenderLead\Implementation\bin\Debug\net6.0\QualifyLenderLead.exe"

# Define path for PopulateLenderOpportunity
$populateOpportunityPath = Join-Path -Path $PSScriptRoot -ChildPath "..\..\steps\PopulateLenderOpportunity\Implementation\bin\Debug\net6.0\PopulateLenderOpportunity.exe"

# Check if we should use the Mock version instead
if ($UseMock) {
    $populateOpportunityMockPath = Join-Path -Path $PSScriptRoot -ChildPath "..\..\steps\PopulateLenderOpportunity\Implementation\bin\MockDebug\net6.0\PopulateLenderOpportunity.exe"
    if (Test-Path $populateOpportunityMockPath) {
        $populateOpportunityPath = $populateOpportunityMockPath
        Write-Host "Using MockDebug version of PopulateLenderOpportunity (MOCK MODE)"
    } else {
        Write-Host "Error: MockDebug version not found at: $populateOpportunityMockPath"
        Write-Host "Please build the PopulateLenderOpportunity project with MockDebug configuration first."
        exit 1
    }
} else {
    # Verify the path exists
    if (Test-Path $populateOpportunityPath) {
        Write-Host "Using LIVE version of PopulateLenderOpportunity (connecting to real CRM)"
        Write-Host "WARNING: Live version may hang or timeout due to CRM connection issues"
    } else {
        Write-Host "Error: Live version not found at: $populateOpportunityPath"
        Write-Host "Please build the PopulateLenderOpportunity project with Debug configuration first."
        exit 1
    }
}

# Debug information
Write-Host "Script running from: $PSScriptRoot"
Write-Host "CreateLenderLead path: $createLeadPath"
Write-Host "QualifyLenderLead path: $qualifyLeadPath"
Write-Host "PopulateLenderOpportunity path: $populateOpportunityPath"

# Verify that the executables exist
if (-not (Test-Path $createLeadPath)) {
    Write-Host "Error: CreateLenderLead executable not found at: $createLeadPath"
    Write-Host "Please build the CreateLenderLead project first."
    exit 1
}

if (-not (Test-Path $qualifyLeadPath)) {
    Write-Host "Error: QualifyLenderLead executable not found at: $qualifyLeadPath"
    Write-Host "Please build the QualifyLenderLead project first."
    exit 1
}

if (-not (Test-Path $populateOpportunityPath)) {
    Write-Host "Error: PopulateLenderOpportunity executable not found at: $populateOpportunityPath"
    Write-Host "Please build the PopulateLenderOpportunity project first."
    exit 1
}

# Step 1: Execute CreateLenderLead
Write-Host "`n[Step 1] Executing CreateLenderLead..."

# Build the parameter list for CreateLenderLead
$createLeadParams = @("--env", $Environment)

# Add optional parameters if they are provided
if ($FirstName) { $createLeadParams += @("--firstname", $FirstName) }
if ($LastName) { $createLeadParams += @("--lastname", $LastName) }
if ($Address1_line1) { $createLeadParams += @("--address1_line1", $Address1_line1) }
if ($Address1_city) { $createLeadParams += @("--address1_city", $Address1_city) }
if ($Address1_stateorprovince) { $createLeadParams += @("--address1_stateorprovince", $Address1_stateorprovince) }
if ($Address1_postalcode) { $createLeadParams += @("--address1_postalcode", $Address1_postalcode) }
if ($EmailAddress1) { $createLeadParams += @("--emailaddress1", $EmailAddress1) }
if ($Vu_mostrecentphonenumber) { $createLeadParams += @("--vu_mostrecentphonenumber", $Vu_mostrecentphonenumber) }
if ($Telephone2) { $createLeadParams += @("--telephone2", $Telephone2) }
if ($MobilePhone) { $createLeadParams += @("--mobilephone", $MobilePhone) }
if ($Telephone3) { $createLeadParams += @("--telephone3", $Telephone3) }
if ($Vu_purchaselocationstate) { $createLeadParams += @("--vu_purchaselocationstate", $Vu_purchaselocationstate) }
if ($Vu_purchaselocationzip) { $createLeadParams += @("--vu_purchaselocationzip", $Vu_purchaselocationzip) }

# Display the command being executed
$paramDisplay = $createLeadParams -join " "
Write-Host "Executing: $createLeadPath $paramDisplay"

try {
    $createLeadOutput = & $createLeadPath $createLeadParams 2>&1 | Out-String
    Write-Host "CreateLenderLead Output:"
    Write-Host $createLeadOutput
} catch {
    Write-Host "Error executing CreateLenderLead: $_"
    exit 1
}

# Extract the lead ID from the output
$leadIdPattern = "Lead created successfully with ID: ([0-9a-fA-F\-]{36})"
$leadIdMatch = [regex]::Match($createLeadOutput, $leadIdPattern)

if (-not $leadIdMatch.Success) {
    Write-Host "Error: Failed to extract lead ID from CreateLenderLead output."
    Write-Host "Output was:"
    Write-Host $createLeadOutput
    exit 1
}

$leadId = $leadIdMatch.Groups[1].Value
Write-Host "Successfully created lead with ID: $leadId"

# Step 2: Execute QualifyLenderLead with the lead ID
Write-Host "`n[Step 2] Executing QualifyLenderLead with lead ID: $leadId..."
try {
    $qualifyLeadOutput = & $qualifyLeadPath --env $Environment --lead $leadId 2>&1 | Out-String
    Write-Host "QualifyLenderLead Output:"
    Write-Host $qualifyLeadOutput
} catch {
    Write-Host "Error executing QualifyLenderLead: $_"
    exit 1
}

# Extract the result ID from the output
$resultIdPattern = "Lead qualified successfully with result ID: ([0-9a-fA-F\-]{36})"
$resultIdMatch = [regex]::Match($qualifyLeadOutput, $resultIdPattern)

$opportunityId = $null

if ($resultIdMatch.Success) {
    $opportunityId = $resultIdMatch.Groups[1].Value
    Write-Host "Successfully qualified lead. Result ID: $opportunityId"
} else {
    Write-Host "Lead qualification completed, but couldn't extract result ID."
    Write-Host "Output was:"
    Write-Host $qualifyLeadOutput
    exit 1
}

# Step 3: Execute PopulateLenderOpportunity with the opportunity ID
Write-Host "`n[Step 3] Executing PopulateLenderOpportunity with opportunity ID: $opportunityId..."

# Build the parameter list for PopulateLenderOpportunity
$populateOpportunityParams = @("--env", $Environment, "--opportunity", $opportunityId, "--vu_brand", "2", "--closeprobability", "33")

# Display the command being executed
$paramDisplay = $populateOpportunityParams -join " "
Write-Host "Executing: $populateOpportunityPath $paramDisplay"

try {
    # Run PopulateLenderOpportunity the same way as CreateLenderLead
    $populateOpportunityOutput = & $populateOpportunityPath $populateOpportunityParams 2>&1 | Out-String
    Write-Host "PopulateLenderOpportunity Output:"
    Write-Host $populateOpportunityOutput
} catch {
    Write-Host "Error executing PopulateLenderOpportunity: $_"
    exit 1
}

# Extract the result ID from the output to confirm success
$populateResultPattern = "Opportunity populated successfully with ID: ([0-9a-fA-F\-]{36})"
$populateResultMatch = [regex]::Match($populateOpportunityOutput, $populateResultPattern)

if ($populateResultMatch.Success) {
    $populatedOpportunityId = $populateResultMatch.Groups[1].Value
    Write-Host "Successfully populated opportunity with ID: $populatedOpportunityId"
} else {
    Write-Host "Opportunity population completed, but couldn't extract confirmation ID."
    Write-Host "Output was:"
    Write-Host $populateOpportunityOutput
}

Write-Host "`n========================================"
Write-Host "CreateLenderOpp Script Completed"
Write-Host "========================================"

# Return the opportunity ID
return $opportunityId
