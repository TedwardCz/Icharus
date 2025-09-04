# setup.ps1
# This script builds all the .NET projects in the Icharus steps directory
# It builds each project in Debug configuration by default

param(
    [string]$Configuration = "Debug",
    [switch]$Clean,
    [switch]$Verbose,
    [switch]$Help
)

if ($Help) {
    Write-Host @"
setup.ps1 - Build all Icharus step projects

USAGE:
    .\setup.ps1 [OPTIONS]

OPTIONS:
    -Configuration <config>  Build configuration (Debug, Release, MockDebug). Default: Debug
    -Clean                   Clean projects before building
    -Verbose                 Show detailed build output
    -Help                    Show this help message

EXAMPLES:
    .\setup.ps1                    # Build all projects in Debug mode
    .\setup.ps1 -Clean             # Clean and build all projects
    .\setup.ps1 -Configuration Release  # Build in Release mode
    .\setup.ps1 -Verbose           # Show detailed build output

PROJECTS BUILT:
    - CreateLenderLead
    - CreateRealtyLead
    - PopulateLenderOpportunity
    - PopulateRealtyOpportunity
    - QualifyLenderLead
    - QualifyRealtyLead
"@
    exit 0
}

# Validate configuration parameter
$validConfigurations = @("Debug", "Release", "MockDebug")
if (-not ($validConfigurations -contains $Configuration)) {
    Write-Host "Invalid configuration: $Configuration. Valid values are: Debug, Release, MockDebug" -ForegroundColor Red
    exit 1
}

Write-Host "========================================"
Write-Host "Icharus Build Script"
Write-Host "Configuration: $Configuration"
Write-Host "Clean: $($Clean.IsPresent)"
Write-Host "========================================"

# Define the projects to build
$projects = @(
    @{
        Name = "CreateLenderLead"
        Path = "steps\CreateLenderLead\Implementation\CreateLenderLead.csproj"
    },
    @{
        Name = "CreateRealtyLead"
        Path = "steps\CreateRealtyLead\Implementation\CreateRealtyLead.csproj"
    },
    @{
        Name = "PopulateLenderOpportunity"
        Path = "steps\PopulateLenderOpportunity\Implementation\PopulateLenderOpportunity.csproj"
    },
    @{
        Name = "PopulateRealtyOpportunity"
        Path = "steps\PopulateRealtyOpportunity\Implementation\PopulateRealtyOpportunity.csproj"
    },
    @{
        Name = "QualifyLenderLead"
        Path = "steps\QualifyLenderLead\Implementation\QualifyLenderLead.csproj"
    },
    @{
        Name = "QualifyRealtyLead"
        Path = "steps\QualifyRealtyLead\Implementation\QualifyRealtyLead.csproj"
    }
)

# Get the root directory (one level up from setup)
$rootDir = Split-Path -Parent $PSScriptRoot

# Track build results
$buildResults = @()
$successCount = 0
$failureCount = 0

foreach ($project in $projects) {
    $projectPath = Join-Path -Path $rootDir -ChildPath $project.Path
    $projectName = $project.Name
    
    Write-Host "`n[Building] $projectName..." -ForegroundColor Cyan
    Write-Host "Project path: $projectPath"
    
    # Check if project file exists
    if (-not (Test-Path $projectPath)) {
        Write-Host "ERROR: Project file not found: $projectPath" -ForegroundColor Red
        $buildResults += @{
            Project = $projectName
            Status = "FAILED"
            Error = "Project file not found"
        }
        $failureCount++
        continue
    }
    
    try {
        # Build dotnet command arguments
        $dotnetArgs = @("build", $projectPath, "--configuration", $Configuration)
        
        if ($Verbose) {
            $dotnetArgs += "--verbosity", "detailed"
        } else {
            $dotnetArgs += "--verbosity", "minimal"
        }
        
        # Clean if requested
        if ($Clean) {
            Write-Host "  Cleaning $projectName..." -ForegroundColor Yellow
            $cleanArgs = @("clean", $projectPath, "--configuration", $Configuration)
            if ($Verbose) {
                $cleanArgs += "--verbosity", "detailed"
            }
            
            $cleanResult = & dotnet $cleanArgs 2>&1
            if ($LASTEXITCODE -ne 0) {
                Write-Host "  WARNING: Clean failed for $projectName" -ForegroundColor Yellow
                if ($Verbose) {
                    Write-Host $cleanResult
                }
            } else {
                Write-Host "  Clean successful for $projectName" -ForegroundColor Green
            }
        }
        
        # Build the project
        Write-Host "  Building $projectName..." -ForegroundColor Yellow
        $buildOutput = & dotnet $dotnetArgs 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  SUCCESS: $projectName built successfully" -ForegroundColor Green
            $buildResults += @{
                Project = $projectName
                Status = "SUCCESS"
                Error = $null
            }
            $successCount++
        } else {
            Write-Host "  FAILED: $projectName build failed" -ForegroundColor Red
            $buildResults += @{
                Project = $projectName
                Status = "FAILED"
                Error = "Build failed"
            }
            $failureCount++
            
            # Show build output on failure
            Write-Host "Build output:" -ForegroundColor Red
            Write-Host $buildOutput -ForegroundColor Red
        }
        
        if ($Verbose -and $LASTEXITCODE -eq 0) {
            Write-Host "Build output:" -ForegroundColor Gray
            Write-Host $buildOutput -ForegroundColor Gray
        }
        
    } catch {
        Write-Host "  EXCEPTION: Exception building $projectName : $_" -ForegroundColor Red
        $buildResults += @{
            Project = $projectName
            Status = "FAILED"
            Error = $_.Exception.Message
        }
        $failureCount++
    }
}

# Display summary
Write-Host "`n========================================"
Write-Host "BUILD SUMMARY"
Write-Host "========================================"
Write-Host "Configuration: $Configuration"
Write-Host "Total Projects: $($projects.Count)"
Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $failureCount" -ForegroundColor Red

Write-Host "`nDetailed Results:"
foreach ($result in $buildResults) {
    $color = if ($result.Status -eq "SUCCESS") { "Green" } else { "Red" }
    $status = $result.Status.PadRight(8)
    Write-Host "  $status $($result.Project)" -ForegroundColor $color
    if ($result.Error) {
        Write-Host "           Error: $($result.Error)" -ForegroundColor Red
    }
}

if ($failureCount -eq 0) {
    Write-Host "`nSUCCESS: All projects built successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`nERROR: $failureCount project(s) failed to build." -ForegroundColor Red
    exit 1
}
